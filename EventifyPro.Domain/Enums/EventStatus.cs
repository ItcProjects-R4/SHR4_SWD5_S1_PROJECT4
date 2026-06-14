namespace Eventify.Domain.Enums;

/// <summary>
/// Represents the status of an event from creation through completion or cancellation.
/// </summary>
/// <remarks>
/// Events created by organizers wait for admin review before becoming public.
/// Approved events are published for booking; rejected events keep review notes for organizer edits.
/// </remarks>
public enum EventStatus : byte
{
    /// <summary>The event is being prepared and is not yet visible to attendees.</summary>
    Draft = 0,
    /// <summary>The event is published and available for public booking.</summary>
    Published = 1,
    /// <summary>The event has been cancelled and no new bookings are accepted.</summary>
    Cancelled = 2,
    /// <summary>The event has occurred and is marked as completed.</summary>
    Completed = 3,
    /// <summary>The event is waiting for admin review.</summary>
    PendingReview = 4,
    /// <summary>The event was rejected by an admin and needs organizer changes.</summary>
    Rejected = 5
}
