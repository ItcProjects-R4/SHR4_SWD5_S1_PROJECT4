namespace EventifyPro.BLL.DTOs.Auth;

/// <summary>
/// Data transfer object for user login request.
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to remember the user for future logins.
    /// </summary>
    public bool RememberMe { get; set; }
}
