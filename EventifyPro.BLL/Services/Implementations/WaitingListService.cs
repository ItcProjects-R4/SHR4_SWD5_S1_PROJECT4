using Microsoft.Extensions.Configuration;

namespace EventifyPro.BLL.Services.Implementations;

public class WaitingListService : IWaitingListService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IOutboxService _outboxService;
    private readonly IValidator<WaitingListJoinDto> _joinValidator;
    private readonly IConfiguration _configuration;

    public WaitingListService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IOutboxService outboxService,
        IValidator<WaitingListJoinDto> joinValidator,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _outboxService = outboxService;
        _joinValidator = joinValidator;
        _configuration = configuration;
    }

    public async Task<Result<WaitingListResponseDto>> JoinAsync(WaitingListJoinDto dto, string userId, CancellationToken cancellationToken = default)
    {
        var validationError = await _joinValidator.GetValidationErrorAsync(dto, cancellationToken);
        if (validationError is not null)
            return Result<WaitingListResponseDto>.Failure(validationError);

        // Verify event and ticket type exist
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(dto.EventId, cancellationToken);
        if (eventEntity == null)
            return Result<WaitingListResponseDto>.Failure("Event not found.");

        var ticketType = await _unitOfWork.TicketTypes.GetByIdAsync(dto.TicketTypeId, cancellationToken);
        if (ticketType == null || ticketType.EventId != dto.EventId)
            return Result<WaitingListResponseDto>.Failure("Ticket type not found for this event.");

        // Check if user already has a waiting list entry for this ticket type
        var existingWaitingList = await _unitOfWork.WaitingLists.GetTicketTypeWaitingListAsync(dto.TicketTypeId, cancellationToken);
        if (existingWaitingList.Any(w => w.UserId == userId && w.Status == WaitingListStatus.Waiting))
            return Result<WaitingListResponseDto>.Failure("You are already on the waiting list for this ticket type.");

        // Create waiting list entry
        var waitingListEntry = new WaitingList
        {
            EventId = dto.EventId,
            TicketTypeId = dto.TicketTypeId,
            UserId = userId,
            QuantityWanted = dto.QuantityWanted,
            Status = WaitingListStatus.Waiting,
            JoinedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7) // Expire after 7 days
        };

        await _unitOfWork.WaitingLists.AddAsync(waitingListEntry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get user for email notification
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);

        // Map and return
        var dto_result = _mapper.Map<WaitingListResponseDto>(waitingListEntry);
        dto_result = dto_result with 
        { 
            EventTitle = eventEntity.Title,
            TicketTypeName = ticketType.Name,
            Status = waitingListEntry.Status.ToString()
        };

        return Result<WaitingListResponseDto>.Success(dto_result);
    }

    public async Task<Result> LeaveAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var waitingListEntry = await _unitOfWork.WaitingLists.GetByIdAsync(id, cancellationToken);
        if (waitingListEntry == null)
            return Result.Failure("Waiting list entry not found.");

        // Verify ownership
        if (waitingListEntry.UserId != userId)
            return Result.Failure("You are not authorized to remove this waiting list entry.");

        // Remove entry
        _unitOfWork.WaitingLists.Delete(waitingListEntry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> NotifyNextAsync(int ticketTypeId, CancellationToken cancellationToken = default)
    {
        var waitingList = await _unitOfWork.WaitingLists.GetTicketTypeWaitingListAsync(ticketTypeId, cancellationToken);
        var nextEntry = waitingList
            .Where(w => w.Status == WaitingListStatus.Waiting)
            .OrderBy(w => w.JoinedAt)
            .FirstOrDefault();

        if (nextEntry == null)
            return Result.Success();

        return await NotifyEntryAsync(nextEntry, cancellationToken);
    }

    public async Task<Result> AdvanceQueueAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var ticketTypes = await _unitOfWork.TicketTypes.GetTicketTypesByEventAsync(eventId, cancellationToken);

        foreach (var ticketType in ticketTypes)
        {
            var availableQuantity = ticketType.TotalQuantity - ticketType.SoldQuantity;
            if (availableQuantity <= 0)
                continue;

            var waitingList = await _unitOfWork.WaitingLists.GetTicketTypeWaitingListAsync(ticketType.Id, cancellationToken);
            var waitingUsers = waitingList
                .Where(w => w.Status == WaitingListStatus.Waiting)
                .OrderBy(w => w.JoinedAt)
                .Take(availableQuantity)
                .ToList();

            foreach (var entry in waitingUsers)
            {
                var result = await NotifyEntryAsync(entry, cancellationToken);
                if (result.IsFailure)
                    return result;
            }
        }

        return Result.Success();
    }

    private async Task<Result> NotifyEntryAsync(WaitingList entry, CancellationToken cancellationToken)
    {
        if (entry.Status != WaitingListStatus.Waiting)
            return Result.Success();

        var ticketType = await _unitOfWork.TicketTypes.GetByIdAsync(entry.TicketTypeId, cancellationToken);
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(entry.EventId, cancellationToken);
        var user = await _unitOfWork.Users.GetByIdAsync(entry.UserId, cancellationToken);

        if (ticketType == null || eventEntity == null || user == null)
            return Result.Failure("Could not retrieve notification details.");

        entry.Status = WaitingListStatus.Notified;
        entry.NotifiedAt = DateTime.UtcNow;
        entry.ExpiresAt = DateTime.UtcNow.AddHours(2); // Expire after 2 hours as per AppDefaults / PRD

        _unitOfWork.WaitingLists.Update(entry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7198";
        var claimUrl = $"{baseUrl}/Booking/Create/{entry.EventId}?waitingListId={entry.Id}";

        await _outboxService.EnqueueAsync(
            "Email.WaitingListNotification",
            new OutboxService.WaitingListNotificationPayload
            {
                RecipientEmail = user.Email ?? string.Empty,
                RecipientName = user.FullName,
                EventTitle = eventEntity.Title,
                TicketTypeName = ticketType.Name,
                ClaimUrl = claimUrl,
                ExpiresAt = entry.ExpiresAt.Value
            },
            cancellationToken
        );

        return Result.Success();
    }
}
