namespace EventifyPro.BLL.DTOs.Home
{
    public record LandingPageDataDto
    {
        public List<LandingFeedbackDto> ApprovedFeedback { get; init; } = [];
        public List<CategoryDto> Categories { get; init; } = [];
        public List<EventSummaryDto> FeaturedEvents { get; init; } = [];
        public int TotalTickets { get; init; }
        public int TotalOrganizers { get; init; }
        public int TotalEvents { get; init; }
    }

    public record LandingFeedbackDto
    {
        public string DisplayName { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }

    public record FeedbackCreateDto
    {
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}
