
namespace EventifyPro.Web.ViewModels.Review;

public class ReviewCreateViewModel
{
    [Required, Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Required, Range(1, 5)]
    public byte Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}
