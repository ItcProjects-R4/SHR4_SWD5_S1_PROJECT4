namespace EventifyPro.BLL.Services.Interfaces;

public interface IEventService
{
    Task<Result<EventDetailDto>> CreateAsync(EventCreateDto dto, string organizerId, CancellationToken cancellationToken = default);
    Task<Result<EventDetailDto>> UpdateAsync(int id, EventUpdateDto dto, string organizerId, CancellationToken cancellationToken = default);
    Task<Result> PublishAsync(int id, string organizerId, CancellationToken cancellationToken = default);
    Task<Result> CancelAsync(int id, string organizerId, string reason, CancellationToken cancellationToken = default);
    Task<Result<EventDetailDto>> GetDetailAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<EventSummaryDto>> SearchAsync(EventFilterDto filter, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, string organizerId, CancellationToken cancellationToken = default);
}
