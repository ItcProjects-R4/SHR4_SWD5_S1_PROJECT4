namespace EventifyPro.BLL.Services.Implementations;

public class SavedEventService : ISavedEventService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SavedEventService> _logger;

    public SavedEventService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<SavedEventService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<bool>> ToggleSaveEventAsync(
        string userId,
        int eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Toggling saved event state. UserId: {UserId}, EventId: {EventId}", userId, eventId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<bool>.Failure("User ID is required.");
            }

            var eventExists = await _unitOfWork.Events.AnyAsync(e => e.Id == eventId && !e.IsDeleted, cancellationToken);
            if (!eventExists)
            {
                _logger.LogWarning("Toggle save failed: Event {EventId} not found or deleted.", eventId);
                return Result<bool>.Failure("Event not found.");
            }

            var savedEvent = await _unitOfWork.SavedEvents.FirstOrDefaultAsync(
                s => s.UserId == userId && s.EventId == eventId,
                cancellationToken);

            if (savedEvent != null)
            {
                _unitOfWork.SavedEvents.Delete(savedEvent);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Event {EventId} removed from saved list for user {UserId}", eventId, userId);
                return Result<bool>.Success(false); // false indicates unsaved
            }

            var newSavedEvent = new SavedEvent
            {
                UserId = userId,
                EventId = eventId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.SavedEvents.AddAsync(newSavedEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Event {EventId} saved for user {UserId}", eventId, userId);
            return Result<bool>.Success(true); // true indicates saved
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling save event state. UserId: {UserId}, EventId: {EventId}", userId, eventId);
            return Result<bool>.Failure("An error occurred while saving/unsaving the event.");
        }
    }

    public async Task<Result<PagedResult<SavedEventDto>>> GetSavedEventsForUserAsync(
        string userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving saved events for user: {UserId}, Page: {PageNumber}", userId, pageNumber);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<PagedResult<SavedEventDto>>.Failure("User ID is required.");
            }

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var savedEvents = await _unitOfWork.SavedEvents.GetUserSavedEventsAsync(
                userId,
                pageNumber,
                pageSize,
                cancellationToken);

            var totalCount = await _unitOfWork.SavedEvents.CountAsync(
                s => s.UserId == userId,
                cancellationToken);

            var mappedData = savedEvents.Select(se => _mapper.Map<SavedEventDto>(se)).ToList();

            var result = PagedResult<SavedEventDto>.Create(
                mappedData,
                totalCount,
                pageNumber,
                pageSize);

            return Result<PagedResult<SavedEventDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saved events for user: {UserId}", userId);
            return Result<PagedResult<SavedEventDto>>.Failure("An error occurred while retrieving saved events.");
        }
    }

    public async Task<Result<bool>> IsEventSavedByUserAsync(
        string userId,
        int eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Result<bool>.Success(false);
            }

            var isSaved = await _unitOfWork.SavedEvents.IsEventSavedByUserAsync(userId, eventId, cancellationToken);
            return Result<bool>.Success(isSaved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if event {EventId} is saved by user {UserId}", eventId, userId);
            return Result<bool>.Success(false); // Fallback to false on error, don't crash
        }
    }
}
