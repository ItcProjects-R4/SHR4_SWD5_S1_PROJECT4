namespace EventifyPro.BLL.Services.Interfaces;

public interface IEventService
{
    Task<Result<EventDetailDto>> CreateAsync(EventCreateDto dto, string organizerId, CancellationToken cancellationToken = default);
    Task<Result<EventDetailDto>> UpdateAsync(int id, EventUpdateDto dto, string organizerId, CancellationToken cancellationToken = default);
    Task<Result> PublishAsync(int id, string organizerId, CancellationToken cancellationToken = default);
    Task<Result> ApproveAsync(int id, string adminId, CancellationToken cancellationToken = default);
    Task<Result> RejectAsync(int id, string adminId, string reason, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<EventSummaryDto>>> GetPendingReviewAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<Result> CancelAsync(int id, string organizerId, string reason, CancellationToken cancellationToken = default);
    Task<Result> CancelByAdminAsync(int id, string adminId, string reason, CancellationToken cancellationToken = default);
    Task<Result<EventDetailDto>> GetDetailAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<EventSummaryDto>> SearchAsync(EventFilterDto filter, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, string organizerId, CancellationToken cancellationToken = default);
    Task<Result> RestoreAsync(int id, string organizerId, CancellationToken cancellationToken = default);
    Task<Result<EventPerformanceDto>> GetPerformanceAsync(int id, string organizerId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EventSummaryDto>>> GetOrganizerEventsAsync(string organizerId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<EventSummaryDto>>> GetOrganizerEventsPagedAsync(
        string organizerId,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsByTitleAsync(string title, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<EventAttendeeDto>>> GetEventAttendeesPageAsync(
        int eventId,
        string organizerId,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EventAttendeeDto>>> GetEventAttendeesForExportAsync(
        int eventId,
        string organizerId,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<EventSummaryDto>>> GetAttendeeEventsPagedAsync(
        string userId,
        string statusFilter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}


