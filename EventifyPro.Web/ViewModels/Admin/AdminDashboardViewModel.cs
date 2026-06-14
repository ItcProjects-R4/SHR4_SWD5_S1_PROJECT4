namespace EventifyPro.Web.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public IReadOnlyList<AdminFeedbackItemViewModel> PendingFeedback { get; set; } = [];

    public IReadOnlyList<AdminFeedbackItemViewModel> ApprovedFeedback { get; set; } = [];
}
