using System.Threading;
using System.Threading.Tasks;
using EventifyPro.BLL.DTOs.Home;
using Eventify.Shared.Wrappers;

namespace EventifyPro.BLL.Services.Interfaces
{
    public interface IHomeService
    {
        Task<Result<LandingPageDataDto>> GetLandingPageDataAsync(CancellationToken cancellationToken = default);
        Task<Result<bool>> SubmitFeedbackAsync(FeedbackCreateDto dto, CancellationToken cancellationToken = default);
    }
}
