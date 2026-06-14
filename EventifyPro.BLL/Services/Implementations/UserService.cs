using Eventify.Domain.Entities;
using EventifyPro.BLL.DTOs.User;
using EventifyPro.BLL.Services.Interfaces;
using EventifyPro.DAL.Repositories.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventifyPro.BLL.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUploadHelper _uploadHelper;

    public UserService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUploadHelper uploadHelper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userManager = userManager;
        _roleManager = roleManager;
        _uploadHelper = uploadHelper;
    }

    public async Task<Result<UserProfileDto>> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result<UserProfileDto>.Failure("User not found.");
        }

        var dto = _mapper.Map<UserProfileDto>(user);
        return Result<UserProfileDto>.Success(dto);
    }

    public async Task<Result<UserProfileDto>> UpdateProfileAsync(string userId, UserUpdateProfileDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result<UserProfileDto>.Failure("User not found.");
        }

        // Apply updates
        user.FullName = dto.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.ProfileImageUrl))
        {
            user.ProfileImageUrl = dto.ProfileImageUrl.Trim();
        }
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return Result<UserProfileDto>.Failure($"Failed to update profile: {errors}");
        }

        var profileDto = _mapper.Map<UserProfileDto>(user);
        return Result<UserProfileDto>.Success(profileDto);
    }

    public async Task<Result<IReadOnlyList<UserProfileDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = users.Select(u => _mapper.Map<UserProfileDto>(u)).ToList().AsReadOnly();
        return Result<IReadOnlyList<UserProfileDto>>.Success(dtos);
    }

    public async Task<Result> SetActiveAsync(string id, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Result.Failure("User not found.");
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return Result.Failure($"Failed to update user status: {errors}");
        }

        return Result.Success();
    }

    public async Task<Result> AssignRoleAsync(string id, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return Result.Failure("User not found.");
        }

        // Check if role exists
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            return Result.Failure($"Role '{roleName}' does not exist.");
        }

        // Retrieve current roles
        var currentRoles = await _userManager.GetRolesAsync(user);

        // Remove from current roles
        if (currentRoles.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join(" ", removeResult.Errors.Select(e => e.Description));
                return Result.Failure($"Failed to remove current roles: {errors}");
            }
        }

        // Add to new role
        var addResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!addResult.Succeeded)
        {
            var errors = string.Join(" ", addResult.Errors.Select(e => e.Description));
            return Result.Failure($"Failed to assign role '{roleName}': {errors}");
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Result.Success();
    }
}
