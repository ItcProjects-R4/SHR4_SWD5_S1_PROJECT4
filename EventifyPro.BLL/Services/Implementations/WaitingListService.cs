namespace EventifyPro.BLL.Services.Implementations;

public class WaitingListService : IWaitingListService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IOutboxService _outboxService;
    private readonly IValidator<WaitingListJoinDto> _joinValidator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WaitingListService> _logger;

    public WaitingListService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IOutboxService outboxService,
        IValidator<WaitingListJoinDto> joinValidator,
        IConfiguration configuration,
        ILogger<WaitingListService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _outboxService = outboxService;
        _joinValidator = joinValidator;
        _configuration = configuration;
        _logger = logger;
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
        var ticketType = await _unitOfWork.TicketTypes.GetByIdAsync(ticketTypeId, cancellationToken);
        if (ticketType == null)
            return Result.Failure("Ticket type not found.");

        var waitingList = await _unitOfWork.WaitingLists.GetTicketTypeWaitingListAsync(ticketTypeId, cancellationToken);
        var nextEntry = waitingList
            .Where(w => w.Status == WaitingListStatus.Waiting)
            .OrderBy(w => w.JoinedAt)
            .FirstOrDefault();

        if (nextEntry == null)
            return Result.Success();

        // Calculate currently notified and active reservations to prevent overselling waitlist seats
        var activeNotifications = await _unitOfWork.WaitingLists.GetQuery()
            .AsNoTracking()
            .Where(w => w.TicketTypeId == ticketTypeId && w.Status == WaitingListStatus.Notified && w.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        var totalNotifiedReserved = activeNotifications.Sum(w => w.QuantityWanted);

        if (ticketType.SoldQuantity + totalNotifiedReserved + nextEntry.QuantityWanted > ticketType.TotalQuantity)
        {
            return Result.Success(); // Do not notify because capacity would exceed total quantity
        }

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

        var baseUrl = _configuration["BaseUrl"] ?? "https://eventifypro.runasp.net";
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

    public async Task<int> ProcessExpiredNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var expiredEntries = await _unitOfWork.WaitingLists.GetQuery()
            .Where(w => w.Status == WaitingListStatus.Notified && w.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var entry in expiredEntries)
        {
            try
            {
                entry.Status = WaitingListStatus.Expired;
                _unitOfWork.WaitingLists.Update(entry);
                count++;

                // Trigger the next person in line for this ticket type
                await NotifyNextAsync(entry.TicketTypeId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired waiting list entry ID {EntryId}.", entry.Id);
            }
        }

        if (count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return count;
    }

    public async Task<Result<OrganizerWaitingListSummaryDto>> GetOrganizerWaitingListSummaryAsync(
        int? eventId,
        WaitingListStatus? status,
        string? searchTerm,
        string organizerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Base query for waiting list entries belonging to organizer's events
            var baseQuery = _unitOfWork.WaitingLists.GetQuery()
                .Where(w => w.Event.OrganizerId == organizerId && !w.Event.IsDeleted);

            // Compute overall summary statistics
            var totalWaiting = await baseQuery.CountAsync(w => w.Status == WaitingListStatus.Waiting, cancellationToken);
            var totalNotified = await baseQuery.CountAsync(w => w.Status == WaitingListStatus.Notified, cancellationToken);
            var totalConverted = await baseQuery.CountAsync(w => w.Status == WaitingListStatus.Converted, cancellationToken);
            var totalExpired = await baseQuery.CountAsync(w => w.Status == WaitingListStatus.Expired, cancellationToken);

            var totalNotifiedEver = totalNotified + totalConverted + totalExpired;
            var conversionRate = totalNotifiedEver > 0 ? Math.Round(((double)totalConverted / totalNotifiedEver) * 100, 1) : 0.0;

            // Build filtered query
            var filteredQuery = baseQuery;

            if (eventId.HasValue)
            {
                filteredQuery = filteredQuery.Where(w => w.EventId == eventId.Value);
            }

            if (status.HasValue)
            {
                filteredQuery = filteredQuery.Where(w => w.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                filteredQuery = filteredQuery.Where(w => 
                    (w.User != null && w.User.FullName != null && w.User.FullName.ToLower().Contains(search)) ||
                    (w.User != null && w.User.Email != null && w.User.Email.ToLower().Contains(search)) ||
                    (w.TicketType != null && w.TicketType.Name.ToLower().Contains(search)));
            }

            var totalFiltered = await filteredQuery.CountAsync(cancellationToken);

            var entries = await filteredQuery
                .Include(w => w.Event)
                .Include(w => w.TicketType)
                .Include(w => w.User)
                .OrderByDescending(w => w.JoinedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Fetch all waiting list entries with status Waiting ordered by JoinedAt to determine queue positions
            var waitingIdsOrdered = await _unitOfWork.WaitingLists.GetQuery()
                .AsNoTracking()
                .Where(w => w.Event.OrganizerId == organizerId && w.Status == WaitingListStatus.Waiting)
                .OrderBy(w => w.JoinedAt)
                .Select(w => new { w.Id, w.EventId, w.TicketTypeId })
                .ToListAsync(cancellationToken);

            var entryDtos = entries.Select(w =>
            {
                var attendeeName = w.User?.FullName ?? "Unknown Attendee";

                // Calculate queue position if status is Waiting
                int position = 0;
                if (w.Status == WaitingListStatus.Waiting)
                {
                    position = waitingIdsOrdered
                        .Where(x => x.EventId == w.EventId && x.TicketTypeId == w.TicketTypeId)
                        .Select((x, idx) => new { x.Id, Index = idx + 1 })
                        .FirstOrDefault(x => x.Id == w.Id)?.Index ?? 0;
                }

                return new OrganizerWaitingListEntryDto
                {
                    Id = w.Id,
                    EventId = w.EventId,
                    EventTitle = w.Event.Title,
                    TicketTypeName = w.TicketType != null ? w.TicketType.Name : "General Waitlist",
                    QuantityWanted = w.QuantityWanted,
                    AttendeeName = attendeeName,
                    AttendeeEmail = w.User?.Email ?? string.Empty,
                    Status = w.Status.ToString(),
                    JoinedAt = w.JoinedAt,
                    NotifiedAt = w.NotifiedAt,
                    ExpiresAt = w.ExpiresAt,
                    PositionInQueue = position
                };
            }).ToList();

            var pagedResult = PagedResult<OrganizerWaitingListEntryDto>.Create(entryDtos, totalFiltered, page, pageSize);

            var summary = new OrganizerWaitingListSummaryDto
            {
                Entries = pagedResult,
                TotalWaiting = totalWaiting,
                TotalNotified = totalNotified,
                TotalConverted = totalConverted,
                ConversionRate = conversionRate
            };

            return Result<OrganizerWaitingListSummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            return Result<OrganizerWaitingListSummaryDto>.Failure($"Failed to retrieve waiting list summary: {ex.Message}");
        }
    }

    public async Task<Result> RemoveByOrganizerAsync(int id, string organizerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var waitingListEntry = await _unitOfWork.WaitingLists.GetQuery()
                .Include(w => w.Event)
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

            if (waitingListEntry == null)
                return Result.Failure("Waiting list entry not found.");

            if (waitingListEntry.Event.OrganizerId != organizerId)
                return Result.Failure("You are not authorized to remove this waiting list entry.");

            _unitOfWork.WaitingLists.Delete(waitingListEntry);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to remove waiting list entry: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<WaitingListResponseDto>>> GetUserWaitingListEntriesAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await _unitOfWork.WaitingLists.GetQuery()
                .AsNoTracking()
                .Include(w => w.Event)
                .Include(w => w.TicketType)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.JoinedAt)
                .ToListAsync(cancellationToken);

            var dtos = entries.Select(w => new WaitingListResponseDto
            {
                Id = w.Id,
                EventId = w.EventId,
                EventTitle = w.Event != null ? w.Event.Title : string.Empty,
                TicketTypeId = w.TicketTypeId,
                TicketTypeName = w.TicketType != null ? w.TicketType.Name : string.Empty,
                UserId = w.UserId,
                QuantityWanted = w.QuantityWanted,
                Status = w.Status.ToString(),
                CreatedAt = w.JoinedAt
            }).ToList();

            return Result<IReadOnlyList<WaitingListResponseDto>>.Success(dtos.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<WaitingListResponseDto>>.Failure($"Failed to retrieve user waiting list: {ex.Message}");
        }
    }
}

