namespace EventifyPro.BLL.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EventifyDbContext _context;
    private readonly IMemoryCache _cache;

    public DashboardService(IUnitOfWork unitOfWork, IMapper mapper, UserManager<ApplicationUser> userManager, EventifyDbContext context, IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userManager = userManager;
        _context = context;
        _cache = cache;
    }

    public async Task<Result<OrganizerDashboardDto>> GetOrganizerDashboardAsync(string organizerId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"OrganizerDashboard_{organizerId}";
        if (_cache.TryGetValue(cacheKey, out OrganizerDashboardDto? cachedDto) && cachedDto != null)
        {
            return Result<OrganizerDashboardDto>.Success(cachedDto);
        }

        // 1. Get total events count
        var totalEvents = await _context.Events
            .CountAsync(e => e.OrganizerId == organizerId && !e.IsDeleted, cancellationToken);

        // 2. Get active events count
        var now = DateTime.UtcNow;
        var activeEvents = await _context.Events
            .CountAsync(e => e.OrganizerId == organizerId && !e.IsDeleted && e.Status == EventStatus.Published && now < e.EndDate, cancellationToken);

        // 3. Get total bookings count across all events owned by organizer
        var totalBookings = await _context.Bookings
            .CountAsync(b => b.Event.OrganizerId == organizerId && !b.Event.IsDeleted, cancellationToken);

        // 4. Get total revenue (amount of completed payments for bookings of this organizer, excluding fees & refunds)
        var totalPayments = await _context.Payments
            .Where(p => p.Booking.Event.OrganizerId == organizerId && !p.Booking.Event.IsDeleted && p.Status == PaymentStatus.Completed)
            .SumAsync(p => (decimal?)(p.Booking.TotalAmount - p.Booking.ServiceFee), cancellationToken) ?? 0m;

        var totalRefunds = await _context.Payments
            .Where(p => p.Booking.Event.OrganizerId == organizerId && !p.Booking.Event.IsDeleted && p.Status == PaymentStatus.Completed)
            .SelectMany(p => p.Refunds)
            .Where(r => r.Status == RefundStatus.Completed)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

        var totalRevenue = totalPayments - totalRefunds;

        // 5. Calculate average review rating for organizer events
        var averageRating = await _context.Reviews
            .Where(r => r.Event.OrganizerId == organizerId && !r.Event.IsDeleted)
            .AnyAsync(cancellationToken)
            ? await _context.Reviews
                .Where(r => r.Event.OrganizerId == organizerId && !r.Event.IsDeleted)
                .AverageAsync(r => (double)r.Rating, cancellationToken)
            : 0.0;

        // 6. Calculate total unique attendees (unique users from confirmed bookings of this organizer)
        var totalAttendees = await _context.Bookings
            .Where(b => b.Event.OrganizerId == organizerId && !b.Event.IsDeleted && b.Status == BookingStatus.Confirmed)
            .Select(b => b.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        // 7. Get top performing events by revenue (limit 5)
        var topEventsList = await _context.Events
            .Where(e => e.OrganizerId == organizerId && !e.IsDeleted && e.Status == EventStatus.Published && now < e.EndDate)
            .Select(e => new {
                EventId = e.Id,
                EventTitle = e.Title,
                StartDate = e.StartDate,
                TotalBookings = e.Bookings.Count,
                TotalTicketsSold = e.Bookings.SelectMany(b => b.Tickets).Count(),
                TotalRevenue = e.Bookings
                    .Where(b => b.Payment != null && b.Payment!.Status == PaymentStatus.Completed)
                    .Sum(b => (decimal?)b.Payment!.Amount) ?? 0m,
                ConfirmedCount = e.Bookings.Count(b => b.Status == BookingStatus.Confirmed)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topEvents = topEventsList.Select(x => new EventSalesDto
        {
            EventId = x.EventId,
            EventTitle = x.EventTitle,
            TotalBookings = x.TotalBookings,
            TotalTicketsSold = x.TotalTicketsSold,
            TotalRevenue = x.TotalRevenue,
            AverageTicketPrice = x.ConfirmedCount > 0 ? x.TotalRevenue / x.ConfirmedCount : 0,
            StartDate = x.StartDate
        }).ToList();

        // 8. Booking status breakdown
        var bookingStatusBreakdownList = await _context.Bookings
            .Where(b => b.Event.OrganizerId == organizerId && !b.Event.IsDeleted)
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var bookingStatusBreakdown = bookingStatusBreakdownList
            .ToDictionary(x => x.Status.ToString(), x => x.Count);

        // 9. Revenue by month (last 12 months)
        var revenueByMonth = new Dictionary<string, decimal>();
        var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-11);
        twelveMonthsAgo = new DateTime(twelveMonthsAgo.Year, twelveMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var monthlyPayments = await _context.Payments
            .Where(p => p.Booking.Event.OrganizerId == organizerId && !p.Booking.Event.IsDeleted && p.Status == PaymentStatus.Completed && p.PaymentDate >= twelveMonthsAgo)
            .Select(p => new { p.PaymentDate.Year, p.PaymentDate.Month, p.Amount })
            .ToListAsync(cancellationToken);

        for (int i = 11; i >= 0; i--)
        {
            var monthStart = DateTime.UtcNow.AddMonths(-i);
            var monthKey = monthStart.ToString("yyyy-MM");

            var monthRevenue = monthlyPayments
                .Where(p => p.Year == monthStart.Year && p.Month == monthStart.Month)
                .Sum(p => p.Amount);

            revenueByMonth[monthKey] = monthRevenue;
        }

        // 10. Get active waiting list count across organizer's events
        var waitingListCount = await _context.WaitingLists
            .CountAsync(w => w.Event.OrganizerId == organizerId && !w.Event.IsDeleted && w.Status == WaitingListStatus.Waiting, cancellationToken);

        var dashboard = new OrganizerDashboardDto
        {
            TotalEvents = totalEvents,
            ActiveEvents = activeEvents,
            TotalBookings = totalBookings,
            TotalRevenue = totalRevenue,
            AverageEventRating = averageRating,
            TotalAttendees = totalAttendees,
            TopEvents = topEvents,
            BookingStatusBreakdown = bookingStatusBreakdown,
            RevenueByMonth = revenueByMonth,
            WaitingListCount = waitingListCount
        };

        _cache.Set(cacheKey, dashboard, TimeSpan.FromMinutes(5));

        return Result<OrganizerDashboardDto>.Success(dashboard);
    }

    public async Task<Result<AdminDashboardDto>> GetAdminDashboardAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"AdminDashboard_{startDate?.Ticks}_{endDate?.Ticks}";
        if (_cache.TryGetValue(cacheKey, out AdminDashboardDto? cachedDto) && cachedDto != null)
        {
            return Result<AdminDashboardDto>.Success(cachedDto);
        }

        // 1. Filtered base queries
        var userQuery = _context.Users.AsNoTracking().Where(u => u.IsActive);
        var eventQuery = _context.Events.AsNoTracking().Where(e => !e.IsDeleted);
        var bookingQuery = _context.Bookings.AsNoTracking();
        var paymentQuery = _context.Payments.AsNoTracking();
        var ticketQuery = _context.Tickets.AsNoTracking();
        var reviewQuery = _context.Reviews.AsNoTracking();

        if (startDate.HasValue)
        {
            userQuery = userQuery.Where(u => u.CreatedAt >= startDate.Value);
            eventQuery = eventQuery.Where(e => e.StartDate >= startDate.Value);
            bookingQuery = bookingQuery.Where(b => b.BookingDate >= startDate.Value);
            paymentQuery = paymentQuery.Where(p => p.PaymentDate >= startDate.Value);
            ticketQuery = ticketQuery.Where(t => t.CreatedAt >= startDate.Value);
            reviewQuery = reviewQuery.Where(r => r.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            userQuery = userQuery.Where(u => u.CreatedAt <= endDate.Value);
            eventQuery = eventQuery.Where(e => e.StartDate <= endDate.Value);
            bookingQuery = bookingQuery.Where(b => b.BookingDate <= endDate.Value);
            paymentQuery = paymentQuery.Where(p => p.PaymentDate <= endDate.Value);
            ticketQuery = ticketQuery.Where(t => t.CreatedAt <= endDate.Value);
            reviewQuery = reviewQuery.Where(r => r.CreatedAt <= endDate.Value);
        }

        // 2. Perform DB counts and sums directly (much faster!)
        var totalUsers = await userQuery.CountAsync(cancellationToken);
        var totalEvents = await eventQuery.CountAsync(cancellationToken);
        var totalBookings = await bookingQuery.CountAsync(cancellationToken);
        var totalTickets = await ticketQuery.CountAsync(cancellationToken);
        
        var totalPayments = await paymentQuery
            .Where(p => p.Status == PaymentStatus.Completed)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var totalRefunds = await paymentQuery
            .Where(p => p.Status == PaymentStatus.Completed)
            .SelectMany(p => p.Refunds)
            .Where(r => r.Status == RefundStatus.Completed)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

        var totalRevenue = totalPayments - totalRefunds;

        var averageReviewRating = await reviewQuery.AnyAsync(cancellationToken)
            ? await reviewQuery.AverageAsync(r => (double)r.Rating, cancellationToken)
            : 0.0;

        // 3. User roles distribution
        var usersByRole = new Dictionary<string, int>
        {
            { RoleNames.Admin, 0 },
            { RoleNames.Organizer, 0 },
            { RoleNames.Attendee, 0 },
            { RoleNames.Scanner, 0 }
        };

        var rolesQuery = from ur in _context.UserRoles
                         join r in _context.Roles on ur.RoleId equals r.Id
                         join u in userQuery on ur.UserId equals u.Id
                         group r by r.Name into g
                         select new { Role = g.Key, Count = g.Count() };

        var rolesList = await rolesQuery.ToListAsync(cancellationToken);
        foreach (var item in rolesList)
        {
            if (item.Role != null && usersByRole.ContainsKey(item.Role))
            {
                usersByRole[item.Role] = item.Count;
            }
        }

        // 4. Revenue by month (Single query optimization)
        var revenueStartDate = DateTime.UtcNow.AddMonths(-11);
        revenueStartDate = new DateTime(revenueStartDate.Year, revenueStartDate.Month, 1);

        var monthlyRevenueData = await _context.Payments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Completed && p.PaymentDate >= revenueStartDate)
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Total = g.Sum(p => p.Amount)
            })
            .ToListAsync(cancellationToken);

        var revenueByMonth = new Dictionary<string, decimal>();
        for (int i = 11; i >= 0; i--)
        {
            var monthStart = DateTime.UtcNow.AddMonths(-i);
            var monthKey = monthStart.ToString("yyyy-MM");

            var match = monthlyRevenueData.FirstOrDefault(m => m.Year == monthStart.Year && m.Month == monthStart.Month);
            revenueByMonth[monthKey] = match?.Total ?? 0m;
        }

        // 5. Fetch Recent Users
        var recentUsers = await _context.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new RecentUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                ProfileImageUrl = u.ProfileImageUrl,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                Role = (from ur in _context.UserRoles
                        join r in _context.Roles on ur.RoleId equals r.Id
                        where ur.UserId == u.Id
                        select r.Name).FirstOrDefault() ?? "Attendee"
            })
            .ToListAsync(cancellationToken);

        // 6. Calculate Current Month Weekly Revenue
        var now = DateTime.UtcNow;
        var startOfCurrentMonth = new DateTime(now.Year, now.Month, 1);
        var endOfCurrentMonth = startOfCurrentMonth.AddMonths(1).AddTicks(-1);

        var currentMonthPayments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Completed 
                     && p.PaymentDate >= startOfCurrentMonth 
                     && p.PaymentDate <= endOfCurrentMonth)
            .Select(p => new { p.PaymentDate.Day, p.Amount })
            .ToListAsync(cancellationToken);

        var currentMonthWeeklyRevenue = new Dictionary<int, decimal>
        {
            { 1, currentMonthPayments.Where(p => p.Day >= 1 && p.Day <= 7).Sum(p => p.Amount) },
            { 2, currentMonthPayments.Where(p => p.Day >= 8 && p.Day <= 14).Sum(p => p.Amount) },
            { 3, currentMonthPayments.Where(p => p.Day >= 15 && p.Day <= 21).Sum(p => p.Amount) },
            { 4, currentMonthPayments.Where(p => p.Day >= 22).Sum(p => p.Amount) }
        };

        // 7. Calculate Last Month Weekly Revenue
        var startOfLastMonth = startOfCurrentMonth.AddMonths(-1);
        var endOfLastMonth = startOfCurrentMonth.AddTicks(-1);

        var lastMonthPayments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Completed 
                     && p.PaymentDate >= startOfLastMonth 
                     && p.PaymentDate <= endOfLastMonth)
            .Select(p => new { p.PaymentDate.Day, p.Amount })
            .ToListAsync(cancellationToken);

        var lastMonthWeeklyRevenue = new Dictionary<int, decimal>
        {
            { 1, lastMonthPayments.Where(p => p.Day >= 1 && p.Day <= 7).Sum(p => p.Amount) },
            { 2, lastMonthPayments.Where(p => p.Day >= 8 && p.Day <= 14).Sum(p => p.Amount) },
            { 3, lastMonthPayments.Where(p => p.Day >= 15 && p.Day <= 21).Sum(p => p.Amount) },
            { 4, lastMonthPayments.Where(p => p.Day >= 22).Sum(p => p.Amount) }
        };

        var dashboard = new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            TotalEvents = totalEvents,
            TotalRevenue = totalRevenue,
            TotalBookings = totalBookings,
            TotalTickets = totalTickets,
            AverageReviewRating = averageReviewRating,
            UsersByRole = usersByRole,
            RevenueByMonth = revenueByMonth,
            RecentUsers = recentUsers,
            CurrentMonthWeeklyRevenue = currentMonthWeeklyRevenue,
            LastMonthWeeklyRevenue = lastMonthWeeklyRevenue
        };

        _cache.Set(cacheKey, dashboard, TimeSpan.FromMinutes(5));

        return Result<AdminDashboardDto>.Success(dashboard);
    }

    public async Task<Result<AttendeeDashboardDto>> GetAttendeeDashboardAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"AttendeeDashboard_{userId}";
        if (_cache.TryGetValue(cacheKey, out AttendeeDashboardDto? cachedDto) && cachedDto != null)
        {
            return Result<AttendeeDashboardDto>.Success(cachedDto);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result<AttendeeDashboardDto>.Failure("User not found.");
        }

        var now = DateTime.UtcNow;

        var bookingStats = await _context.Bookings
            .Where(b => b.UserId == userId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Confirmed = g.Count(b => b.Status == BookingStatus.Confirmed),
                Pending = g.Count(b => b.Status == BookingStatus.Pending)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var confirmedBookingsIds = await _context.Bookings
            .Where(b => b.UserId == userId && b.Status == BookingStatus.Confirmed)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var ticketStats = await _context.Tickets
            .Where(t => confirmedBookingsIds.Contains(t.BookingId))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Used = g.Count(t => t.IsUsed),
                Expired = g.Count(t => !t.IsUsed && t.Event.EndDate < now),
                Active = g.Count(t => !t.IsUsed && t.Event.EndDate >= now)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var totalReviews = await _context.Reviews.CountAsync(r => r.UserId == userId, cancellationToken);

        var recentBookings = await _context.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookingDate)
            .Take(4)
            .Select(b => new AttendeeBookingSummaryDto
            {
                Id = b.Id,
                UserId = b.UserId,
                EventId = b.EventId,
                EventTitle = b.Event.Title,
                TotalAmount = b.TotalAmount,
                Status = b.Status.ToString(),
                BookingReference = b.BookingReference,
                BookingDate = b.BookingDate
            })
            .ToListAsync(cancellationToken);

        var upcomingEvent = await _context.Bookings
            .Where(b => b.UserId == userId &&
                        b.Status == BookingStatus.Confirmed &&
                        b.Event.EndDate >= now &&
                        !b.Event.IsDeleted)
            .OrderBy(b => b.Event.StartDate)
            .Select(b => new AttendeeUpcomingEventDto
            {
                EventId = b.EventId,
                TicketId = b.Tickets.OrderBy(t => t.Id).Select(t => (int?)t.Id).FirstOrDefault(),
                Title = b.Event.Title,
                Description = b.Event.Description ?? string.Empty,
                Location = b.Event.Location ?? string.Empty,
                City = b.Event.City ?? string.Empty,
                ImageUrl = b.Event.ImageUrl,
                StartDate = b.Event.StartDate,
                TicketCount = b.Tickets.Count,
                BookingReference = b.BookingReference
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (upcomingEvent != null)
        {
            var timeLeft = upcomingEvent.StartDate - now;
            var daysRemaining = Math.Max(0, timeLeft.Days);
            var hoursRemaining = Math.Max(0, timeLeft.Hours);
            upcomingEvent = upcomingEvent with { DaysRemaining = daysRemaining, HoursRemaining = hoursRemaining };
        }

        var reviewedEventIds = await _context.Reviews
            .Where(r => r.UserId == userId)
            .Select(r => r.EventId)
            .ToListAsync(cancellationToken);

        var reviewPrompt = await _context.Bookings
            .Where(b => b.UserId == userId &&
                        b.Status == BookingStatus.Confirmed &&
                        b.Event.EndDate < now &&
                        !reviewedEventIds.Contains(b.EventId))
            .OrderByDescending(b => b.Event.EndDate)
            .Select(b => new AttendeeReviewPromptDto
            {
                EventId = b.EventId,
                EventTitle = b.Event.Title,
                EventDate = b.Event.EndDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        var bookingActivities = await _context.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookingDate)
            .Take(3)
            .Select(b => new AttendeeActivityDto
            {
                Title = b.Status == BookingStatus.Confirmed ? "Booking confirmed" : "Booking updated",
                Description = b.Event.Title,
                Icon = b.Status == BookingStatus.Confirmed ? "fa-circle-check" : "fa-receipt",
                Tone = b.Status == BookingStatus.Confirmed ? "success" : "neutral",
                Date = b.BookingDate
            })
            .ToListAsync(cancellationToken);

        var ticketActivities = await _context.Tickets
            .Where(t => confirmedBookingsIds.Contains(t.BookingId) && t.IsUsed && t.UsedAt.HasValue)
            .OrderByDescending(t => t.UsedAt)
            .Take(3)
            .Select(t => new AttendeeActivityDto
            {
                Title = "Ticket scanned",
                Description = t.Event.Title,
                Icon = "fa-qrcode",
                Tone = "purple",
                Date = t.UsedAt!.Value
            })
            .ToListAsync(cancellationToken);

        var recentActivity = bookingActivities
            .Concat(ticketActivities)
            .OrderByDescending(a => a.Date)
            .Take(5)
            .ToList();

        var completedProfileItems = 0;
        completedProfileItems += string.IsNullOrWhiteSpace(user.FullName) ? 0 : 1;
        completedProfileItems += await _userManager.IsEmailConfirmedAsync(user) ? 1 : 0;
        completedProfileItems += string.IsNullOrWhiteSpace(user.PhoneNumber) ? 0 : 1;
        completedProfileItems += string.IsNullOrWhiteSpace(user.ProfileImageUrl) ? 0 : 1;

        // Personalized Recommendations ("Events you may like")
        var userBookedCategoryIds = await _context.Bookings
            .Where(b => b.UserId == userId && b.Status == BookingStatus.Confirmed)
            .Select(b => b.Event.CategoryId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var recommendedEvents = new List<AttendeeRecommendedEventDto>();
        if (userBookedCategoryIds.Any())
        {
            recommendedEvents = await _context.Events
                .Where(e => !e.IsDeleted && e.EndDate >= now && userBookedCategoryIds.Contains(e.CategoryId))
                .Where(e => !_context.Bookings.Any(b => b.UserId == userId && b.EventId == e.Id && b.Status == BookingStatus.Confirmed))
                .OrderBy(e => e.StartDate)
                .Take(3)
                .Select(e => new AttendeeRecommendedEventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    City = e.City,
                    ImageUrl = e.ImageUrl,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    CategoryName = e.Category.Name,
                    MinPrice = e.TicketTypes.OrderBy(tt => tt.Price).Select(tt => tt.Price).FirstOrDefault(),
                    OrganizerName = e.Organizer.FullName
                })
                .ToListAsync(cancellationToken);
        }

        if (recommendedEvents.Count < 3)
        {
            var excludeIds = recommendedEvents.Select(r => r.Id).ToList();
            var fallbackEvents = await _context.Events
                .Where(e => !e.IsDeleted && e.EndDate >= now && !excludeIds.Contains(e.Id))
                .Where(e => !_context.Bookings.Any(b => b.UserId == userId && b.EventId == e.Id && b.Status == BookingStatus.Confirmed))
                .OrderBy(e => e.StartDate)
                .Take(3 - recommendedEvents.Count)
                .Select(e => new AttendeeRecommendedEventDto
                {
                    Id = e.Id,
                    Title = e.Title,
                    City = e.City,
                    ImageUrl = e.ImageUrl,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    CategoryName = e.Category.Name,
                    MinPrice = e.TicketTypes.OrderBy(tt => tt.Price).Select(tt => tt.Price).FirstOrDefault(),
                    OrganizerName = e.Organizer.FullName
                })
                .ToListAsync(cancellationToken);

            recommendedEvents.AddRange(fallbackEvents);
        }

        var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

        var dashboardDto = new AttendeeDashboardDto
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            ProfileImageUrl = user.ProfileImageUrl,
            TotalBookings = bookingStats?.Total ?? 0,
            ConfirmedBookings = bookingStats?.Confirmed ?? 0,
            PendingBookings = bookingStats?.Pending ?? 0,
            TotalTickets = ticketStats?.Total ?? 0,
            ActiveTickets = ticketStats?.Active ?? 0,
            UsedTickets = ticketStats?.Used ?? 0,
            ExpiredTickets = ticketStats?.Expired ?? 0,
            TotalReviews = totalReviews,
            HasPhoneNumber = !string.IsNullOrWhiteSpace(user.PhoneNumber),
            HasProfileImage = !string.IsNullOrWhiteSpace(user.ProfileImageUrl),
            IsEmailConfirmed = isEmailConfirmed,
            ProfileCompletionPercentage = completedProfileItems * 25,
            UpcomingEvent = upcomingEvent,
            ReviewPrompt = reviewPrompt,
            RecentActivity = recentActivity,
            RecentBookings = recentBookings,
            RecommendedEvents = recommendedEvents
        };

        _cache.Set(cacheKey, dashboardDto, TimeSpan.FromMinutes(5));

        return Result<AttendeeDashboardDto>.Success(dashboardDto);
    }
}
