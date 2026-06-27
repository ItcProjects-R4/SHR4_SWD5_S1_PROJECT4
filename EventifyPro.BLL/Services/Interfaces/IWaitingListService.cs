using System.Threading;
using System.Threading.Tasks;
using EventifyPro.BLL.DTOs.WaitingList;
using Eventify.Domain.Enums;
using Eventify.Shared.Wrappers;

namespace EventifyPro.BLL.Services.Interfaces
{
    public interface IWaitingListService
    {
        Task<Result<WaitingListResponseDto>> JoinAsync(WaitingListJoinDto dto, string userId, CancellationToken cancellationToken = default);
        Task<Result> LeaveAsync(int id, string userId, CancellationToken cancellationToken = default);
        Task<Result> NotifyNextAsync(int ticketTypeId, CancellationToken cancellationToken = default);
        Task<Result> AdvanceQueueAsync(int eventId, CancellationToken cancellationToken = default);
        Task<int> ProcessExpiredNotificationsAsync(CancellationToken cancellationToken = default);
        Task<Result<OrganizerWaitingListSummaryDto>> GetOrganizerWaitingListSummaryAsync(
            int? eventId,
            WaitingListStatus? status,
            string? searchTerm,
            string organizerId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
        Task<Result> RemoveByOrganizerAsync(int id, string organizerId, CancellationToken cancellationToken = default);
        Task<Result<IReadOnlyList<WaitingListResponseDto>>> GetUserWaitingListEntriesAsync(string userId, CancellationToken cancellationToken = default);
    }
}

