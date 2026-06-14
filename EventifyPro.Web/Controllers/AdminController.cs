using Eventify.Domain.Constants;
using EventifyPro.DAL.AppDatabase;
using EventifyPro.Web.ViewModels;
using EventifyPro.Web.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventifyPro.Web.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminController : Controller
    {
        private readonly EventifyDbContext _context;

        public AdminController(EventifyDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var feedback = _context.Feedback
                .AsNoTracking()
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new AdminFeedbackItemViewModel
                {
                    Id = f.Id,
                    DisplayName = string.IsNullOrWhiteSpace(f.Name) ? "Eventify Pro User" : f.Name,
                    Email = f.Email,
                    Message = f.Message,
                    IsApproved = f.IsApproved,
                    CreatedAt = f.CreatedAt
                })
                .ToList();

            var model = new AdminDashboardViewModel
            {
                PendingFeedback = feedback.Where(f => !f.IsApproved).ToList(),
                ApprovedFeedback = feedback.Where(f => f.IsApproved).Take(8).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveFeedback(int id)
        {
            var feedback = await _context.Feedback.FindAsync(id);
            if (feedback is null)
            {
                return NotFound();
            }

            feedback.IsApproved = true;
            feedback.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["AdminFeedbackSuccess"] = "Feedback approved and will now appear on the landing page.";
            return RedirectToAction(nameof(Index));
        }
    }
}
