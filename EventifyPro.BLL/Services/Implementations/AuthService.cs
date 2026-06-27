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
    private readonly ISystemSettingService _systemSettingService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        IOutboxService outboxService,
        IEmailService emailService,
        IConfiguration configuration,
        ISystemSettingService systemSettingService,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _outboxService = outboxService;
        _emailService = emailService;
        _configuration = configuration;
        _systemSettingService = systemSettingService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> RegisterPublicUserAsync(RegisterDto dto, CancellationToken cancellationToken = default)
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
            PhoneNumber = dto.BusinessPhone?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Custom password policy validation based on system settings
        var minLength = await _systemSettingService.GetSettingValueAsync<int>("PasswordMinLength", 8, cancellationToken);
        var requireUppercase = await _systemSettingService.GetSettingValueAsync<bool>("RequirePasswordUppercase", true, cancellationToken);
        var requireDigits = await _systemSettingService.GetSettingValueAsync<bool>("RequirePasswordDigits", true, cancellationToken);

        if (dto.Password.Length < minLength)
        {
            return Result.Failure($"Password must be at least {minLength} characters long.");
        }
        if (requireUppercase && !dto.Password.Any(char.IsUpper))
        {
            return Result.Failure("Password must contain at least one uppercase letter.");
        }
        if (requireDigits && !dto.Password.Any(char.IsDigit))
        {
            return Result.Failure("Password must contain at least one digit.");
        }

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return Result.Failure(ToErrorMessage(createResult));

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return Result.Failure(ErrorMessages.User.RoleAssignmentFailed);
        }

        if (role == RoleNames.Organizer)
        {
            var profile = new OrganizerProfile
            {
                UserId = user.Id,
                OrganizationName = string.IsNullOrWhiteSpace(dto.OrganizationName)
                    ? user.FullName
                    : dto.OrganizationName.Trim(),
                BusinessPhone = dto.BusinessPhone?.Trim(),
                WebsiteUrl = dto.WebsiteUrl?.Trim(),
                CommercialRegister = dto.CommercialRegister?.Trim(),
                TaxNumber = dto.TaxNumber?.Trim(),
                LogoUrl = dto.LogoUrl,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _unitOfWork.OrganizerProfiles.AddAsync(profile);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception)
            {
                await _userManager.DeleteAsync(user);
                return Result.Failure("Failed to create organizer profile. Please try again.");
            }
        }

        // Generate alphanumeric OTP & standard Identity confirmation token
        var otp = GenerateAlphanumericOtp();
        var identityToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var expiry = DateTime.UtcNow.AddMinutes(15).Ticks;
        var attempts = 0;
        var hashedOtp = HashString(otp);

        // Store compound token string in AspNetUserTokens
        await _userManager.SetAuthenticationTokenAsync(
            user, 
            "EmailOtpProvider", 
            "VerifyEmail", 
            $"{hashedOtp}|{identityToken}|{expiry}|{attempts}");

        // Enqueue verification email to outbox
        await _outboxService.EnqueueAsync("Email.Verification", new OutboxService.VerificationPayload
        {
            RecipientEmail = user.Email!,
            RecipientName = user.FullName,
            OtpCode = otp
        });

        return Result.Success();
    }

    public async Task<Result> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return Result.Failure(ErrorMessages.User.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure(ErrorMessages.User.AccountDisabled);

        var roles = await _userManager.GetRolesAsync(user);
        var isOrganizer = roles.Contains(RoleNames.Organizer);

        var signInResult = await _signInManager.PasswordSignInAsync(
            user,
            dto.Password,
            dto.RememberMe,
            lockoutOnFailure: !isOrganizer);

        if (signInResult.IsLockedOut)
            return Result.Failure("Account is locked. Please try again later.");

        if (signInResult.IsNotAllowed)
        {
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                // Resend email confirmation OTP
                var otp = GenerateAlphanumericOtp();
                var identityToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var expiry = DateTime.UtcNow.AddMinutes(15).Ticks;
                var attempts = 0;
                var hashedOtp = HashString(otp);

                // Store compound token string in AspNetUserTokens
                await _userManager.SetAuthenticationTokenAsync(
                    user, 
                    "EmailOtpProvider", 
                    "VerifyEmail", 
                    $"{hashedOtp}|{identityToken}|{expiry}|{attempts}");

                await _outboxService.EnqueueAsync("Email.Verification", new OutboxService.VerificationPayload
                {
                    RecipientEmail = user.Email!,
                    RecipientName = user.FullName,
                    OtpCode = otp
                }, cancellationToken);

                return Result.Failure("Your email is not verified. A new verification code (OTP) has been sent to your email.");
            }
        }

        if (signInResult.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            return Result.Success();
        }

        return Result.Failure(ErrorMessages.User.InvalidCredentials);
    }

    public async Task<Result> LogoutAsync(CancellationToken cancellationToken = default)
    {
        await _signInManager.SignOutAsync();
        return Result.Success();
    }

    public async Task<Result<string>> GetUserPrimaryRoleByEmailAsync(string email, CancellationToken cancellationToken = default)
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

    public async Task<bool> IsOrganizerVerifiedAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var profile = await _unitOfWork.OrganizerProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        return profile?.IsVerified ?? false;
    }

    public async Task<Result> CreateScannerForOrganizerAsync(CreateScannerDto dto, string organizerId, CancellationToken cancellationToken = default)
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
            CreatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var password = string.IsNullOrWhiteSpace(dto.Password)
            ? GenerateRandomPassword()
            : dto.Password;

        var createResult = await _userManager.CreateAsync(scanner, password);
        if (!createResult.Succeeded)
            return Result.Failure(ToErrorMessage(createResult));

        var roleResult = await _userManager.AddToRoleAsync(scanner, RoleNames.Scanner);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(scanner);
            return Result.Failure(ErrorMessages.User.RoleAssignmentFailed);
        }

        // Enqueue scanner credentials email via Outbox
        await _outboxService.EnqueueAsync("Email.ScannerCredentials", new OutboxService.ScannerCredentialsPayload
        {
            RecipientEmail = dto.Email.Trim(),
            RecipientName = dto.FullName.Trim(),
            TemporaryPassword = password,
            OrganizerName = organizer.FullName
        }, cancellationToken);

        return Result.Success();
    }

    public async Task<bool> IsUserNameAvailableAsync(string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return false;

        return await _userManager.FindByNameAsync(userName.Trim()) is null;
    }

    public async Task<bool> IsEmailAvailableAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return await _userManager.FindByEmailAsync(email.Trim()) is null;
    }

    public async Task<Result> ConfirmEmailAsync(string email, string otpCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otpCode))
            return Result.Failure("Email and verification code are required.");

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null)
            return Result.Failure("Invalid verification code.");

        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var remainingMinutes = lockoutEnd.HasValue 
                ? Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes) 
                : 15;
            return Result.Failure($"Too many failed verification attempts. Your account is locked. Please try again in {remainingMinutes} minutes.");
        }

        if (user.EmailConfirmed)
            return Result.Success(); // Already verified

        var storedValue = await _userManager.GetAuthenticationTokenAsync(user, "EmailOtpProvider", "VerifyEmail");
        if (string.IsNullOrEmpty(storedValue))
            return Result.Failure("No active verification code found for this email. Please request a new one.");

        var parts = storedValue.Split('|');
        if (parts.Length < 4)
            return Result.Failure("Invalid verification token format. Please request a new code.");

        var storedHashedOtp = parts[0];
        var identityToken = parts[1];
        var expiryTicks = long.Parse(parts[2]);
        var attempts = int.Parse(parts[3]);

        if (DateTime.UtcNow.Ticks > expiryTicks)
        {
            await _userManager.RemoveAuthenticationTokenAsync(user, "EmailOtpProvider", "VerifyEmail");
            return Result.Failure("Your verification code has expired. Please request a new code.");
        }

        var hashedInputOtp = HashString(otpCode.Trim().ToUpper());
        if (!string.Equals(storedHashedOtp, hashedInputOtp, StringComparison.Ordinal))
        {
            attempts++;
            if (attempts >= 3)
            {
                await _userManager.RemoveAuthenticationTokenAsync(user, "EmailOtpProvider", "VerifyEmail");
                
                // Lock the account for 15 minutes
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
                
                return Result.Failure("Too many incorrect attempts. Your account has been locked for 15 minutes.");
            }

            await _userManager.SetAuthenticationTokenAsync(user, "EmailOtpProvider", "VerifyEmail", $"{storedHashedOtp}|{identityToken}|{expiryTicks}|{attempts}");
            return Result.Failure($"Invalid verification code. You have {3 - attempts} attempts remaining.");
        }

        // OTP matches! Confirm the email using the stored identity token
        var result = await _userManager.ConfirmEmailAsync(user, identityToken);
        if (!result.Succeeded)
            return Result.Failure(ToErrorMessage(result));

        // Clean up the token
        await _userManager.RemoveAuthenticationTokenAsync(user, "EmailOtpProvider", "VerifyEmail");

        // Enqueue the Welcome email now that email is verified and registration is complete
        await _outboxService.EnqueueAsync(
            "Email.Welcome",
            new OutboxService.WelcomePayload
            {
                RecipientEmail = user.Email!,
                RecipientName = user.FullName
            },
            DateTime.UtcNow.AddSeconds(5),
            cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure("Email address is required.");

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            // Security best practice: do not reveal that the user does not exist
            return Result.Success();
        }

        // Generate alphanumeric OTP & standard Identity password reset token
        var otp = GenerateAlphanumericOtp();
        var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var expiry = DateTime.UtcNow.AddMinutes(15).Ticks;
        var attempts = 0;
        var hashedOtp = HashString(otp);

        // Store compound token string in AspNetUserTokens
        await _userManager.SetAuthenticationTokenAsync(
            user, 
            "PasswordResetOtpProvider", 
            "ResetPassword", 
            $"{hashedOtp}|{identityToken}|{expiry}|{attempts}");

        // Enqueue password reset email to outbox
        await _outboxService.EnqueueAsync("Email.PasswordReset", new OutboxService.PasswordResetPayload
        {
            RecipientEmail = user.Email!,
            RecipientName = user.FullName,
            OtpCode = otp
        }, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<string>> VerifyPasswordResetOtpAsync(string email, string otpCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otpCode))
            return Result<string>.Failure("Email and verification code are required.");

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null)
            return Result<string>.Failure("Invalid verification code.");

        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var remainingMinutes = lockoutEnd.HasValue 
                ? Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes) 
                : 15;
            return Result<string>.Failure($"Too many failed verification attempts. Your account is locked. Please try again in {remainingMinutes} minutes.");
        }

        var storedValue = await _userManager.GetAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetPassword");
        if (string.IsNullOrEmpty(storedValue))
            return Result<string>.Failure("No active verification code found for this email. Please request a new one.");

        var parts = storedValue.Split('|');
        if (parts.Length < 4)
            return Result<string>.Failure("Invalid verification token format. Please request a new code.");

        var storedHashedOtp = parts[0];
        var identityToken = parts[1];
        var expiryTicks = long.Parse(parts[2]);
        var attempts = int.Parse(parts[3]);

        if (DateTime.UtcNow.Ticks > expiryTicks)
        {
            await _userManager.RemoveAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetPassword");
            return Result<string>.Failure("Your verification code has expired. Please request a new code.");
        }

        var hashedInputOtp = HashString(otpCode.Trim().ToUpper());
        if (!string.Equals(storedHashedOtp, hashedInputOtp, StringComparison.Ordinal))
        {
            attempts++;
            if (attempts >= 3)
            {
                await _userManager.RemoveAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetPassword");
                
                // Lock the account for 15 minutes
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
                
                return Result<string>.Failure("Too many incorrect attempts. Your account has been locked for 15 minutes.");
            }

            await _userManager.SetAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetPassword", $"{storedHashedOtp}|{identityToken}|{expiryTicks}|{attempts}");
            return Result<string>.Failure($"Invalid verification code. You have {3 - attempts} attempts remaining.");
        }

        // Clean up the token
        await _userManager.RemoveAuthenticationTokenAsync(user, "PasswordResetOtpProvider", "ResetPassword");

        return Result<string>.Success(identityToken);
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default)
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
        }, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ResendVerificationEmailAsync(string email, CancellationToken cancellationToken = default)
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

        // Generate new alphanumeric OTP & standard Identity confirmation token
        var otp = GenerateAlphanumericOtp();
        var identityToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var expiry = DateTime.UtcNow.AddMinutes(15).Ticks;
        var attempts = 0;
        var hashedOtp = HashString(otp);

        // Store compound token string in AspNetUserTokens
        await _userManager.SetAuthenticationTokenAsync(
            user, 
            "EmailOtpProvider", 
            "VerifyEmail", 
            $"{hashedOtp}|{identityToken}|{expiry}|{attempts}");

        // Enqueue verification email to outbox
        await _outboxService.EnqueueAsync("Email.Verification", new OutboxService.VerificationPayload
        {
            RecipientEmail = user.Email!,
            RecipientName = user.FullName,
            OtpCode = otp
        }, cancellationToken);

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

    private string GenerateRandomPassword(int length = 12)
    {
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var allChars = lower + upper + digits + special;
        var bytes = new byte[length];

        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        var result = new char[length];
        
        // Ensure at least one of each class to satisfy standard password policies
        result[0] = lower[bytes[0] % lower.Length];
        result[1] = upper[bytes[1] % upper.Length];
        result[2] = digits[bytes[2] % digits.Length];
        result[3] = special[bytes[3] % special.Length];

        for (int i = 4; i < length; i++)
        {
            result[i] = allChars[bytes[i] % allChars.Length];
        }

        // Fisher-Yates shuffle using cryptographically secure random bytes
        byte[] shuffleBytes = new byte[length];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(shuffleBytes);
        }

        for (int i = length - 1; i > 0; i--)
        {
            int j = shuffleBytes[i] % (i + 1);
            var temp = result[i];
            result[i] = result[j];
            result[j] = temp;
        }

        return new string(result);
    }

    private string GenerateAlphanumericOtp(int length = 6)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = new byte[length];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        return new string(result);
    }

    private static string HashString(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }
}
