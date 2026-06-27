namespace EventifyPro.BLL.DTOs.Review;

public record OrganizerReviewsSummaryDto
{
    public string? SearchTerm { get; init; }
    public int? RatingFilter { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int TotalReviews { get; init; }
    public double AverageRating { get; init; }
    public Dictionary<int, int> RatingDistribution { get; init; } = new();
    public IReadOnlyList<OrganizerReviewItemDto> Reviews { get; init; } = Array.Empty<OrganizerReviewItemDto>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
