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
    private readonly IOutboxService _outboxService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IOutboxService outboxService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _outboxService = outboxService;
        _emailService = emailService;
        _configuration = configuration;
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

        // Generate email confirmation token
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = System.Net.WebUtility.UrlEncode(token);
        var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7198";
        var verificationLink = $"{baseUrl}/Account/ConfirmEmail?userId={user.Id}&token={encodedToken}";

        // Enqueue verification email to outbox
        await _outboxService.EnqueueAsync("Email.Verification", new OutboxService.VerificationPayload
        {
            RecipientEmail = user.Email,
            RecipientName = user.FullName,
            VerificationLink = verificationLink
        });

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

        if (signInResult.IsNotAllowed)
        {
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                // Resend email confirmation
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = System.Net.WebUtility.UrlEncode(token);
                var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7198";
                var verificationLink = $"{baseUrl}/Account/ConfirmEmail?userId={user.Id}&token={encodedToken}";

                await _outboxService.EnqueueAsync("Email.Verification", new OutboxService.VerificationPayload
                {
                    RecipientEmail = user.Email!,
                    RecipientName = user.FullName,
                    VerificationLink = verificationLink
                });

                return Result.Failure("Your email is not verified. A new verification link has been sent to your email.");
            }
        }

        return signInResult.Succeeded
            ? Result.Success()
            : Result.Failure(ErrorMessages.User.InvalidCredentials);
    }

    public async Task<Result> LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return Result.Success();
    }

    public async Task<Result<string>> GetUserPrimaryRoleByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<string>.Failure("Email address is required.");

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null)
            return Result<string>.Failure(ErrorMessages.User.NotFound);

        var roles = await _userManager.GetRolesAsync(user);
        var role = LoginRoles.FirstOrDefault(r => roles.Contains(r));

        return string.IsNullOrWhiteSpace(role)
            ? Result<string>.Failure(ErrorMessages.User.RoleNotFound)
            : Result<string>.Success(role);
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

        var scannerUserName = await GenerateUniqueUserNameAsync(dto.Email);

        var scanner = new ApplicationUser
        {
            UserName = scannerUserName,
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

        // Send scanner credentials email
        await _emailService.SendScannerCredentialsEmailAsync(
            dto.Email,
            dto.FullName,
            dto.Password,
            organizer.FullName);

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

    public async Task<Result> ConfirmEmailAsync(string userId, string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            return Result.Failure("User ID and confirmation token are required.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure(ErrorMessages.User.NotFound);

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            return Result.Failure(ToErrorMessage(result));

        await _outboxService.EnqueueAsync(
            "Email.Welcome",
            new OutboxService.WelcomePayload
            {
                RecipientEmail = user.Email!,
                RecipientName = user.FullName
            },
            DateTime.UtcNow.AddSeconds(10));

        return Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(string email, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure("Email address is required.");

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            // Security best practice: do not reveal that the user does not exist
            return Result.Success();
        }

        // Generate password reset token
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = System.Net.WebUtility.UrlEncode(token);
        var resetLink = $"{baseUrl}/Account/ResetPassword?email={System.Net.WebUtility.UrlEncode(user.Email!)}&token={encodedToken}";

        // Enqueue password reset email to outbox
        await _outboxService.EnqueueAsync("Email.PasswordReset", new OutboxService.PasswordResetPayload
        {
            RecipientEmail = user.Email!,
            RecipientName = user.FullName,
            ResetLink = resetLink
        });

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (dto is null)
            return Result.Failure("Invalid request.");

        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        if (user is null)
        {
            // Security best practice: do not reveal user existence
            return Result.Failure(ErrorMessages.User.NotFound);
        }

        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (!result.Succeeded)
            return Result.Failure(ToErrorMessage(result));

        // Enqueue security notification email to outbox
        await _outboxService.EnqueueAsync("Email.SecurityNotification", new OutboxService.SecurityNotificationPayload
        {
            RecipientEmail = user.Email!,
            RecipientName = user.FullName,
            SecurityAction = "Password Reset",
            Details = $"Your password was successfully reset on {DateTime.UtcNow:dd MMM yyyy, hh:mm tt} UTC."
        });

        return Result.Success();
    }

    public async Task<Result> ResendVerificationEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure("Email address is required.");

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return Result.Success();
        }

        if (user.EmailConfirmed)
        {
            return Result.Failure("Email is already confirmed.");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = System.Net.WebUtility.UrlEncode(token);
        var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7198";
        var verificationLink = $"{baseUrl}/Account/ConfirmEmail?userId={user.Id}&token={encodedToken}";

        await _outboxService.EnqueueAsync("Email.Verification", new OutboxService.VerificationPayload
        {
            RecipientEmail = user.Email!,
            RecipientName = user.FullName,
            VerificationLink = verificationLink
        });

        return Result.Success();
    }

    private static string NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return string.Empty;

        return LoginRoles.FirstOrDefault(
                   r => string.Equals(r, role.Trim(), StringComparison.OrdinalIgnoreCase))
               ?? string.Empty;
    }

    private async Task<string> GenerateUniqueUserNameAsync(string email)
    {
        var localPart = email.Trim().Split('@')[0];
        var sanitized = new string(localPart
            .Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_')
            .ToArray())
            .Trim('_');

        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "scanner";

        var candidate = sanitized;
        var suffix = 1;

        while (await _userManager.FindByNameAsync(candidate) is not null)
        {
            candidate = $"{sanitized}_{suffix++}";
        }

        return candidate;
    }

    private static string ToErrorMessage(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));
}
