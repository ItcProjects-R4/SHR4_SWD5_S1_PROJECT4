namespace EventifyPro.Web.ViewModels.Admin;

public class AdminOrganizerItemViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string? BusinessPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? CommercialRegister { get; set; }
    public string? TaxNumber { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}
