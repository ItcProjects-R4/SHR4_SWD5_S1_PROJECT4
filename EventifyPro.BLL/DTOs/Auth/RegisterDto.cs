namespace EventifyPro.BLL.DTOs.Auth;

/// <summary>
/// Data transfer object for user registration request.
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(40, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    [Required, MinLength(8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password confirmation to verify the user entered the password correctly.
    /// </summary>
    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role assigned to the user during registration.
    /// </summary>
    [Required]
    public string Role { get; set; } = string.Empty;
}
