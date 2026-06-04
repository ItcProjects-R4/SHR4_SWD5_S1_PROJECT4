namespace EventifyPro.BLL.Services.Implementations;

public class AuthService : IAuthService
{
    private static readonly HashSet<string> PublicRegistrationRoles =
    [
        RoleNames.Attendee,
        RoleNames.Organizer
    ];

    private static readonly HashSet<string> LoginRoles =
    [
        RoleNames.Admin,
        RoleNames.Organizer,
        RoleNames.Attendee,
        RoleNames.Scanner
    ];

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    public async Task<Result> RegisterPublicUserAsync(RegisterDto dto)
    {
        var role = NormalizeRole(dto.Role);

        if (string.IsNullOrEmpty(role) || !PublicRegistrationRoles.Contains(role))
            return Result.Failure("Only Attendee and Organizer accounts can be created from public registration.");

        if (!await _roleManager.RoleExistsAsync(role))
            return Result.Failure(ErrorMessages.User.RoleNotFound);

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
            return Result.Failure(ErrorMessages.User.AlreadyExists);

        var existingUserName = await _userManager.FindByNameAsync(dto.UserName);
        if (existingUserName is not null)
            return Result.Failure("This username is already taken.");

        var user = new ApplicationUser
        {
            UserName = dto.UserName.Trim(),
            Email = dto.Email.Trim(),
            FullName = $"{dto.FirstName.Trim()} {dto.LastName.Trim()}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return Result.Failure(ToErrorMessage(createResult));

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return Result.Failure(ErrorMessages.User.RoleAssignmentFailed);
        }

        return Result.Success();
    }

    public async Task<Result> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return Result.Failure(ErrorMessages.User.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure(ErrorMessages.User.AccountDisabled);

        var signInResult = await _signInManager.PasswordSignInAsync(
            user,
            dto.Password,
            dto.RememberMe,
            lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
            return Result.Failure("Account is locked. Please try again later.");

        return signInResult.Succeeded
            ? Result.Success()
            : Result.Failure(ErrorMessages.User.InvalidCredentials);
    }

    public async Task<Result> LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return Result.Success();
    }

    public async Task<Result> CreateScannerForOrganizerAsync(CreateScannerDto dto, string organizerId)
    {
        var organizer = await _userManager.FindByIdAsync(organizerId);
        if (organizer is null || !await _userManager.IsInRoleAsync(organizer, RoleNames.Organizer))
            return Result.Failure(ErrorMessages.General.Unauthorized);

        if (!await _roleManager.RoleExistsAsync(RoleNames.Scanner))
            return Result.Failure(ErrorMessages.User.RoleNotFound);

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
            return Result.Failure(ErrorMessages.User.AlreadyExists);

        var scanner = new ApplicationUser
        {
            UserName = dto.Email.Trim(),
            Email = dto.Email.Trim(),
            FullName = dto.FullName.Trim(),
            ScannerCreatedByOrganizerId = organizerId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(scanner, dto.Password);
        if (!createResult.Succeeded)
            return Result.Failure(ToErrorMessage(createResult));

        var roleResult = await _userManager.AddToRoleAsync(scanner, RoleNames.Scanner);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(scanner);
            return Result.Failure(ErrorMessages.User.RoleAssignmentFailed);
        }

        return Result.Success();
    }

    public async Task<bool> IsUserNameAvailableAsync(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return false;

        return await _userManager.FindByNameAsync(userName.Trim()) is null;
    }

    public async Task<bool> IsEmailAvailableAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return await _userManager.FindByEmailAsync(email.Trim()) is null;
    }

    private static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return string.Empty;

        return LoginRoles.FirstOrDefault(
                   r => string.Equals(r, role.Trim(), StringComparison.OrdinalIgnoreCase))
               ?? string.Empty;
    }

    private static string ToErrorMessage(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));
}
