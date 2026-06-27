namespace Eventify.Domain.Entities;

/// <summary>
/// Represents the profile details of an Organizer entity (business or individual).
/// </summary>
public class OrganizerProfile : AuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the associated user ID (foreign key).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the commercial organization/business name.
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets organization details.
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Gets or sets the URL of the organization's logo.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the official website URL.
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Gets or sets the official telephone number for business inquires.
    /// </summary>
    public string? BusinessPhone { get; set; }

    /// <summary>
    /// Gets or sets the commercial register number (for corporate verification).
    /// </summary>
    public string? CommercialRegister { get; set; }

    /// <summary>
    /// Gets or sets the tax number (for verification).
    /// </summary>
    public string? TaxNumber { get; set; }

    /// <summary>
    /// Gets or sets the social media links.
    /// </summary>
    public string? FacebookUrl { get; set; }
    public string? LinkedInUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the organizer's business profile has been verified by an administrator.
    /// </summary>
    public bool IsVerified { get; set; } = false;

    public DateTime? VerifiedAt { get; set; }
    public string? VerifiedById { get; set; }
    public ApplicationUser? VerifiedBy { get; set; }

    // Banking connection details
    public string? BankAccountName { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankRoutingNumber { get; set; }
    public bool IsBankAccountConnected { get; set; } = false;

    public string? RejectionReason { get; set; }
}
