namespace EventifyPro.BLL.Services.Implementations;

/// <summary>
/// Service implementation for managing payment refund operations.
/// </summary>
public class RefundService : IRefundService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IOutboxService _outboxService;
    private readonly IWaitingListService _waitingListService;

    public RefundService(IUnitOfWork unitOfWork, IMapper mapper, IOutboxService outboxService, IWaitingListService waitingListService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _outboxService = outboxService;
        _waitingListService = waitingListService;
    }

    /// <inheritdoc />
    public async Task<Result<RefundResponseDto>> InitiateAsync(RefundCreateDto dto, string initiatedByUserId, CancellationToken cancellationToken = default)
    {
        // 1. Validate payment exists
        var payment = await _unitOfWork.Payments.GetByIdAsync(dto.PaymentId, cancellationToken);
        if (payment is null)
            return Result<RefundResponseDto>.Failure("Payment not found.");

        if (payment.Status != PaymentStatus.Completed)
            return Result<RefundResponseDto>.Failure("Can only refund completed payments.");

        // 2. Check total already refunded doesn't exceed payment amount
        var totalRefunded = await GetTotalRefundedInternalAsync(dto.PaymentId, cancellationToken);
        if (totalRefunded + dto.Amount > payment.Amount)
            return Result<RefundResponseDto>.Failure($"Refund amount exceeds remaining refundable amount. Max refundable: {payment.Amount - totalRefunded:F2}");

        // 3. Create refund record
        var refund = new Refund
        {
            PaymentId = dto.PaymentId,
            BookingId = payment.BookingId,
            Amount = dto.Amount,
            Status = RefundStatus.Pending,
            Reason = dto.Reason,
            InitiatedById = initiatedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Refunds.AddAsync(refund, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Mark as completed (Paymob void/refund API can be integrated here)
        // For now, we mark the refund as completed locally.
        // In production, you would call Paymob's void_refund API endpoint:
        // POST {BaseUrl}/api/acceptance/void_refund/
        // with auth_token, transaction_id, and amount_cents
        refund.Status = RefundStatus.Completed;
        refund.ProcessedAt = DateTime.UtcNow;
        _unitOfWork.Refunds.Update(refund);

        // 5. Update payment status if fully refunded
        var newTotalRefunded = totalRefunded + dto.Amount;
        var booking = await _unitOfWork.Bookings.GetByIdWithDetailsAsync(payment.BookingId, cancellationToken);
        if (booking is not null)
        {
            if (newTotalRefunded >= payment.Amount)
            {
                payment.Status = PaymentStatus.Refunded;
                payment.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Payments.Update(payment);

                booking.Status = BookingStatus.Refunded;
                _unitOfWork.Bookings.Update(booking);

                // Release ticket capacity and update SoldQuantity in TicketTypes
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

            // Add Refund Status Notification
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(booking.EventId, cancellationToken);
            var eventTitle = eventEntity?.Title ?? "your event";
            var refundNotification = new Notification
            {
                UserId = booking.UserId,
                Title = "Refund Processed",
                Message = $"Refund of {dto.Amount:N2} EGP for '{eventTitle}' has been processed back to your original payment method.",
                Type = NotificationType.RefundStatus,
                IsRead = false,
                RedirectUrl = "/Attendee/Bookings",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Notifications.AddAsync(refundNotification, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (newTotalRefunded >= payment.Amount && booking is not null)
        {
            foreach (var item in booking.Items)
            {
                await _waitingListService.NotifyNextAsync(item.TicketTypeId, cancellationToken);
            }
        }

        return Result<RefundResponseDto>.Success(_mapper.Map<RefundResponseDto>(refund));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RefundResponseDto>>> GetByBookingAsync(int bookingId, string userId, CancellationToken cancellationToken = default)
    {
        var refunds = await _unitOfWork.Refunds.FindAsync(
            r => r.BookingId == bookingId, cancellationToken);

        var refundList = refunds.ToList();
        var result = _mapper.Map<IReadOnlyList<RefundResponseDto>>(refundList);
        return Result<IReadOnlyList<RefundResponseDto>>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<decimal>> GetTotalRefundedAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        var total = await GetTotalRefundedInternalAsync(paymentId, cancellationToken);
        return Result<decimal>.Success(total);
    }

    /// <inheritdoc />
    public async Task<Result<RefundInitiationDetailsDto>> GetRefundInitiationDetailsAsync(int bookingId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = await _unitOfWork.Bookings.GetByIdWithDetailsAsync(bookingId, cancellationToken);
            if (booking == null || booking.UserId != userId)
            {
                return Result<RefundInitiationDetailsDto>.Failure("Booking not found.");
            }

            if (booking.Payment == null)
            {
                return Result<RefundInitiationDetailsDto>.Failure("No completed payment associated with this booking.");
            }

            if (booking.Payment.Status != PaymentStatus.Completed)
            {
                return Result<RefundInitiationDetailsDto>.Failure("Only completed payments can be refunded.");
            }

            var totalRefunded = await GetTotalRefundedInternalAsync(booking.Payment.Id, cancellationToken);
            var maxRefundable = booking.Payment.Amount - totalRefunded;

            if (maxRefundable <= 0)
            {
                return Result<RefundInitiationDetailsDto>.Failure("This booking has already been fully refunded.");
            }

            var dto = new RefundInitiationDetailsDto
            {
                BookingId = bookingId,
                PaymentId = booking.Payment.Id,
                MaxRefundableAmount = maxRefundable
            };

            return Result<RefundInitiationDetailsDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<RefundInitiationDetailsDto>.Failure($"Failed to load refund details: {ex.Message}");
        }
    }

    private async Task<decimal> GetTotalRefundedInternalAsync(int paymentId, CancellationToken cancellationToken)
    {
        var refunds = await _unitOfWork.Refunds.FindAsync(
            r => r.PaymentId == paymentId && r.Status == RefundStatus.Completed, cancellationToken);
        return refunds.Sum(r => r.Amount);
    }
}


