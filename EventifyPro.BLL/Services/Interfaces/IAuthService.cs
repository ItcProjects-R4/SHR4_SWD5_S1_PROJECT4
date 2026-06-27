namespace EventifyPro.BLL.Services.Interfaces;

public interface IAuthService
{
    Task<Result> RegisterPublicUserAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<Result> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> GetUserPrimaryRoleByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> CreateScannerForOrganizerAsync(CreateScannerDto dto, string organizerId, CancellationToken cancellationToken = default);
    Task<bool> IsUserNameAvailableAsync(string userName, CancellationToken cancellationToken = default);
    Task<bool> IsEmailAvailableAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> ConfirmEmailAsync(string email, string otpCode, CancellationToken cancellationToken = default);
    Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<Result<string>> VerifyPasswordResetOtpAsync(string email, string otpCode, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);
    Task<Result> ResendVerificationEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> IsOrganizerVerifiedAsync(string userId, CancellationToken cancellationToken = default);
}
