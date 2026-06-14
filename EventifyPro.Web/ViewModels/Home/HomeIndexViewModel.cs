using System.Collections.Generic;
using EventifyPro.Web.ViewModels.Event;
using EventifyPro.BLL.DTOs.Category;

namespace EventifyPro.Web.ViewModels.Home
{
    public class HomeIndexViewModel
    {
        // Public Landing Page Content
        public IReadOnlyList<LandingFeedbackViewModel> ApprovedFeedback { get; set; } = [];
        public List<CategoryDto> Categories { get; set; } = [];
        public List<EventSummaryViewModel> FeaturedEvents { get; set; } = [];

        // Authenticated Dashboard Content
        public bool IsAuthenticated { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public int UpcomingEventsCount { get; set; }
        public int MyTicketsCount { get; set; }
        public int NotificationsCount { get; set; }
        public List<EventSummaryViewModel> UpcomingBookedEvents { get; set; } = [];
    }
}
