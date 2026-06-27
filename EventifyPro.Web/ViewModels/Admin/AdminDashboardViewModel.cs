namespace EventifyPro.Web.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public IReadOnlyList<AdminFeedbackItemViewModel> PendingFeedback { get; set; } = [];

    public IReadOnlyList<AdminFeedbackItemViewModel> ApprovedFeedback { get; set; } = [];

    public IReadOnlyList<AdminOrganizerItemViewModel> PendingOrganizers { get; set; } = [];

    public IReadOnlyList<AdminOrganizerItemViewModel> VerifiedOrganizers { get; set; } = [];
    public IReadOnlyList<Eventify.Domain.Entities.Event> PendingEvents { get; set; } = [];
}
