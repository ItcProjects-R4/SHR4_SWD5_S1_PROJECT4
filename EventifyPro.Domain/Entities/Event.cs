using Eventify.Domain.Interfaces;

namespace Eventify.Domain.Entities;

/// <summary>
/// Represents an event that can be organized and attended by users.
/// Events have ticket types, bookings, reviews, and waiting list functionality.
/// </summary>
/// <remarks>
/// Events are the core entity of the EventifyPro system. They transition through various states
/// from PendingReview to Published or Rejected, and eventually Completed or Cancelled. Events support capacity limits,
/// multiple ticket types, and comprehensive tracking of bookings and attendance.
/// </remarks>
public class Event : AuditableEntity, ISoftDelete
{
    /// <summary>
    /// Gets or sets the unique identifier for the event.
    /// </summary>
    /// <value>The primary key.</value>
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the current status of the event.
    /// </summary>
    /// <value>The event status (Draft, PendingReview, Published, Rejected, Cancelled, or Completed).</value>
    public EventStatus Status { get; set; } = EventStatus.PendingReview;

    public string? ReviewNotes { get; set; }
    public string? ReviewedByAdminId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// Gets or sets the maximum capacity of attendees for the event.
    /// </summary>
    /// <value>The maximum number of attendees, or null for unlimited capacity.</value>
    public int? MaxCapacity { get; set; }
    public int? MaxTicketsPerUser { get; set; }

    public bool IsDeleted { get; set; } = false;
    public bool IsFeatured { get; set; } = false;
    public int ViewCount { get; set; } = 0;

    /// <summary>
    /// Gets or sets the user ID of the event organizer (foreign key).
    /// </summary>
    /// <value>The organizer's user ID.</value>
    public string OrganizerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event category ID (foreign key).
    /// </summary>
    /// <value>The category identifier.</value>
    public int CategoryId { get; set; }

    // Navigation
    public ApplicationUser Organizer { get; set; } = null!;
    public ApplicationUser? ReviewedByAdmin { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<TicketType> TicketTypes { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
    public ICollection<ScanLog> ScanLogs { get; set; } = [];
    public ICollection<ScanLog> ActualEventScanLogs { get; set; } = [];
    public ICollection<WaitingList> WaitingListEntries { get; set; } = [];
    public ICollection<SavedEvent> SavedEvents { get; set; } = [];
    public ICollection<EventScanner> AssignedScanners { get; set; } = [];
}
