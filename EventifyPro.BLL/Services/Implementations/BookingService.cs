using Eventify.Domain.Entities;
using Eventify.Domain.Enums;
using Eventify.Shared.Helpers;
using EventifyPro.BLL.DTOs.Booking;
using EventifyPro.BLL.Services.Interfaces;
using EventifyPro.DAL.Repositories.Interfaces;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace EventifyPro.BLL.Services.Implementations;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ITicketService _ticketService;
    private readonly IOutboxService _outboxService;
    private readonly IPdfService _pdfService;
    private readonly IRefundService _refundService;
    private readonly IWaitingListService _waitingListService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ITicketService ticketService,
        IOutboxService outboxService,
        IPdfService pdfService,
        IRefundService refundService,
        IWaitingListService waitingListService,
        IConfiguration configuration,
        ILogger<BookingService> _logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _ticketService = ticketService;
        _outboxService = outboxService;
        _pdfService = pdfService;
        _refundService = refundService;
        _waitingListService = waitingListService;
        _configuration = configuration;
        this._logger = _logger;
    }

    public async Task<Result<BookingDetailDto>> CreatePendingAsync(BookingCreateDto dto, string userId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Validate Event exists and is Published
            var eventEntity = await _unitOfWork.Events.GetByIdWithTicketTypesAsync(dto.EventId, cancellationToken);
            if (eventEntity == null || eventEntity.IsDeleted)
            {
                return Result<BookingDetailDto>.Failure("Event not found.");
            }

            if (eventEntity.Status != EventStatus.Published)
            {
                return Result<BookingDetailDto>.Failure("Cannot book tickets for an unpublished event.");
            }

            if (eventEntity.StartDate <= DateTime.UtcNow)
            {
                return Result<BookingDetailDto>.Failure("Cannot book tickets for an event that has already started.");
            }

            // 1.5 Validate MaxTicketsPerUser limit
            if (eventEntity.MaxTicketsPerUser.HasValue)
            {
                var existingBookedCount = await _unitOfWork.Bookings.GetQuery()
                    .AsNoTracking()
                    .Where(b => b.UserId == userId 
                             && b.EventId == dto.EventId 
                             && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending))
                    .SelectMany(b => b.Items)
                    .SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0;

                var newlyRequested = dto.Items.Sum(i => i.Quantity);
                if (existingBookedCount + newlyRequested > eventEntity.MaxTicketsPerUser.Value)
                {
                    return Result<BookingDetailDto>.Failure($"Booking exceeds the maximum ticket limit per user for this event. You can purchase at most {eventEntity.MaxTicketsPerUser.Value - existingBookedCount} more ticket(s).");
                }
            }

            decimal totalAmount = 0;
            var bookingItems = new List<BookingItem>();

            // 2. Validate, reserve and prepare BookingItems
            foreach (var item in dto.Items)
            {
                var ticketType = eventEntity.TicketTypes.FirstOrDefault(tt => tt.Id == item.TicketTypeId);
                if (ticketType == null)
                {
                    return Result<BookingDetailDto>.Failure($"Ticket type ID {item.TicketTypeId} not found for this event.");
                }

                // Validate Sale window if configured
                if (ticketType.SaleStartDate.HasValue && ticketType.SaleStartDate.Value > DateTime.UtcNow)
                {
                    return Result<BookingDetailDto>.Failure($"Ticket sale for '{ticketType.Name}' has not started yet.");
                }

                if (ticketType.SaleEndDate.HasValue && ticketType.SaleEndDate.Value < DateTime.UtcNow)
                {
                    return Result<BookingDetailDto>.Failure($"Ticket sale for '{ticketType.Name}' has ended.");
                }

                // Check capacity
                if (ticketType.SoldQuantity + item.Quantity > ticketType.TotalQuantity)
                {
                    return Result<BookingDetailDto>.Failure($"Not enough tickets available for '{ticketType.Name}'. Remaining: {ticketType.TotalQuantity - ticketType.SoldQuantity}");
                }

                // Reserve the tickets immediately
                ticketType.SoldQuantity += item.Quantity;
                _unitOfWork.TicketTypes.Update(ticketType);

                totalAmount += item.Quantity * ticketType.Price;

                bookingItems.Add(new BookingItem
                {
                    TicketTypeId = item.TicketTypeId,
                    Quantity = item.Quantity,
                    UnitPrice = ticketType.Price // Historical snapshot
                });
            }

            // Check overall event capacity limit
            if (eventEntity.MaxCapacity.HasValue)
            {
                var currentSold = eventEntity.TicketTypes.Sum(tt => tt.SoldQuantity);
                // Note: currentSold already includes newlyReserved from the loop above, so no need to add newlyRequested again!
                if (currentSold > eventEntity.MaxCapacity.Value)
                {
                    return Result<BookingDetailDto>.Failure($"Booking exceeds maximum event capacity. Max allowed: {eventEntity.MaxCapacity.Value - (currentSold - dto.Items.Sum(i => i.Quantity))}");
                }
            }

            // 3. Create Pending Booking (including Flat Service Fee from settings)
            decimal serviceFee = _configuration.GetValue<decimal?>("BookingSettings:FlatServiceFee") ?? 50.00m;
            decimal finalTotalAmount = totalAmount + serviceFee;

            var booking = new Booking
            {
                UserId = userId,
                EventId = dto.EventId,
                TotalAmount = finalTotalAmount,
                ServiceFee = serviceFee,
                Status = BookingStatus.Pending,
                BookingDate = DateTime.UtcNow,
                BookingReference = string.Empty, // Will update next
                Items = bookingItems
            };

            await _unitOfWork.Bookings.AddAsync(booking, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 4. Generate Unique Booking Reference
            booking.BookingReference = BookingReferenceHelper.Generate(booking.Id, booking.BookingDate);
            _unitOfWork.Bookings.Update(booking);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var resultDto = _mapper.Map<BookingDetailDto>(booking);
            return Result<BookingDetailDto>.Success(resultDto);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result> ConfirmAsync(int bookingId, string transactionId, CancellationToken cancellationToken = default)
    {
        int retries = 3;
        while (retries > 0)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var booking = await _unitOfWork.Bookings.GetByIdWithDetailsAsync(bookingId, cancellationToken);
                if (booking == null)
                {
                    return Result.Failure("Booking not found.");
                }

                if (booking.Status == BookingStatus.Confirmed)
                {
                    // Already confirmed, handle idempotency
                    await transaction.CommitAsync(cancellationToken);
                    return Result.Success();
                }

                if (booking.Status != BookingStatus.Pending)
                {
                    return Result.Failure("Only pending bookings can be confirmed.");
                }

                var eventEntity = await _unitOfWork.Events.GetByIdWithTicketTypesAsync(booking.EventId, cancellationToken);
                if (eventEntity == null || eventEntity.IsDeleted)
                {
                    return Result.Failure("Event not found.");
                }

                // Ticket capacity was already reserved in CreatePendingAsync, so we don't need to check capacity or increment SoldQuantity again.
                // We only need to ensure the ticket types still exist as a basic safety check.
                foreach (var item in booking.Items)
                {
                    var ticketType = eventEntity.TicketTypes.FirstOrDefault(tt => tt.Id == item.TicketTypeId);
                    if (ticketType == null)
                    {
                        return Result.Failure($"Ticket type ID {item.TicketTypeId} not found for this event.");
                    }
                }

                // Update Booking Status
                booking.Status = BookingStatus.Confirmed;
                booking.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Bookings.Update(booking);

                // Add or update Payment record
                var payment = await _unitOfWork.Payments.GetPaymentByBookingAsync(bookingId, cancellationToken);
                if (payment == null)
                {
                    payment = new Payment
                    {
                        BookingId = bookingId,
                        Amount = booking.TotalAmount,
                        Method = PaymentMethod.CreditCard,
                        Status = PaymentStatus.Completed,
                        TransactionId = transactionId,
                        PaymentDate = DateTime.UtcNow
                    };
                    await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
                }
                else
                {
                    payment.Status = PaymentStatus.Completed;
                    payment.TransactionId = transactionId;
                    payment.PaymentDate = DateTime.UtcNow;
                    payment.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Payments.Update(payment);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Generate Tickets (creates ticket rows and generates unique QR codes)
                var ticketResult = await _ticketService.GenerateForBookingAsync(bookingId, cancellationToken);
                if (ticketResult.IsFailure)
                {
                    return Result.Failure(ticketResult.Error!);
                }

                // Get Attendee user details for email confirmation
                var user = await _unitOfWork.Users.GetByIdAsync(booking.UserId, cancellationToken);
                if (user != null)
                {
                    // Generate PDF ticket for the booking (taking the first generated ticket or combined details)
                    byte[] pdfBytes = [];
                    var generatedTickets = ticketResult.Data!;
                    if (generatedTickets.Count > 0)
                    {
                        // Generate PDF for the first ticket in booking, or if PdfService gets extended we can generate combined.
                        // Here we generate PDF for the first ticket as the main attachment or placeholder.
                        pdfBytes = await _pdfService.GenerateTicketPdfAsync(generatedTickets[0].Id, cancellationToken);
                    }

                    // Enqueue Booking Confirmation Email to transactional outbox
                    await _outboxService.EnqueueAsync(
                        "Email.TicketConfirmation",
                        new OutboxService.TicketConfirmationPayload
                        {
                            RecipientEmail = user.Email ?? string.Empty,
                            RecipientName = user.FullName,
                            BookingRef = booking.BookingReference,
                            PdfAttachmentBase64 = Convert.ToBase64String(pdfBytes)
                        },
                        cancellationToken
                    );
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Result.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);
                retries--;
                if (retries == 0)
                {
                    return Result.Failure("Overselling concurrency conflict occurred. Please try booking again.");
                }
                // Back-off briefly before retry
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        return Result.Failure("Failed to confirm booking due to concurrent updates.");
    }

    public async Task<Result> CancelAsync(int bookingId, string userId, string reason, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var booking = await _unitOfWork.Bookings.GetByIdWithDetailsAsync(bookingId, cancellationToken);
            if (booking == null)
            {
                return Result.Failure("Booking not found.");
            }

            // Verify ownership
            if (booking.UserId != userId)
            {
                return Result.Failure("You are not authorized to cancel this booking.");
            }

            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Refunded)
            {
                await transaction.CommitAsync(cancellationToken);
                return Result.Success(); // Idempotent
            }

            // Check if cancellation window is open (allowed up to 24 hours before event starts)
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(booking.EventId, cancellationToken);
            if (eventEntity == null)
            {
                return Result.Failure("Associated event not found.");
            }

            if (eventEntity.StartDate <= DateTime.UtcNow)
            {
                return Result.Failure("Cannot cancel booking for an event that has already started.");
            }

            if (eventEntity.StartDate.AddHours(-24) <= DateTime.UtcNow)
            {
                return Result.Failure("Cancellations are only allowed up to 24 hours before the event starts.");
            }

            var previousStatus = booking.Status;

            // Release ticket capacity if it was a Confirmed or Pending booking
            if (previousStatus == BookingStatus.Confirmed || previousStatus == BookingStatus.Pending)
            {
                var eventWithTicketTypes = await _unitOfWork.Events.GetByIdWithTicketTypesAsync(booking.EventId, cancellationToken);
                if (eventWithTicketTypes != null)
                {
                    foreach (var item in booking.Items)
                    {
                        var ticketType = eventWithTicketTypes.TicketTypes.FirstOrDefault(tt => tt.Id == item.TicketTypeId);
                        if (ticketType != null)
                        {
                            ticketType.SoldQuantity = Math.Max(0, ticketType.SoldQuantity - item.Quantity);
                            _unitOfWork.TicketTypes.Update(ticketType);
                        }
                    }
                }

                // If payment exists (Confirmed booking), initiate a full refund
                if (previousStatus == BookingStatus.Confirmed)
                {
                    var payment = await _unitOfWork.Payments.GetPaymentByBookingAsync(bookingId, cancellationToken);
                    if (payment != null && payment.Status == PaymentStatus.Completed)
                    {
                        var refundResult = await _refundService.InitiateAsync(
                            new RefundCreateDto
                            {
                                PaymentId = payment.Id,
                                Amount = payment.Amount,
                                Reason = $"User cancelled booking. Reason: {reason}"
                            },
                            userId,
                            cancellationToken
                        );

                        if (refundResult.IsFailure)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            return Result.Failure($"Cancellation failed due to refund error: {refundResult.Error}");
                        }
                    }
                }
            }

            // Update Booking Status to Cancelled
            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = reason;
            booking.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Bookings.Update(booking);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Trigger waiting list queue check for released ticket types
            if (previousStatus == BookingStatus.Confirmed || previousStatus == BookingStatus.Pending)
            {
                foreach (var item in booking.Items)
                {
                    await _waitingListService.NotifyNextAsync(item.TicketTypeId, cancellationToken);
                }
            }

            return Result.Success();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<BookingDetailDto>> GetBookingDetailAsync(int bookingId, string userId, CancellationToken cancellationToken = default)
    {
        var booking = await _unitOfWork.Bookings.GetByIdWithDetailsAsync(bookingId, cancellationToken);
        if (booking == null)
        {
            return Result<BookingDetailDto>.Failure("Booking not found.");
        }

        // Verify access: user can only view their own bookings
        if (booking.UserId != userId)
        {
            return Result<BookingDetailDto>.Failure("You are not authorized to view this booking.");
        }

        var dto = _mapper.Map<BookingDetailDto>(booking);
        return Result<BookingDetailDto>.Success(dto);
    }

    public async Task<PagedResult<BookingSummaryDto>> GetUserBookingsAsync(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var pagedBookings = await _unitOfWork.Bookings.GetByUserIdPagedAsync(userId, pageNumber, pageSize, cancellationToken);
        var mappedData = pagedBookings.Data.Select(b => _mapper.Map<BookingSummaryDto>(b)).ToList();

        return PagedResult<BookingSummaryDto>.Create(
            mappedData,
            pagedBookings.TotalCount,
            pagedBookings.PageNumber,
            pagedBookings.PageSize
        );
    }

    public async Task<int> ExpirePendingBookingsAsync(CancellationToken cancellationToken = default)
    {
        var timeout = DateTime.UtcNow.AddMinutes(-15);
        var expiredBookings = await _unitOfWork.Bookings.GetQuery()
            .Where(b => b.Status == BookingStatus.Pending && b.BookingDate < timeout)
            .Include(b => b.Items)
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var booking in expiredBookings)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                booking.Status = BookingStatus.Cancelled;
                booking.CancellationReason = "Payment session expired (15 minutes timeout).";
                booking.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Bookings.Update(booking);

                // Release capacity
                var eventWithTicketTypes = await _unitOfWork.Events.GetByIdWithTicketTypesAsync(booking.EventId, cancellationToken);
                if (eventWithTicketTypes != null)
                {
                    foreach (var item in booking.Items)
                    {
                        var ticketType = eventWithTicketTypes.TicketTypes.FirstOrDefault(tt => tt.Id == item.TicketTypeId);
                        if (ticketType != null)
                        {
                            ticketType.SoldQuantity = Math.Max(0, ticketType.SoldQuantity - item.Quantity);
                            _unitOfWork.TicketTypes.Update(ticketType);
                        }
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                count++;

                // Notify waitlist if capacity is released
                foreach (var item in booking.Items)
                {
                    await _waitingListService.NotifyNextAsync(item.TicketTypeId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to expire booking {BookingId}", booking.Id);
            }
        }
        return count;
    }
}
