namespace EventifyPro.BLL.Services.Implementations;

public class EventService : IEventService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IUploadHelper _uploadHelper;
    private readonly IOutboxService _outboxService;
    private readonly IRefundService _refundService;
    private readonly ICacheInvalidationService _cacheInvalidationService;
    private readonly IValidator<EventCreateDto> _createValidator;
    private readonly IValidator<EventUpdateDto> _updateValidator;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<EventifyPro.BLL.Hubs.NotificationHub> _hubContext;

    public EventService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUploadHelper uploadHelper,
        IOutboxService outboxService,
        IRefundService refundService,
        ICacheInvalidationService cacheInvalidationService,
        IValidator<EventCreateDto> createValidator,
        IValidator<EventUpdateDto> updateValidator,
        Microsoft.AspNetCore.SignalR.IHubContext<EventifyPro.BLL.Hubs.NotificationHub> hubContext)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _uploadHelper = uploadHelper;
        _outboxService = outboxService;
        _refundService = refundService;
        _cacheInvalidationService = cacheInvalidationService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _hubContext = hubContext;
    }

    public async Task<Result<EventDetailDto>> CreateAsync(EventCreateDto dto, string organizerId, CancellationToken cancellationToken = default)
    {
        var validationError = await _createValidator.GetValidationErrorAsync(dto, cancellationToken);
        if (validationError is not null)
        {
            return Result<EventDetailDto>.Failure(validationError);
        }

        // Validate Category exists
        var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result<EventDetailDto>.Failure("Category not found.");
        }

        if (dto.StartDate <= DateTime.UtcNow)
        {
            return Result<EventDetailDto>.Failure("Event start date must be in the future.");
        }

        if (dto.StartDate >= dto.EndDate)
        {
            return Result<EventDetailDto>.Failure("Event start date must be before the end date.");
        }

        // Map DTO to Event entity
        var eventEntity = _mapper.Map<Event>(dto);
        eventEntity.OrganizerId = organizerId;
        eventEntity.Status = EventStatus.PendingReview;
        eventEntity.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.Events.AddAsync(eventEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);

        // Fetch detailed Event to return
        var details = await _unitOfWork.Events.GetByIdWithDetailsAsync(eventEntity.Id, cancellationToken);
        var resultDto = _mapper.Map<EventDetailDto>(details!);
        return Result<EventDetailDto>.Success(resultDto);
    }

    public async Task<Result<EventDetailDto>> UpdateAsync(int id, EventUpdateDto dto, string organizerId, CancellationToken cancellationToken = default)
    {
        var validationError = await _updateValidator.GetValidationErrorAsync(dto, cancellationToken);
        if (validationError is not null)
        {
            return Result<EventDetailDto>.Failure(validationError);
        }

        var eventEntity = await _unitOfWork.Events.GetByIdWithTicketTypesAsync(id, cancellationToken);
        if (eventEntity == null || eventEntity.IsDeleted)
        {
            return Result<EventDetailDto>.Failure("Event not found.");
        }

        // Validate ownership
        if (eventEntity.OrganizerId != organizerId)
        {
            return Result<EventDetailDto>.Failure("You are not authorized to update this event.");
        }

        if (eventEntity.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            return Result<EventDetailDto>.Failure("Cannot update a cancelled or completed event.");
        }

        // Validate that event hasn't started (unless dates are not being changed)
        if (eventEntity.StartDate <= DateTime.UtcNow)
        {
            if (dto.StartDate != eventEntity.StartDate)
            {
                return Result<EventDetailDto>.Failure("Cannot change the start date of an event that has already started.");
            }
            if (dto.EndDate != eventEntity.EndDate)
            {
                return Result<EventDetailDto>.Failure("Cannot change the end date of an event that has already started.");
            }
        }

        // Validate Category exists
        var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result<EventDetailDto>.Failure("Category not found.");
        }

        if (dto.StartDate >= dto.EndDate)
        {
            return Result<EventDetailDto>.Failure("Event start date must be before the end date.");
        }

        // Validate MaxCapacity isn't reduced below currently sold tickets or total ticket types capacity
        var currentSold = eventEntity.TicketTypes.Sum(tt => tt.SoldQuantity);
        if (dto.MaxCapacity.HasValue && dto.MaxCapacity.Value < currentSold)
        {
            return Result<EventDetailDto>.Failure($"Cannot reduce capacity below currently sold tickets ({currentSold}).");
        }

        var totalTicketCapacity = eventEntity.TicketTypes.Sum(tt => tt.TotalQuantity);
        if (dto.MaxCapacity.HasValue && dto.MaxCapacity.Value < totalTicketCapacity)
        {
            return Result<EventDetailDto>.Failure($"Cannot reduce capacity below the sum of all ticket types' total capacities ({totalTicketCapacity}).");
        }

        // Map updates to entity (preserving created properties and image url if not updated)
        string? oldImageUrl = eventEntity.ImageUrl;
        bool dateOrLocationChanged = eventEntity.StartDate != dto.StartDate ||
                                     eventEntity.EndDate != dto.EndDate ||
                                     eventEntity.Location != dto.Location ||
                                     eventEntity.City != dto.City;

        _mapper.Map(dto, eventEntity);

        if (string.IsNullOrWhiteSpace(dto.ImageUrl))
        {
            eventEntity.ImageUrl = oldImageUrl; // Keep previous cover image
        }

        eventEntity.UpdatedAt = DateTime.UtcNow;
        eventEntity.Status = EventStatus.PendingReview;
        eventEntity.ReviewNotes = null;
        eventEntity.ReviewedByAdminId = null;
        eventEntity.ReviewedAt = null;
        _unitOfWork.Events.Update(eventEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (dateOrLocationChanged)
        {
            try
            {
                var attendeeIds = await _unitOfWork.Bookings.GetQuery()
                    .Where(b => b.EventId == id && b.Status == BookingStatus.Confirmed)
                    .Select(b => b.UserId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                foreach (var attendeeId in attendeeIds)
                {
                    var notification = new Notification
                    {
                        UserId = attendeeId,
                        Title = "Event Updated",
                        Message = $"The event '{eventEntity.Title}' has updated details. Date: {eventEntity.StartDate:g}, Location: {eventEntity.Location}, {eventEntity.City}.",
                        Type = NotificationType.CustomAlert,
                        IsRead = false,
                        RedirectUrl = $"/Events/Details/{eventEntity.Id}",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);

                    await _hubContext.Clients.Group($"attendee_{attendeeId.ToLowerInvariant()}")
                        .SendAsync("ReceiveEventUpdated", new
                        {
                            EventId = eventEntity.Id,
                            EventTitle = eventEntity.Title,
                            Message = $"The event '{eventEntity.Title}' date or location has been updated!"
                        }, cancellationToken);
                }
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception)
            {
                // Silence errors to ensure update succeeds
            }
        }

        await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);

        // Fetch and return detailed event
        var details = await _unitOfWork.Events.GetByIdWithDetailsAsync(id, cancellationToken);
        var resultDto = _mapper.Map<EventDetailDto>(details!);
        return Result<EventDetailDto>.Success(resultDto);
    }

    public async Task<Result> PublishAsync(int id, string organizerId, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _unitOfWork.Events.GetByIdWithDetailsAsync(id, cancellationToken);
        if (eventEntity == null || eventEntity.IsDeleted)
        {
            return Result.Failure("Event not found.");
        }

        // Validate ownership
        if (eventEntity.OrganizerId != organizerId)
        {
            return Result.Failure("You are not authorized to publish this event.");
        }

        if (eventEntity.TicketTypes == null || !eventEntity.TicketTypes.Any())
        {
            return Result.Failure(Eventify.Shared.Constants.ErrorMessages.Event.RequiresTicketType);
        }

        return Result.Failure("Events cannot be published directly by organizers. Admin approval is required.");
    }

    public async Task<Result> ApproveAsync(int id, string adminId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adminId))
        {
            return Result.Failure("Admin ID is required.");
        }

        var eventEntity = await _unitOfWork.Events.GetByIdWithDetailsAsync(id, cancellationToken);
        if (eventEntity == null || eventEntity.IsDeleted)
        {
            return Result.Failure("Event not found.");
        }

        if (eventEntity.Status == EventStatus.Published)
        {
            return Result.Success();
        }

        if (eventEntity.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            return Result.Failure("Cannot approve a cancelled or completed event.");
        }

        if (eventEntity.TicketTypes == null || !eventEntity.TicketTypes.Any())
        {
            return Result.Failure(Eventify.Shared.Constants.ErrorMessages.Event.RequiresTicketType);
        }

        eventEntity.Status = EventStatus.Published;
        eventEntity.ReviewNotes = null;
        eventEntity.ReviewedByAdminId = adminId;
        eventEntity.ReviewedAt = DateTime.UtcNow;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Events.Update(eventEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);

        await EnqueueOrganizerApprovalEmailAsync(eventEntity, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RejectAsync(int id, string adminId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adminId))
        {
            return Result.Failure("Admin ID is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure("Rejection reason is required.");
        }

        var sanitizedReason = reason.Trim();
        if (sanitizedReason.Length > 1000)
        {
            return Result.Failure("Rejection reason cannot exceed 1000 characters.");
        }

        var eventEntity = await _unitOfWork.Events.GetByIdWithDetailsAsync(id, cancellationToken);
        if (eventEntity == null || eventEntity.IsDeleted)
        {
            return Result.Failure("Event not found.");
        }

        if (eventEntity.Status is EventStatus.Cancelled or EventStatus.Completed)
        {
            return Result.Failure("Cannot reject a cancelled or completed event.");
        }

        eventEntity.Status = EventStatus.Rejected;
        eventEntity.ReviewNotes = sanitizedReason;
        eventEntity.ReviewedByAdminId = adminId;
        eventEntity.ReviewedAt = DateTime.UtcNow;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Events.Update(eventEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);

        await EnqueueOrganizerRejectionEmailAsync(eventEntity, sanitizedReason, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<PagedResult<EventSummaryDto>>> GetPendingReviewAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var pagedEvents = await _unitOfWork.Events.GetPendingReviewAsync(pageNumber, pageSize, cancellationToken);
        var mappedData = pagedEvents.Data.Select(e => _mapper.Map<EventSummaryDto>(e)).ToList();

        var result = PagedResult<EventSummaryDto>.Create(mappedData, pagedEvents.TotalCount, pagedEvents.PageNumber, pagedEvents.PageSize);
        return Result<PagedResult<EventSummaryDto>>.Success(result);
    }

    public async Task<Result> CancelAsync(int id, string organizerId, string reason, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var eventEntity = await _unitOfWork.Events.GetByIdWithDetailsAsync(id, cancellationToken);
            if (eventEntity == null || eventEntity.IsDeleted)
            {
                return Result.Failure("Event not found.");
            }

            // Validate ownership
            if (eventEntity.OrganizerId != organizerId)
            {
                return Result.Failure("You are not authorized to cancel this event.");
            }

            if (eventEntity.Status == EventStatus.Cancelled)
            {
                await transaction.CommitAsync(cancellationToken);
                return Result.Success(); // Idempotent
            }

            // Update status
            eventEntity.Status = EventStatus.Cancelled;
            eventEntity.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Events.Update(eventEntity);

            // Fetch all bookings (Confirmed and Pending) to cancel and refund
            var bookingsToCancel = await _unitOfWork.Bookings.GetQuery()
                .Where(b => b.EventId == id && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending))
                .Include(b => b.Items)
                    .ThenInclude(i => i.TicketType)
                .ToListAsync(cancellationToken);

            foreach (var booking in bookingsToCancel)
            {
                if (booking.Status == BookingStatus.Confirmed)
                {
                    // Fetch completed payment
                    var payment = await _unitOfWork.Payments.GetPaymentByBookingAsync(booking.Id, cancellationToken);
                    if (payment != null && payment.Status == PaymentStatus.Completed)
                    {
                        // Refund payment fully (InitiateAsync will mark booking as Refunded and release capacity)
                        var refundResult = await _refundService.InitiateAsync(
                            new RefundCreateDto
                            {
                                PaymentId = payment.Id,
                                Amount = payment.Amount,
                                Reason = $"Event cancellation by organizer. Event: {eventEntity.Title}. Reason: {reason}"
                            },
                            organizerId,
                            cancellationToken
                        );

                        if (refundResult.IsFailure)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            return Result.Failure($"Failed to cancel event due to booking refund error: {refundResult.Error}");
                        }
                    }
                    else
                    {
                        // Confirmed with no completed payment, cancel booking and release capacity
                        booking.Status = BookingStatus.Cancelled;
                        booking.CancellationReason = $"Event cancelled by organizer. Reason: {reason}";
                        booking.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Bookings.Update(booking);

                        foreach (var item in booking.Items)
                        {
                            if (item.TicketType != null)
                            {
                                item.TicketType.SoldQuantity = Math.Max(0, item.TicketType.SoldQuantity - item.Quantity);
                                _unitOfWork.TicketTypes.Update(item.TicketType);
                            }
                        }
                    }
                }
                else if (booking.Status == BookingStatus.Pending)
                {
                    // Cancel pending booking and release capacity
                    booking.Status = BookingStatus.Cancelled;
                    booking.CancellationReason = $"Event cancelled by organizer. Reason: {reason}";
                    booking.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Bookings.Update(booking);

                    foreach (var item in booking.Items)
                    {
                        if (item.TicketType != null)
                        {
                            item.TicketType.SoldQuantity = Math.Max(0, item.TicketType.SoldQuantity - item.Quantity);
                            _unitOfWork.TicketTypes.Update(item.TicketType);
                        }
                    }
                }

                // Add dashboard notification
                var cancellationNotification = new Notification
                {
                    UserId = booking.UserId,
                    Title = "Event Cancelled",
                    Message = $"The event '{eventEntity.Title}' has been cancelled by the organizer. Reason: {reason}",
                    Type = NotificationType.CustomAlert,
                    IsRead = false,
                    RedirectUrl = "/Attendee/Bookings",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Notifications.AddAsync(cancellationNotification, cancellationToken);

                try
                {
                    await _hubContext.Clients.Group($"attendee_{booking.UserId.ToLowerInvariant()}")
                        .SendAsync("ReceiveEventCancelled", new
                        {
                            EventId = eventEntity.Id,
                            EventTitle = eventEntity.Title,
                            Message = $"The event '{eventEntity.Title}' has been cancelled: {reason}"
                        }, cancellationToken);
                }
                catch (Exception)
                {
                    // Ignore SignalR errors in loop
                }

                // Enqueue Event Cancellation notification email to attendee
                var attendee = await _unitOfWork.Users.GetByIdAsync(booking.UserId, cancellationToken);
                if (attendee != null)
                {
                    await _outboxService.EnqueueAsync(
                        "Email.EventCancelled",
                        new OutboxService.EventCancelledPayload
                        {
                            RecipientEmail = attendee.Email ?? string.Empty,
                            RecipientName = attendee.FullName,
                            EventTitle = eventEntity.Title,
                            Reason = reason
                        },
                        cancellationToken
                    );
                }
            }

            // Reset SoldQuantity for all ticket types since event is cancelled
            foreach (var ticketType in eventEntity.TicketTypes)
            {
                ticketType.SoldQuantity = 0;
                _unitOfWork.TicketTypes.Update(ticketType);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result> CancelByAdminAsync(int id, string adminId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adminId))
        {
            return Result.Failure("Admin ID is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure("Cancellation reason is required.");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var eventEntity = await _unitOfWork.Events.GetByIdWithDetailsAsync(id, cancellationToken);
            if (eventEntity == null || eventEntity.IsDeleted)
            {
                return Result.Failure("Event not found.");
            }

            if (eventEntity.Status == EventStatus.Cancelled)
            {
                await transaction.CommitAsync(cancellationToken);
                return Result.Success(); // Idempotent
            }

            // Update status
            eventEntity.Status = EventStatus.Cancelled;
            eventEntity.ReviewNotes = reason;
            eventEntity.ReviewedByAdminId = adminId;
            eventEntity.ReviewedAt = DateTime.UtcNow;
            eventEntity.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Events.Update(eventEntity);

            // Fetch all bookings (Confirmed and Pending) to cancel and refund
            var bookingsToCancel = await _unitOfWork.Bookings.GetQuery()
                .Where(b => b.EventId == id && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending))
                .Include(b => b.Items)
                    .ThenInclude(i => i.TicketType)
                .ToListAsync(cancellationToken);

            foreach (var booking in bookingsToCancel)
            {
                if (booking.Status == BookingStatus.Confirmed)
                {
                    // Fetch completed payment
                    var payment = await _unitOfWork.Payments.GetPaymentByBookingAsync(booking.Id, cancellationToken);
                    if (payment != null && payment.Status == PaymentStatus.Completed)
                    {
                        // Refund payment fully (InitiateAsync will mark booking as Refunded and release capacity)
                        var refundResult = await _refundService.InitiateAsync(
                            new RefundCreateDto
                            {
                                PaymentId = payment.Id,
                                Amount = payment.Amount,
                                Reason = $"Event cancellation by Administrator. Event: {eventEntity.Title}. Reason: {reason}"
                            },
                            adminId,
                            cancellationToken
                        );

                        if (refundResult.IsFailure)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            return Result.Failure($"Failed to cancel event due to booking refund error: {refundResult.Error}");
                        }
                    }
                    else
                    {
                        // Confirmed with no completed payment, cancel booking and release capacity
                        booking.Status = BookingStatus.Cancelled;
                        booking.CancellationReason = $"Event cancelled by Administrator. Reason: {reason}";
                        booking.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Bookings.Update(booking);

                        foreach (var item in booking.Items)
                        {
                            if (item.TicketType != null)
                            {
                                item.TicketType.SoldQuantity = Math.Max(0, item.TicketType.SoldQuantity - item.Quantity);
                                _unitOfWork.TicketTypes.Update(item.TicketType);
                            }
                        }
                    }
                }
                else if (booking.Status == BookingStatus.Pending)
                {
                    // Cancel pending booking and release capacity
                    booking.Status = BookingStatus.Cancelled;
                    booking.CancellationReason = $"Event cancelled by Administrator. Reason: {reason}";
                    booking.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Bookings.Update(booking);

                    foreach (var item in booking.Items)
                    {
                        if (item.TicketType != null)
                        {
                            item.TicketType.SoldQuantity = Math.Max(0, item.TicketType.SoldQuantity - item.Quantity);
                            _unitOfWork.TicketTypes.Update(item.TicketType);
                        }
                    }
                }

                // Add dashboard notification
                var cancellationNotification = new Notification
                {
                    UserId = booking.UserId,
                    Title = "Event Cancelled",
                    Message = $"The event '{eventEntity.Title}' has been cancelled by the Administrator. Reason: {reason}",
                    Type = NotificationType.CustomAlert,
                    IsRead = false,
                    RedirectUrl = "/Attendee/Bookings",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Notifications.AddAsync(cancellationNotification, cancellationToken);

                try
                {
                    await _hubContext.Clients.Group($"attendee_{booking.UserId.ToLowerInvariant()}")
                        .SendAsync("ReceiveEventCancelled", new
                        {
                            EventId = eventEntity.Id,
                            EventTitle = eventEntity.Title,
                            Message = $"The event '{eventEntity.Title}' has been cancelled by the Administrator: {reason}"
                        }, cancellationToken);
                }
                catch (Exception)
                {
                    // Ignore SignalR errors in loop
                }

                // Enqueue Event Cancellation notification email to attendee
                var attendee = await _unitOfWork.Users.GetByIdAsync(booking.UserId, cancellationToken);
                if (attendee != null)
                {
                    await _outboxService.EnqueueAsync(
                        "Email.EventCancelled",
                        new OutboxService.EventCancelledPayload
                        {
                            RecipientEmail = attendee.Email ?? string.Empty,
                            RecipientName = attendee.FullName,
                            EventTitle = eventEntity.Title,
                            Reason = reason
                        },
                        cancellationToken
                    );
                }
            }

            // Reset SoldQuantity for all ticket types since event is cancelled
            foreach (var ticketType in eventEntity.TicketTypes)
            {
                ticketType.SoldQuantity = 0;
                _unitOfWork.TicketTypes.Update(ticketType);
            }

            // Notify Organizer
            if (eventEntity.Organizer != null)
            {
                await _outboxService.EnqueueAsync(
                    "Email.OrganizerEventRejected",
                    new OutboxService.OrganizerEmailWithReasonPayload
                    {
                        RecipientEmail = eventEntity.Organizer.Email ?? string.Empty,
                        RecipientName = eventEntity.Organizer.FullName,
                        EventTitle = eventEntity.Title,
                        Reason = $"Your event was cancelled by the Administrator. Reason: {reason}"
                    },
                    cancellationToken
                );
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Result<EventDetailDto>> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _unitOfWork.Events.GetByIdWithDetailsAsync(id, cancellationToken);
        if (eventEntity == null || eventEntity.IsDeleted)
        {
            return Result<EventDetailDto>.Failure("Event not found.");
        }

        var dto = _mapper.Map<EventDetailDto>(eventEntity);
        return Result<EventDetailDto>.Success(dto);
    }

    public async Task<PagedResult<EventSummaryDto>> SearchAsync(EventFilterDto filter, CancellationToken cancellationToken = default)
    {
        var pagedEvents = await _unitOfWork.Events.GetPublishedPagedAsync(
            filter.Title,
            filter.CategoryId,
            filter.City,
            filter.StartDateFrom,
            filter.StartDateTo,
            filter.PageNumber,
            filter.PageSize,
            filter.IsFeatured,
            cancellationToken
        );

        var mappedData = pagedEvents.Data.Select(e => {
            var dto = _mapper.Map<EventSummaryDto>(e);
            return dto with { MinPrice = e.TicketTypes.Any() ? e.TicketTypes.Min(t => t.Price) : 0 };
        }).ToList();

        return PagedResult<EventSummaryDto>.Create(
            mappedData,
            pagedEvents.TotalCount,
            pagedEvents.PageNumber,
            pagedEvents.PageSize
        );
    }

    public async Task<Result> DeleteAsync(int id, string organizerId, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _unitOfWork.Events.GetByIdWithTicketTypesAsync(id, cancellationToken);
        if (eventEntity == null || eventEntity.IsDeleted)
        {
            return Result.Failure("Event not found.");
        }

        // Validate ownership
        if (eventEntity.OrganizerId != organizerId)
        {
            return Result.Failure("You are not authorized to delete this event.");
        }

        // Check if there are sold tickets
        var hasSoldTickets = eventEntity.TicketTypes.Any(tt => tt.SoldQuantity > 0);
        if (hasSoldTickets)
        {
            return Result.Failure("Cannot delete this event because tickets have already been sold. You must cancel the event first to refund the buyers.");
        }

        // Soft-delete the event
        _unitOfWork.Events.SoftDelete(eventEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RestoreAsync(int id, string organizerId, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _unitOfWork.Events.GetQuery()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == organizerId, cancellationToken);

        if (eventEntity == null)
        {
            return Result.Failure("Event not found.");
        }

        if (!eventEntity.IsDeleted)
        {
            return Result.Failure("Event is not deleted.");
        }

        eventEntity.IsDeleted = false;
        eventEntity.Status = EventStatus.PendingReview; // Requires re-review upon restoration
        eventEntity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Events.Update(eventEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheInvalidationService.InvalidateEventCacheAsync(cancellationToken);

        return Result.Success();
    }

    private async Task EnqueueOrganizerApprovalEmailAsync(Event eventEntity, CancellationToken cancellationToken)
    {
        if (eventEntity.Organizer == null)
        {
            return;
        }

        await _outboxService.EnqueueAsync(
            "Email.OrganizerEventApproved",
            new OutboxService.OrganizerEmailPayload
            {
                RecipientEmail = eventEntity.Organizer.Email ?? string.Empty,
                RecipientName = eventEntity.Organizer.FullName,
                EventTitle = eventEntity.Title
            },
            cancellationToken);
    }

    private async Task EnqueueOrganizerRejectionEmailAsync(Event eventEntity, string reason, CancellationToken cancellationToken)
    {
        if (eventEntity.Organizer == null)
        {
            return;
        }

        await _outboxService.EnqueueAsync(
            "Email.OrganizerEventRejected",
            new OutboxService.OrganizerEmailWithReasonPayload
            {
                RecipientEmail = eventEntity.Organizer.Email ?? string.Empty,
                RecipientName = eventEntity.Organizer.FullName,
                EventTitle = eventEntity.Title,
                Reason = reason
            },
            cancellationToken);
    }

    public async Task<Result<EventPerformanceDto>> GetPerformanceAsync(int id, string organizerId, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _unitOfWork.Events.GetByIdWithTicketTypesAsync(id, cancellationToken);
        if (eventEntity == null || eventEntity.IsDeleted)
        {
            return Result<EventPerformanceDto>.Failure("Event not found.");
        }

        if (eventEntity.OrganizerId != organizerId)
        {
            return Result<EventPerformanceDto>.Failure("You are not authorized to view performance for this event.");
        }

        var ticketTypes = eventEntity.TicketTypes.ToList();
        var totalSold = ticketTypes.Sum(tt => tt.SoldQuantity);
        var totalCapacity = ticketTypes.Sum(tt => tt.TotalQuantity);

        var bookingsQuery = _unitOfWork.Bookings.GetQuery()
            .Where(b => b.EventId == id);

        var bookingStats = await bookingsQuery
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var confirmedBookings = bookingStats.FirstOrDefault(s => s.Status == BookingStatus.Confirmed)?.Count ?? 0;
        var pendingBookings = bookingStats.FirstOrDefault(s => s.Status == BookingStatus.Pending)?.Count ?? 0;
        var cancelledBookings = bookingStats.FirstOrDefault(s => s.Status == BookingStatus.Cancelled)?.Count ?? 0;

        var totalRevenue = confirmedBookings > 0
            ? await bookingsQuery
                .Where(b => b.Status == BookingStatus.Confirmed)
                .SumAsync(b => b.TotalAmount, cancellationToken)
            : 0m;

        var waitingListCount = await _unitOfWork.WaitingLists.CountAsync(
            w => w.EventId == id && w.Status == WaitingListStatus.Waiting,
            cancellationToken
        );

        var dto = new EventPerformanceDto
        {
            EventId = id,
            Title = eventEntity.Title,
            TotalRevenue = totalRevenue,
            TotalTicketsSold = totalSold,
            TotalCapacity = totalCapacity,
            SoldPercentage = totalCapacity > 0 ? (double)totalSold / totalCapacity * 100 : 0,
            WaitingListCount = waitingListCount,
            ConfirmedBookings = confirmedBookings,
            PendingBookings = pendingBookings,
            CancelledBookings = cancelledBookings,
            TicketTypes = ticketTypes.Select(tt => new TicketTypePerformanceDto
            {
                Id = tt.Id,
                Name = tt.Name,
                Price = tt.Price,
                TotalQuantity = tt.TotalQuantity,
                SoldQuantity = tt.SoldQuantity,
                SoldPercentage = tt.TotalQuantity > 0 ? (double)tt.SoldQuantity / tt.TotalQuantity * 100 : 0
            }).ToList()
        };

        return Result<EventPerformanceDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<EventSummaryDto>>> GetOrganizerEventsAsync(string organizerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await _unitOfWork.Events.GetQuery()
                .AsNoTracking()
                .Where(e => e.OrganizerId == organizerId && !e.IsDeleted)
                .OrderByDescending(e => e.CreatedAt)
                .ProjectToType<EventSummaryDto>()
                .ToListAsync(cancellationToken);

            return Result<IReadOnlyList<EventSummaryDto>>.Success(events.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<EventSummaryDto>>.Failure($"Failed to retrieve organizer events: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<EventSummaryDto>>> GetOrganizerEventsPagedAsync(
        string organizerId,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _unitOfWork.Events.GetQuery()
                .AsNoTracking()
                .Where(e => e.OrganizerId == organizerId && !e.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var events = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ProjectToType<EventSummaryDto>()
                .ToListAsync(cancellationToken);

            var paged = PagedResult<EventSummaryDto>.Create(events, totalCount, pageNumber, pageSize);
            return Result<PagedResult<EventSummaryDto>>.Success(paged);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<EventSummaryDto>>.Failure($"Failed to retrieve organizer paged events: {ex.Message}");
        }
    }

    public async Task<bool> ExistsByTitleAsync(string title, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var titleTrimmed = title.Trim().ToLower();
        return await _unitOfWork.Events.GetQuery()
            .AsNoTracking()
            .AnyAsync(e => e.Title.ToLower() == titleTrimmed && (!excludeId.HasValue || e.Id != excludeId) && !e.IsDeleted, cancellationToken);
    }

    public async Task<Result<PagedResult<EventAttendeeDto>>> GetEventAttendeesPageAsync(
        int eventId,
        string organizerId,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
            if (eventEntity == null || eventEntity.IsDeleted)
            {
                return Result<PagedResult<EventAttendeeDto>>.Failure("Event not found.");
            }

            if (eventEntity.OrganizerId != organizerId)
            {
                return Result<PagedResult<EventAttendeeDto>>.Failure("You are not authorized to view this event's attendee list.");
            }

            var query = _unitOfWork.Bookings.GetQuery()
                .AsNoTracking()
                .Include(b => b.User)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.TicketType)
                .Where(b => b.EventId == eventId && b.Status == BookingStatus.Confirmed);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                bool isNumeric = int.TryParse(search, out int searchId);

                query = query.Where(b =>
                    (b.User != null && b.User.FullName != null && b.User.FullName.ToLower().Contains(search)) ||
                    (b.User != null && b.User.Email != null && b.User.Email.ToLower().Contains(search)) ||
                    (isNumeric && b.Id == searchId));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var viewModels = bookings.Select(b => new EventAttendeeDto
            {
                BookingId = b.Id,
                AttendeeName = b.User?.FullName ?? "Unknown Attendee",
                AttendeeEmail = b.User?.Email ?? "N/A",
                ProfileImageUrl = b.User?.ProfileImageUrl,
                TotalAmount = b.TotalAmount,
                BookingDate = b.BookingDate,
                Tickets = b.Tickets.Select(t => new EventAttendeeTicketDto
                {
                    TicketId = t.Id,
                    TicketTypeName = t.TicketType?.Name ?? "General",
                    Price = t.TicketType?.Price ?? 0m,
                    IsUsed = t.IsUsed,
                    UsedAt = t.UsedAt
                }).ToList()
            }).ToList();

            var paged = PagedResult<EventAttendeeDto>.Create(viewModels, totalCount, pageNumber, pageSize);
            return Result<PagedResult<EventAttendeeDto>>.Success(paged);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<EventAttendeeDto>>.Failure($"Failed to retrieve event attendees: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<EventAttendeeDto>>> GetEventAttendeesForExportAsync(
        int eventId,
        string organizerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
            if (eventEntity == null || eventEntity.IsDeleted)
            {
                return Result<IReadOnlyList<EventAttendeeDto>>.Failure("Event not found.");
            }

            if (eventEntity.OrganizerId != organizerId)
            {
                return Result<IReadOnlyList<EventAttendeeDto>>.Failure("You are not authorized to view this event's attendee list.");
            }

            var bookings = await _unitOfWork.Bookings.GetQuery()
                .AsNoTracking()
                .Include(b => b.User)
                .Include(b => b.Tickets)
                    .ThenInclude(t => t.TicketType)
                .Where(b => b.EventId == eventId && b.Status == BookingStatus.Confirmed)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync(cancellationToken);

            var viewModels = bookings.Select(b => new EventAttendeeDto
            {
                BookingId = b.Id,
                AttendeeName = b.User?.FullName ?? "Unknown Attendee",
                AttendeeEmail = b.User?.Email ?? "N/A",
                ProfileImageUrl = b.User?.ProfileImageUrl,
                TotalAmount = b.TotalAmount,
                BookingDate = b.BookingDate,
                Tickets = b.Tickets.Select(t => new EventAttendeeTicketDto
                {
                    TicketId = t.Id,
                    TicketTypeName = t.TicketType?.Name ?? "General",
                    Price = t.TicketType?.Price ?? 0m,
                    IsUsed = t.IsUsed,
                    UsedAt = t.UsedAt
                }).ToList()
            }).ToList();

            return Result<IReadOnlyList<EventAttendeeDto>>.Success(viewModels.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<EventAttendeeDto>>.Failure($"Failed to retrieve event attendees for export: {ex.Message}");
        }
    }

    public async Task<Result<PagedResult<EventSummaryDto>>> GetAttendeeEventsPagedAsync(
        string userId,
        string statusFilter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            
            var query = _unitOfWork.Events.GetQuery()
                .AsNoTracking()
                .Where(e => !e.IsDeleted && e.Bookings.Any(b => b.UserId == userId && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending)));

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                var filter = statusFilter.Trim().ToLower();
                if (filter == "upcoming")
                {
                    query = query.Where(e => e.EndDate >= now);
                }
                else if (filter == "finished" || filter == "past")
                {
                    query = query.Where(e => e.EndDate < now);
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var events = await query
                .OrderBy(e => e.StartDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ProjectToType<EventSummaryDto>()
                .ToListAsync(cancellationToken);

            var paged = PagedResult<EventSummaryDto>.Create(events, totalCount, pageNumber, pageSize);
            return Result<PagedResult<EventSummaryDto>>.Success(paged);
        }
        catch (Exception ex)
        {
            return Result<PagedResult<EventSummaryDto>>.Failure($"Failed to retrieve attendee paged events: {ex.Message}");
        }
    }
}


