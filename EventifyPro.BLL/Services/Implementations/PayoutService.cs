namespace EventifyPro.BLL.Services.Implementations
{
    public class PayoutService : IPayoutService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataProtector _protector;

        public PayoutService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IDataProtectionProvider protectionProvider)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _protector = protectionProvider.CreateProtector("EventifyPro.BankAccount.v1");
        }

        public async Task<Result<OrganizerPayoutSummaryDto>> GetOrganizerPayoutSummaryAsync(string organizerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.OrganizerProfile)
                    .FirstOrDefaultAsync(u => u.Id == organizerId, cancellationToken);

                if (user == null)
                {
                    return Result<OrganizerPayoutSummaryDto>.Failure("Organizer not found.");
                }

                // Calculate real earnings from completed payment sales
                var basePaymentsQuery = _unitOfWork.Payments.GetQuery()
                    .AsNoTracking()
                    .Where(p => p.Booking.Event.OrganizerId == organizerId && !p.Booking.Event.IsDeleted && p.Status == PaymentStatus.Completed);

                var totalPaymentsAmount = await basePaymentsQuery
                    .SumAsync(p => (decimal?)(p.Booking.TotalAmount - p.Booking.ServiceFee), cancellationToken) ?? 0m;

                var totalRefundsAmount = await basePaymentsQuery
                    .SelectMany(p => p.Refunds)
                    .Where(r => r.Status == RefundStatus.Completed)
                    .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

                var totalEarnings = totalPaymentsAmount - totalRefundsAmount;

                // Fetch actual database payout requests
                var dbPayouts = await _unitOfWork.DbContext.Set<PayoutRequest>()
                    .AsNoTracking()
                    .Where(p => p.OrganizerId == organizerId)
                    .OrderByDescending(p => p.RequestedAt)
                    .ToListAsync(cancellationToken);

                var completedPayouts = dbPayouts
                    .Where(p => p.Status == "Completed")
                    .Sum(p => p.Amount);

                var pendingPayouts = dbPayouts
                    .Where(p => p.Status == "Pending")
                    .Sum(p => p.Amount);

                var availableBalance = totalEarnings - completedPayouts - pendingPayouts;
                if (availableBalance < 0m) availableBalance = 0m;

                var historyList = dbPayouts.Select(p => new PayoutRequestDto
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    RequestedAt = p.RequestedAt,
                    Status = p.Status,
                    Method = p.Method
                }).ToList();

                var profile = user.OrganizerProfile;
                var isConnected = profile?.IsBankAccountConnected ?? false;
                var bankLast4 = string.Empty;
                if (isConnected && profile != null && !string.IsNullOrEmpty(profile.BankAccountNumber))
                {
                    try
                    {
                        var decrypted = _protector.Unprotect(profile.BankAccountNumber);
                        bankLast4 = decrypted.Length >= 4 ? decrypted.Substring(decrypted.Length - 4) : decrypted;
                    }
                    catch
                    {
                        bankLast4 = "****";
                    }
                }

                var summary = new OrganizerPayoutSummaryDto
                {
                    TotalEarnings = totalEarnings,
                    AvailableBalance = availableBalance,
                    PendingBalance = pendingPayouts,
                    IsBankAccountConnected = isConnected,
                    BankAccountLast4 = bankLast4,
                    PayoutHistory = historyList
                };

                return Result<OrganizerPayoutSummaryDto>.Success(summary);
            }
            catch (Exception)
            {
                return Result<OrganizerPayoutSummaryDto>.Failure("Failed to retrieve payout summary.");
            }
        }

        public async Task<Result<bool>> RequestPayoutAsync(string organizerId, decimal amount, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.OrganizerProfile)
                    .FirstOrDefaultAsync(u => u.Id == organizerId, cancellationToken);

                if (user == null || user.OrganizerProfile == null || !user.OrganizerProfile.IsBankAccountConnected)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<bool>.Failure("Please connect a bank account before requesting a payout.");
                }

                // Calculate real earnings from completed payment sales
                var basePaymentsQueryForPayout = _unitOfWork.Payments.GetQuery()
                    .AsNoTracking()
                    .Where(p => p.Booking.Event.OrganizerId == organizerId && !p.Booking.Event.IsDeleted && p.Status == PaymentStatus.Completed);

                var totalPaymentsAmountForPayout = await basePaymentsQueryForPayout
                    .SumAsync(p => (decimal?)(p.Booking.TotalAmount - p.Booking.ServiceFee), cancellationToken) ?? 0m;

                var totalRefundsAmountForPayout = await basePaymentsQueryForPayout
                    .SelectMany(p => p.Refunds)
                    .Where(r => r.Status == RefundStatus.Completed)
                    .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

                var totalEarnings = totalPaymentsAmountForPayout - totalRefundsAmountForPayout;

                // Fetch payout aggregates
                var completedPayouts = await _unitOfWork.DbContext.Set<PayoutRequest>()
                    .AsNoTracking()
                    .Where(p => p.OrganizerId == organizerId && p.Status == "Completed")
                    .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

                var pendingPayouts = await _unitOfWork.DbContext.Set<PayoutRequest>()
                    .AsNoTracking()
                    .Where(p => p.OrganizerId == organizerId && p.Status == "Pending")
                    .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

                var availableBalance = totalEarnings - completedPayouts - pendingPayouts;
                if (availableBalance < 0m) availableBalance = 0m;

                if (amount > availableBalance)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<bool>.Failure($"Insufficient funds. Your available balance is EGP {availableBalance:N2}.");
                }

                // Save payout request
                var payoutRequest = new PayoutRequest
                {
                    OrganizerId = organizerId,
                    Amount = amount,
                    Status = "Pending",
                    Method = "Bank Transfer",
                    RequestedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.DbContext.Set<PayoutRequest>().AddAsync(payoutRequest, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<bool>.Failure($"An error occurred while submitting your payout request: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ConnectBankAsync(string organizerId, string bankAccountName, string bankName, string bankAccountNumber, string bankRoutingNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.OrganizerProfile)
                    .FirstOrDefaultAsync(u => u.Id == organizerId, cancellationToken);

                if (user == null)
                {
                    return Result<bool>.Failure("Organizer not found.");
                }

                if (user.OrganizerProfile == null)
                {
                    user.OrganizerProfile = new OrganizerProfile
                    {
                        UserId = user.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                user.OrganizerProfile.BankAccountName = bankAccountName.Trim();
                user.OrganizerProfile.BankName = bankName.Trim();
                user.OrganizerProfile.BankAccountNumber = _protector.Protect(bankAccountNumber.Trim());
                user.OrganizerProfile.BankRoutingNumber = _protector.Protect(bankRoutingNumber.Trim());
                user.OrganizerProfile.IsBankAccountConnected = true;
                user.OrganizerProfile.UpdatedAt = DateTime.UtcNow;

                await _userManager.UpdateAsync(user);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to connect bank account: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DisconnectBankAsync(string organizerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userManager.Users
                    .Include(u => u.OrganizerProfile)
                    .FirstOrDefaultAsync(u => u.Id == organizerId, cancellationToken);

                if (user == null)
                {
                    return Result<bool>.Failure("Organizer not found.");
                }

                if (user.OrganizerProfile != null)
                {
                    user.OrganizerProfile.IsBankAccountConnected = false;
                    user.OrganizerProfile.BankAccountName = null;
                    user.OrganizerProfile.BankName = null;
                    user.OrganizerProfile.BankAccountNumber = null;
                    user.OrganizerProfile.BankRoutingNumber = null;
                    user.OrganizerProfile.UpdatedAt = DateTime.UtcNow;

                    await _userManager.UpdateAsync(user);
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to disconnect bank account: {ex.Message}");
            }
        }
    }
}
