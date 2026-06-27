using EventifyPro.BLL.DTOs.Dashboard;

namespace EventifyPro.BLL.Services.Interfaces;

public interface IDashboardService
{
    Task<Result<OrganizerDashboardDto>> GetOrganizerDashboardAsync(string organizerId, CancellationToken cancellationToken = default);
    Task<Result<AdminDashboardDto>> GetAdminDashboardAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<Result<AttendeeDashboardDto>> GetAttendeeDashboardAsync(string userId, CancellationToken cancellationToken = default);
}
