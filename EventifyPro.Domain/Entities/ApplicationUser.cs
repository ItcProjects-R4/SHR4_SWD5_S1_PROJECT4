namespace Eventify.Domain.Entities;

/// <summary>
/// Represents an application user extending ASP.NET Core Identity.
/// Users can have roles such as Admin, Organizer, Attendee, or Scanner.
/// </summary>
/// <remarks>
/// This class extends <see cref="IdentityUser{TKey}"/> to provide application-specific user properties.
/// Each user can organize events, attend events, review events, and participate in waiting lists.
/// </remarks>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the full name of the user.
    /// </summary>
    /// <value>The user's complete name.</value>
    public string FullName { get; set; } = string.Empty;

    public string? ProfileImageUrl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user account is active.
    /// </summary>
    /// <value><c>true</c> if the account is active; otherwise, <c>false</c>.</value>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the user account was created.
    /// </summary>
    /// <value>The creation timestamp in UTC.</value>
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? ScannerCreatedByOrganizerId { get; set; }

    // Navigation
    public ApplicationUser? ScannerCreatedByOrganizer { get; set; }
    public ICollection<ApplicationUser> CreatedScannerAccounts { get; set; } = [];
    public ICollection<Event> OrganizedEvents { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<Refund> InitiatedRefunds { get; set; } = [];
    public ICollection<WaitingList> WaitingListItems { get; set; } = [];
    public ICollection<ScanLog> ScanLogs { get; set; } = [];
    public ICollection<Ticket> ScannedTickets { get; set; } = [];
}
