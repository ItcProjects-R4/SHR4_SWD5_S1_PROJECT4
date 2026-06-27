
namespace EventifyPro.Web.Controllers;

[Authorize(Roles = RoleNames.Organizer)]
[TypeFilter(typeof(VerifiedOrganizerFilter))]
public class OrganizerPayoutsController : Controller
{
    private readonly IPayoutService _payoutService;

    public OrganizerPayoutsController(IPayoutService payoutService)
    {
        _payoutService = payoutService;
    }

    [HttpGet("/OrganizerPayouts")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var result = await _payoutService.GetOrganizerPayoutSummaryAsync(organizerId, cancellationToken);
        if (!result.IsSuccess || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to retrieve payout summary.";
            return RedirectToAction("Index", "Dashboard");
        }

        var data = result.Data;

        var viewModel = new OrganizerPayoutsViewModel
        {
            TotalEarnings = data.TotalEarnings,
            AvailableBalance = data.AvailableBalance,
            PendingBalance = data.PendingBalance,
            IsStripeConnected = false,
            IsBankAccountConnected = data.IsBankAccountConnected,
            BankAccountLast4 = data.BankAccountLast4,
            PayoutHistory = data.PayoutHistory.Select(p => new PayoutRequestViewModel
            {
                Id = p.Id,
                Amount = p.Amount,
                RequestedAt = p.RequestedAt,
                Status = p.Status,
                Method = p.Method
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost("/OrganizerPayouts/RequestPayout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPayout(decimal amount, CancellationToken cancellationToken)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        if (amount <= 0m)
        {
            TempData["ErrorMessage"] = "Please enter a valid payout amount.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _payoutService.RequestPayoutAsync(organizerId, amount, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "An error occurred while submitting your payout request. Please try again.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Payout request of EGP {amount:N2} has been submitted successfully to administration.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/OrganizerPayouts/ConnectBank")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConnectBank(
        string bankAccountName, 
        string bankName, 
        string bankAccountNumber, 
        string bankRoutingNumber, 
        CancellationToken cancellationToken)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(bankAccountName) || 
            string.IsNullOrWhiteSpace(bankName) || 
            string.IsNullOrWhiteSpace(bankAccountNumber) || 
            string.IsNullOrWhiteSpace(bankRoutingNumber))
        {
            TempData["ErrorMessage"] = "All banking details are required.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _payoutService.ConnectBankAsync(organizerId, bankAccountName, bankName, bankAccountNumber, bankRoutingNumber, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to connect bank account.";
        }
        else
        {
            TempData["SuccessMessage"] = "Bank account connected successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/OrganizerPayouts/DisconnectBank")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisconnectBank(CancellationToken cancellationToken)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var result = await _payoutService.DisconnectBankAsync(organizerId, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to disconnect bank account.";
        }
        else
        {
            TempData["SuccessMessage"] = "Bank account disconnected successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
