namespace EventifyPro.BLL.Services.Interfaces;

public interface IAuthService
{
    Task<Result> RegisterPublicUserAsync(RegisterDto dto);
    Task<Result> LoginAsync(LoginDto dto);
    Task<Result> LogoutAsync();
    Task<Result<string>> GetUserPrimaryRoleByEmailAsync(string email);
    Task<Result> CreateScannerForOrganizerAsync(CreateScannerDto dto, string organizerId);
    Task<bool> IsUserNameAvailableAsync(string userName);
    Task<bool> IsEmailAvailableAsync(string email);
    Task<Result> ConfirmEmailAsync(string userId, string token);
    Task<Result> ForgotPasswordAsync(string email, string baseUrl);
    Task<Result> ResetPasswordAsync(ResetPasswordDto dto);
    Task<Result> ResendVerificationEmailAsync(string email);
}
