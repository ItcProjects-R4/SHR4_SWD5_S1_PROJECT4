namespace EventifyPro.BLL.DTOs.Admin
{
    public class AdminDashboardDataDto
    {
        public IReadOnlyList<Feedback> PendingFeedback { get; set; } = [];
        public IReadOnlyList<Feedback> ApprovedFeedback { get; set; } = [];
        public IReadOnlyList<OrganizerProfile> PendingOrganizers { get; set; } = [];
        public IReadOnlyList<OrganizerProfile> VerifiedOrganizers { get; set; } = [];
        public IReadOnlyList<Eventify.Domain.Entities.Event> PendingEvents { get; set; } = [];
    }
}
