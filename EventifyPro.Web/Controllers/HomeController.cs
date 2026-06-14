using Eventify.Domain.Entities;
using EventifyPro.DAL.AppDatabase;
using EventifyPro.Web.Models;
using EventifyPro.Web.ViewModels;
using EventifyPro.Web.ViewModels.Feedback;
using EventifyPro.Web.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace EventifyPro.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly EventifyDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(EventifyDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var model = new HomeIndexViewModel
            {
                ApprovedFeedback = _context.Feedback
                    .AsNoTracking()
                    .Where(f => f.IsApproved)
                    .OrderByDescending(f => f.ApprovedAt ?? f.CreatedAt)
                    .Select(f => new LandingFeedbackViewModel
                    {
                        DisplayName = string.IsNullOrWhiteSpace(f.Name) ? "Eventify Pro User" : f.Name,
                        Message = f.Message,
                        CreatedAt = f.CreatedAt
                    })
                    .ToList()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult FAQs()
        {
            return View();
        }

        public IActionResult HelpCenter()
        {
            return View();
        }

        public IActionResult Feedback()
        {
            return View(new FeedbackViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(FeedbackViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Feedback", model);
            }

            var feedback = new Feedback
            {
                Name = model.Name?.Trim(),
                Email = model.Email?.Trim(),
                Message = model.Message.Trim(),
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Feedback.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["FeedbackSuccess"] = "Thanks for your feedback. It will appear on the homepage after admin approval.";
            return RedirectToAction(nameof(Feedback));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
