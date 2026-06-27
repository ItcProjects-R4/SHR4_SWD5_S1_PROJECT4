
namespace EventifyPro.Web.Controllers;

[Authorize]
public class ReviewController : Controller
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// GET: Displays the write-review page.
    /// </summary>
    [HttpGet]
    public IActionResult Create(int eventId)
    {
        var model = new ReviewCreateViewModel
        {
            EventId = eventId
        };
        return View(model);
    }

    /// <summary>
    /// POST: Creates a review for an event.
    /// Redirects to Event Details on success.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var dto = new ReviewCreateDto
        {
            EventId = model.EventId,
            Rating = model.Rating,
            Comment = model.Comment
        };

        var result = await _reviewService.CreateAsync(dto, userId, cancellationToken);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create review.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Review submitted successfully!";
        return RedirectToAction("Details", "Events", new { id = model.EventId });
    }

    /// <summary>
    /// POST: Deletes a review. Only the author of the review can delete it.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var result = await _reviewService.DeleteAsync(id, userId, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to delete review.";
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        TempData["SuccessMessage"] = "Review deleted successfully!";
        return RedirectToAction("Details", "Events", new { id = eventId });
    }

    /// <summary>
    /// POST: Hides a review (Admin moderation).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HideReview(int id, int eventId, CancellationToken cancellationToken)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(adminId))
        {
            return Challenge();
        }

        var result = await _reviewService.HideAsync(id, adminId, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to hide review.";
        }
        else
        {
            TempData["SuccessMessage"] = "Review hidden successfully.";
        }

        return RedirectToAction("Details", "Events", new { id = eventId });
    }

    /// <summary>
    /// GET: Displays all approved (visible) reviews for a specific event.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> EventReviews(int eventId, CancellationToken cancellationToken)
    {
        ViewBag.EventId = eventId;
        var result = await _reviewService.GetByEventAsync(eventId, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to load reviews.";
            return View(Array.Empty<ReviewResponseDto>());
        }

        return View(result.Data);
    }
}
