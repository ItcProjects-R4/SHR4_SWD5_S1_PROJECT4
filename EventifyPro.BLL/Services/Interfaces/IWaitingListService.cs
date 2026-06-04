namespace EventifyPro.BLL.Services.Interfaces;

public interface IWaitingListService
{
    Task<Result<WaitingListResponseDto>> JoinAsync(WaitingListJoinDto dto, string userId, CancellationToken cancellationToken = default);
    Task<Result> LeaveAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task<Result> NotifyNextAsync(int ticketTypeId, CancellationToken cancellationToken = default);
    Task<Result> AdvanceQueueAsync(int eventId, CancellationToken cancellationToken = default);
}
