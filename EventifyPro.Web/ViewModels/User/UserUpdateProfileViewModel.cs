using System.ComponentModel.DataAnnotations;

namespace EventifyPro.Web.ViewModels.User;

public class UserUpdateProfileViewModel
{
    [Required, StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ProfileImageUrl { get; set; }
}
