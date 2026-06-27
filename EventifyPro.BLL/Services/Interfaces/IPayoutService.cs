using System.Threading;
using System.Threading.Tasks;
using EventifyPro.BLL.DTOs.Payout;
using Eventify.Shared.Wrappers;

namespace EventifyPro.BLL.Services.Interfaces
{
    public interface IPayoutService
    {
        Task<Result<OrganizerPayoutSummaryDto>> GetOrganizerPayoutSummaryAsync(string organizerId, CancellationToken cancellationToken = default);
        Task<Result<bool>> RequestPayoutAsync(string organizerId, decimal amount, CancellationToken cancellationToken = default);
        Task<Result<bool>> ConnectBankAsync(string organizerId, string bankAccountName, string bankName, string bankAccountNumber, string bankRoutingNumber, CancellationToken cancellationToken = default);
        Task<Result<bool>> DisconnectBankAsync(string organizerId, CancellationToken cancellationToken = default);
    }
}
