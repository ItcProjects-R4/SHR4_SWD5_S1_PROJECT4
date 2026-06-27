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
    private readonly ISystemSettingService _systemSettingService;
    private readonly ILogger<BookingService> _logger;
    private readonly ICacheInvalidationService _cacheInvalidationService;
    private readonly IValidator<BookingCreateDto> _createValidator;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<EventifyPro.BLL.Hubs.NotificationHub> _hubContext;
    private readonly IMemoryCache _cache;

    public BookingService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ITicketService ticketService,
        IOutboxService outboxService,
        IPdfService pdfService,
        IRefundService refundService,
        IWaitingListService waitingListService,
        IConfiguration configuration,
        ISystemSettingService systemSettingService,
        ICacheInvalidationService cacheInvalidationService,
        IValidator<BookingCreateDto> createValidator,
        Microsoft.AspNetCore.SignalR.IHubContext<EventifyPro.BLL.Hubs.NotificationHub> hubContext,
        ILogger<BookingService> logger,
        IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _ticketService = ticketService;
        _outboxService = outboxService;
        _pdfService = pdfService;
        _refundService = refundService;
        _waitingListService = waitingListService;
        _configuration = configuration;
        _systemSettingService = systemSettingService;
        _cacheInvalidationService = cacheInvalidationService;
        _createValidator = createValidator;
        _hubContext = hubContext;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<BookingDetailDto>> CreatePendingAsync(BookingCreateDto dto, string userId, CancellationToken cancellationToken = default)
    {
        var validationError = await _createValidator.GetValidationErrorAsync(dto, cancellationToken);
        if (validationError is not null)
        {
            return Result<BookingDetailDto>.Failure(validationError);
        }

        int retries = 3;
        while (retries > 0)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await CreatePendingInternalAsync(dto, userId, transaction, cancellationToken);
                return result;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);
                _unitOfWork.DbContext.ChangeTracker.Clear();
                retries--;
                if (retries == 0)
                {
                    return Result<BookingDetailDto>.Failure("This booking could not be completed due to high demand. Please try again.");
                }
                int attempt = 3 - retries; // 1, 2
                int delayMs = (int)Math.Pow(2, attempt) * 100; // 200ms, 400ms
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (Exception)
            {
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch
                {
                    // Ignore exception if the transaction is already completed (e.g. if the exception occurred after commit)
                }
                throw;
            }
        }
        return Result<BookingDetailDto>.Failure("Failed to create booking due to concurrent updates.");
    }

    private async Task<Result<BookingDetailDto>> CreatePendingInternalAsync(BookingCreateDto dto, string userId, IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
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

        // Check for existing confirmed booking for this user and event
        var hasConfirmedBooking = await _unitOfWork.Bookings.HasConfirmedBookingAsync(userId, dto.EventId, cancellationToken);
        if (hasConfirmedBooking)
        {
            return Result<BookingDetailDto>.Failure(Eventify.Shared.Constants.ErrorMessages.Booking.DuplicateBooking);
        }

        var maxTicketsPerBooking = await _systemSettingService.GetSettingValueAsync<int>("MaxTicketsPerBooking", 10, cancellationToken);
        var newlyRequestedTotal = dto.Items.Sum(i => i.Quantity);
        if (newlyRequestedTotal > maxTicketsPerBooking)
        {
            return Result<BookingDetailDto>.Failure($"Booking exceeds the maximum ticket limit per transaction. You can purchase at most {maxTicketsPerBooking} ticket(s) at a time.");
        }

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

        foreach (var item in dto.Items)
        {
            var ticketType = eventEntity.TicketTypes.FirstOrDefault(tt => tt.Id == item.TicketTypeId);
            if (ticketType == null)
            {
                return Result<BookingDetailDto>.Failure($"Ticket type ID {item.TicketTypeId} not found for this event.");
            }

            if (ticketType.SaleStartDate.HasValue && ticketType.SaleStartDate.Value > DateTime.UtcNow)
            {
                return Result<BookingDetailDto>.Failure($"Ticket sale for '{ticketType.Name}' has not started yet.");
            }

            if (ticketType.SaleEndDate.HasValue && ticketType.SaleEndDate.Value < DateTime.UtcNow)
            {
                return Result<BookingDetailDto>.Failure($"Ticket sale for '{ticketType.Name}' has ended.");
            }

            // Check waitlist reservations
            var activeNotifications = await _unitOfWork.WaitingLists.GetQuery()
                .AsNoTracking()
                .Where(w => w.TicketTypeId == item.TicketTypeId && w.Status == WaitingListStatus.Notified && w.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            var totalNotifiedReserved = activeNotifications.Sum(w => w.QuantityWanted);

            int userClaimedQuantity = 0;
            WaitingList? userWaitlistEntry = null;
            if (dto.WaitingListId.HasValue)
            {
                userWaitlistEntry = await _unitOfWork.WaitingLists.GetByIdAsync(dto.WaitingListId.Value, cancellationToken);
                if (userWaitlistEntry != null && userWaitlistEntry.UserId == userId && userWaitlistEntry.TicketTypeId == item.TicketTypeId && userWaitlistEntry.Status == WaitingListStatus.Notified && userWaitlistEntry.ExpiresAt > DateTime.UtcNow)
                {
                    userClaimedQuantity = userWaitlistEntry.QuantityWanted;
                }
                else
                {
                    userWaitlistEntry = null;
                }
            }

            var netReservedForOthers = Math.Max(0, totalNotifiedReserved - userClaimedQuantity);

            if (ticketType.SoldQuantity + item.Quantity > ticketType.TotalQuantity - netReservedForOthers)
            {
                return Result<BookingDetailDto>.Failure($"Not enough tickets available for '{ticketType.Name}'. Some tickets are reserved for waitlisted users.");
            }

            if (userWaitlistEntry != null)
            {
                userWaitlistEntry.Status = WaitingListStatus.Converted;
                _unitOfWork.WaitingLists.Update(userWaitlistEntry);
            }

            ticketType.SoldQuantity += item.Quantity;
            _unitOfWork.TicketTypes.Update(ticketType);

            totalAmount += item.Quantity * ticketType.Price;

            bookingItems.Add(new BookingItem
            {
                TicketTypeId = item.TicketTypeId,
                Quantity = item.Quantity,
                UnitPrice = ticketType.Price
            });
        }

        if (eventEntity.MaxCapacity.HasValue)
        {
            var currentSold = eventEntity.TicketTypes.Sum(tt => tt.SoldQuantity);
            if (currentSold > eventEntity.MaxCapacity.Value)
            {
                return Result<BookingDetailDto>.Failure($"Booking exceeds maximum event capacity. Max allowed: {eventEntity.MaxCapacity.Value - (currentSold - dto.Items.Sum(i => i.Quantity))}");
            }
        }

        decimal commissionRate = await _systemSettingService.GetSettingValueAsync<decimal>("TicketCommissionRate", 5.0m, cancellationToken);
        decimal serviceFee = Math.Round(totalAmount * (commissionRate / 100m), 2);
        decimal finalTotalAmount = totalAmount + serviceFee;

        var booking = new Booking
        {
            UserId = userId,
            EventId = dto.EventId,
            TotalAmount = finalTotalAmount,
            ServiceFee = serviceFee,
            Status = BookingStatus.Pending,
            BookingDate = DateTime.UtcNow,
            BookingReference = string.Empty,
            Items = bookingItems
        };

        await _unitOfWork.Bookings.AddAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        booking.BookingReference = BookingReferenceHelper.Generate(booking.Id, booking.BookingDate);
        _unitOfWork.Bookings.Update(booking);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);

        // Clear dashboard caches for instant updates
        _cache.Remove($"AttendeeDashboard_{userId}");
        _cache.Remove($"OrganizerDashboard_{eventEntity.OrganizerId}");

        var resultDto = _mapper.Map<BookingDetailDto>(booking);
        return Result<BookingDetailDto>.Success(resultDto);
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
                        Currency = "EGP",
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
                    var generatedTickets = ticketResult.Data!;
                    foreach (var ticket in generatedTickets)
                    {
                        byte[] pdfBytes = [];
                        try
                        {
                            pdfBytes = await _pdfService.GenerateTicketPdfAsync(ticket.Id, cancellationToken);
                        }
                        catch (Exception pdfEx)
                        {
                            _logger.LogError(pdfEx, "Failed to generate PDF for ticket {TicketId}. Proceeding without PDF attachment.", ticket.Id);
                        }

                        // Enqueue Booking Confirmation Email to transactional outbox for each ticket
                        await _outboxService.EnqueueAsync(
                            "Email.TicketConfirmation",
                            new OutboxService.TicketConfirmationPayload
                            {
                                RecipientEmail = user.Email ?? string.Empty,
                                RecipientName = user.FullName,
                                BookingRef = $"{booking.BookingReference}-{ticket.Id}",
                                PdfAttachmentBase64 = pdfBytes.Length > 0 ? Convert.ToBase64String(pdfBytes) : string.Empty
                            },
                            cancellationToken
                        );
                    }
                }

                // Add Booking Confirmed Notification
                var bookingNotification = new Notification
                {
                    UserId = booking.UserId,
                    Title = "Booking Confirmed",
                    Message = $"Your booking for '{eventEntity.Title}' has been successfully confirmed! Ticket QR code is ready.",
                    Type = NotificationType.BookingConfirmed,
                    IsRead = false,
                    RedirectUrl = "/Attendee/Dashboard",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Notifications.AddAsync(bookingNotification, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                try
                {
                    await _hubContext.Clients.Group($"organizer_{eventEntity.OrganizerId.ToLowerInvariant()}")
                        .SendAsync("ReceiveTicketBooked", new
                        {
                            EventId = eventEntity.Id,
                            EventTitle = eventEntity.Title,
                            BookingId = booking.Id,
                            Quantity = booking.Items.Sum(i => i.Quantity),
                            TotalAmount = booking.TotalAmount,
                            AttendeeName = user?.FullName ?? "Someone"
                        }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send SignalR notification for booking confirmation");
                }

                // Clear dashboard caches for instant updates
                _cache.Remove($"AttendeeDashboard_{booking.UserId}");
                _cache.Remove($"OrganizerDashboard_{eventEntity.OrganizerId}");

                return Result.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);
                _unitOfWork.DbContext.ChangeTracker.Clear();
                retries--;
                if (retries == 0)
                {
                    return Result.Failure("Overselling concurrency conflict occurred. Please try booking again.");
                }
                // Back-off briefly before retry with exponential delay
                int attempt = 3 - retries; // 1, 2
                int delayMs = (int)Math.Pow(2, attempt) * 100; // 200ms, 400ms
                await Task.Delay(delayMs, cancellationToken);
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

            if (previousStatus == BookingStatus.Confirmed || previousStatus == BookingStatus.Pending)
            {
                if (previousStatus == BookingStatus.Pending)
                {
                    await ReleaseTicketCapacityAsync(booking, cancellationToken);
                }
                else if (previousStatus == BookingStatus.Confirmed)
                {
                    var refundResult = await RefundBookingPaymentAsync(booking, reason, userId, cancellationToken);
                    if (refundResult.IsFailure)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Result.Failure($"Cancellation failed due to refund error: {refundResult.Error}");
                    }
                }
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = reason;
            booking.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Bookings.Update(booking);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);

            if (previousStatus == BookingStatus.Confirmed || previousStatus == BookingStatus.Pending)
            {
                foreach (var item in booking.Items)
                {
                    await _waitingListService.NotifyNextAsync(item.TicketTypeId, cancellationToken);
                }
            }

            // Clear dashboard caches for instant updates
            _cache.Remove($"AttendeeDashboard_{booking.UserId}");
            _cache.Remove($"OrganizerDashboard_{booking.Event.OrganizerId}");

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

                await ReleaseTicketCapacityAsync(booking, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                count++;

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
        if (count > 0)
        {
            await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);
        }
        return count;
    }

    private async Task ReleaseTicketCapacityAsync(Booking booking, CancellationToken cancellationToken)
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
    }

    private async Task<Result> RefundBookingPaymentAsync(Booking booking, string reason, string userId, CancellationToken cancellationToken)
    {
        if (booking.Status != BookingStatus.Confirmed)
            return Result.Success();

        var payment = await _unitOfWork.Payments.GetPaymentByBookingAsync(booking.Id, cancellationToken);
        if (payment == null || payment.Status != PaymentStatus.Completed)
            return Result.Success();

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
            return Result.Failure(refundResult.Error!);

        return Result.Success();
    }
}
