namespace EventifyPro.BLL.DTOs.Review;

public record EventReviewsSummaryDto
{
    public int TotalReviews { get; init; }
    public double AverageRating { get; init; }
    public Dictionary<int, int> RatingDistribution { get; init; } = [];
    public Dictionary<int, int> RatingPercentages { get; init; } = [];
}
