namespace EventifyPro.BLL.Services.Interfaces;

public interface IUserService
{
    // Profile Management
    Task<Result<UserProfileDto>> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<UserProfileDto>> UpdateProfileAsync(string userId, UserUpdateProfileDto dto, CancellationToken cancellationToken = default);

    // Administrative operations
    Task<Result<IReadOnlyList<UserProfileDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result> SetActiveAsync(string id, bool isActive, CancellationToken cancellationToken = default);
    Task<Result> AssignRoleAsync(string id, string roleName, CancellationToken cancellationToken = default);
}
