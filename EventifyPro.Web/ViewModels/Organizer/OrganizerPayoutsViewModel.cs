

namespace EventifyPro.Web.ViewModels.Organizer;

public class OrganizerPayoutsViewModel
{
    public decimal TotalEarnings { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal PendingBalance { get; set; }
    public bool IsStripeConnected { get; set; }
    public bool IsBankAccountConnected { get; set; }
    public string BankAccountLast4 { get; set; } = string.Empty;

    public List<PayoutRequestViewModel> PayoutHistory { get; set; } = new();
}

public class PayoutRequestViewModel
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime RequestedAt { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Completed, Rejected
    public string Method { get; set; } = string.Empty; // Stripe Connect, Bank Transfer
}
