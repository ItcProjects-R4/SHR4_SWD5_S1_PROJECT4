namespace EventifyPro.BLL.Services.Interfaces;

public interface IDashboardService
{
    Task<Result<OrganizerDashboardDto>> GetOrganizerDashboardAsync(string organizerId, CancellationToken cancellationToken = default);
    Task<Result<AdminDashboardDto>> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
}
