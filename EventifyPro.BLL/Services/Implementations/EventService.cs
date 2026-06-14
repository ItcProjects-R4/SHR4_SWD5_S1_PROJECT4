using Eventify.Domain.Entities;
using Eventify.Domain.Enums;
using EventifyPro.BLL.DTOs.Event;
using EventifyPro.BLL.Services.Interfaces;
using EventifyPro.DAL.Repositories.Interfaces;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace EventifyPro.BLL.Services.Implementations;

public class EventService : IEventService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IUploadHelper _uploadHelper;
    private readonly IOutboxService _outboxService;
    private readonly IRefundService _refundService;

    public EventService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IUploadHelper uploadHelper,
        IOutboxService outboxService,
        IRefundService refundService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _uploadHelper = uploadHelper;
        _outboxService = outboxService;
        _refundService = refundService;
    }

    public async Task<Result<EventDetailDto>> CreateAsync(EventCreateDto dto, string organizerId, CancellationToken cancellationToken = default)
    {
        // Validate Category exists
        var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result<EventDetailDto>.Failure("Category not found.");
        }

        // Map DTO to Event entity
        var eventEntity = _mapper.Map<Event>(dto);
        eventEntity.OrganizerId = organizerId;
        eventEntity.Status = EventStatus.PendingReview;
        eventEntity.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.Events.AddAsync(eventEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Fetch detailed Event to return
        var details = await _unitOfWork.Events.GetByIdWithDetailsAsync(eventEntity.Id, cancellationToken);
        var resultDto = _mapper.Map<EventDetailDto>(details!);
        return Result<EventDetailDto>.Success(resultDto);
    }

    public async Task<Result<EventDetailDto>> UpdateAsync(int id, EventUpdateDto dto, string organizerId, CancellationToken cancellationToken = default)
    {
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

        // Validate that event hasn't started
        if (eventEntity.StartDate <= DateTime.UtcNow)
        {
            return Result<EventDetailDto>.Failure("Cannot update an event that has already started.");
        }

        // Validate Category exists
        var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result<EventDetailDto>.Failure("Category not found.");
        }

        // Validate MaxCapacity isn't reduced below currently sold tickets
        var currentSold = eventEntity.TicketTypes.Sum(tt => tt.SoldQuantity);
        if (dto.MaxCapacity.HasValue && dto.MaxCapacity.Value < currentSold)
        {
            return Result<EventDetailDto>.Failure($"Cannot reduce capacity below currently sold tickets ({currentSold}).");
        }

        // Map updates to entity (preserving created properties and image url if not updated)
        string? oldImageUrl = eventEntity.ImageUrl;
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

        if (eventEntity.TicketTypes == null || eventEntity.TicketTypes.Count == 0)
        {
            return Result.Failure("Cannot approve an event with no ticket types defined.");
        }

        eventEntity.Status = EventStatus.Published;
        eventEntity.ReviewNotes = null;
        eventEntity.ReviewedByAdminId = adminId;
        eventEntity.ReviewedAt = DateTime.UtcNow;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Events.Update(eventEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        await EnqueueOrganizerRejectionEmailAsync(eventEntity, sanitizedReason, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<EventSummaryDto>>> GetPendingReviewAsync(CancellationToken cancellationToken = default)
    {
        var events = await _unitOfWork.Events.GetPendingReviewAsync(cancellationToken);
        var result = events.Select(e => _mapper.Map<EventSummaryDto>(e)).ToList();

        return Result<IReadOnlyList<EventSummaryDto>>.Success(result);
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

            // Fetch all confirmed bookings to cancel and refund
            var confirmedBookings = await _unitOfWork.Bookings.GetConfirmedByEventAsync(id, cancellationToken);
            foreach (var booking in confirmedBookings)
            {
                // Fetch completed payment
                var payment = await _unitOfWork.Payments.GetPaymentByBookingAsync(booking.Id, cancellationToken);
                if (payment != null && payment.Status == PaymentStatus.Completed)
                {
                    // Refund payment fully
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
                    // No completed payment, just cancel the booking
                    booking.Status = BookingStatus.Cancelled;
                    booking.CancellationReason = $"Event cancelled by organizer. Reason: {reason}";
                    booking.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Bookings.Update(booking);
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
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(id, cancellationToken);
        if (eventEntity == null || eventEntity.IsDeleted)
        {
            return Result.Failure("Event not found.");
        }

        // Validate ownership
        if (eventEntity.OrganizerId != organizerId)
        {
            return Result.Failure("You are not authorized to delete this event.");
        }

        // Soft-delete the event
        eventEntity.IsDeleted = true;
        eventEntity.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Events.Update(eventEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
}
