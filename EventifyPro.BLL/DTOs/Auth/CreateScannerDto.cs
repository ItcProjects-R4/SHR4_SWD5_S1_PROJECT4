namespace EventifyPro.BLL.DTOs.Auth;

/// <summary>
/// Data transfer object for creating a new scanner user account.
/// </summary>
public class CreateScannerDto
{
    /// <summary>
    /// Gets or sets the scanner user's full name.
    /// </summary>
    [Required, StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scanner user's email address.
    /// </summary>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scanner user's password.
    /// </summary>
    [Required, MinLength(8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}