namespace EventifyPro.BLL.Hubs;

public class NotificationHub : Hub
{
    public async Task JoinOrganizerGroup(string organizerId)
    {
        if (!string.IsNullOrEmpty(organizerId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"organizer_{organizerId.ToLowerInvariant()}");
        }
    }

    public async Task LeaveOrganizerGroup(string organizerId)
    {
        if (!string.IsNullOrEmpty(organizerId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"organizer_{organizerId.ToLowerInvariant()}");
        }
    }

    public async Task JoinAttendeeGroup(string attendeeId)
    {
        if (!string.IsNullOrEmpty(attendeeId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"attendee_{attendeeId.ToLowerInvariant()}");
        }
    }

    public async Task LeaveAttendeeGroup(string attendeeId)
    {
        if (!string.IsNullOrEmpty(attendeeId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"attendee_{attendeeId.ToLowerInvariant()}");
        }
    }
}
