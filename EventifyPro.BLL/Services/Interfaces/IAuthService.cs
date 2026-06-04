namespace EventifyPro.BLL.Services.Interfaces;

public interface IAuthService
{
    Task<Result> RegisterPublicUserAsync(RegisterDto dto);
    Task<Result> LoginAsync(LoginDto dto);
    Task<Result> LogoutAsync();
    Task<Result> CreateScannerForOrganizerAsync(CreateScannerDto dto, string organizerId);
    Task<bool> IsUserNameAvailableAsync(string userName);
    Task<bool> IsEmailAvailableAsync(string email);
}
