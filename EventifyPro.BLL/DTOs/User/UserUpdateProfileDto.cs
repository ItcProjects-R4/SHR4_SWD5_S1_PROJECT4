namespace EventifyPro.BLL.DTOs.User;

/// <summary>
/// Data transfer object for updating user profile information.
/// </summary>
public class UserUpdateProfileDto
{
    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL to the user's profile image.
    /// </summary>
    public string? ProfileImageUrl { get; set; }
}
