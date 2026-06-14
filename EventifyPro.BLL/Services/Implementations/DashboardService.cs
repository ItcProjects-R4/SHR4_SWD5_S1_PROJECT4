namespace EventifyPro.BLL.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardService(IUnitOfWork unitOfWork, IMapper mapper, UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userManager = userManager;
    }

    public async Task<Result<OrganizerDashboardDto>> GetOrganizerDashboardAsync(string organizerId, CancellationToken cancellationToken = default)
    {
        // Get all events for this organizer
        var allEvents = await _unitOfWork.Events.GetAllAsync(cancellationToken);
        var organizerEvents = allEvents.Where(e => e.OrganizerId == organizerId && !e.IsDeleted).ToList();
        var activeEvents = organizerEvents.Where(e => e.Status == EventStatus.Published && DateTime.UtcNow < e.EndDate).ToList();

        // Get all bookings once for efficiency
        var allBookings = await _unitOfWork.Bookings.GetAllAsync(cancellationToken);
        var allPayments = await _unitOfWork.Payments.GetAllAsync(cancellationToken);

        // Count total bookings across all events
        var organizerBookings = allBookings.Where(b => organizerEvents.Any(e => e.Id == b.EventId)).ToList();
        var bookingIds = new HashSet<int>(organizerBookings.Select(b => b.Id));
        var totalRevenue = 0m;
        var allReviews = new List<Review>();

        foreach (var evt in organizerEvents)
        {
            var eventBookings = organizerBookings.Where(b => b.EventId == evt.Id).ToList();

            foreach (var booking in eventBookings)
            {
                // Add confirmed payment amounts to revenue
                var payment = allPayments.FirstOrDefault(p => p.BookingId == booking.Id);
                if (payment?.Status == PaymentStatus.Completed)
                    totalRevenue += payment.Amount;
            }

            // Get reviews for this event
            var reviews = await _unitOfWork.Reviews.GetReviewsByEventAsync(evt.Id, cancellationToken);
            allReviews.AddRange(reviews);
        }

        // Calculate average rating
        var averageRating = allReviews.Count > 0 ? allReviews.Average(r => r.Rating) : 0;

        // Count attendees (unique users from confirmed bookings)
        var attendeeIds = new HashSet<string>(
            organizerBookings.Where(b => b.Status == BookingStatus.Confirmed).Select(b => b.UserId)
        );

        // Get top events by revenue
        var topEvents = new List<EventSalesDto>();
        var eventRevenues = new Dictionary<int, decimal>();

        foreach (var evt in activeEvents)
        {
            var eventBookings = organizerBookings.Where(b => b.EventId == evt.Id).ToList();
            var eventRevenue = allPayments
                .Where(p => eventBookings.Any(b => b.Id == p.BookingId) && p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);
            eventRevenues[evt.Id] = eventRevenue;
        }

        foreach (var evt in activeEvents.Where(e => eventRevenues.ContainsKey(e.Id)).OrderByDescending(e => eventRevenues[e.Id]).Take(5))
        {
            var eventBookings = organizerBookings.Where(b => b.EventId == evt.Id).ToList();
            var eventRevenue = eventRevenues[evt.Id];
            var confirmedCount = eventBookings.Count(b => b.Status == BookingStatus.Confirmed);
            var totalTicketsSold = 0;

            foreach (var booking in eventBookings)
            {
                var items = await _unitOfWork.BookingItems.GetByBookingIdAsync(booking.Id, cancellationToken);
                totalTicketsSold += items.Sum(i => i.Quantity);
            }

            var eventSalesDto = new EventSalesDto
            {
                EventId = evt.Id,
                EventTitle = evt.Title,
                TotalBookings = eventBookings.Count,
                TotalTicketsSold = totalTicketsSold,
                TotalRevenue = eventRevenue,
                AverageTicketPrice = confirmedCount > 0 ? eventRevenue / confirmedCount : 0,
                StartDate = evt.StartDate
            };
            topEvents.Add(eventSalesDto);
        }

        // Booking status breakdown
        var bookingStatusBreakdown = new Dictionary<string, int>();
        foreach (var status in Enum.GetValues(typeof(BookingStatus)).Cast<BookingStatus>())
        {
            var count = organizerBookings.Count(b => b.Status == status);
            if (count > 0)
                bookingStatusBreakdown[status.ToString()] = count;
        }

        // Revenue by month
        var revenueByMonth = new Dictionary<string, decimal>();
        for (int i = 11; i >= 0; i--)
        {
            var monthStart = DateTime.UtcNow.AddMonths(-i);
            var monthKey = monthStart.ToString("yyyy-MM");

            var monthRevenue = allPayments
                .Where(p => organizerBookings.Any(b => b.Id == p.BookingId) &&
                           p.PaymentDate.Year == monthStart.Year && 
                           p.PaymentDate.Month == monthStart.Month &&
                           p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            revenueByMonth[monthKey] = monthRevenue;
        }

        var dashboard = new OrganizerDashboardDto
        {
            TotalEvents = organizerEvents.Count,
            ActiveEvents = activeEvents.Count,
            TotalBookings = bookingIds.Count,
            TotalRevenue = totalRevenue,
            AverageEventRating = averageRating,
            TotalAttendees = attendeeIds.Count,
            TopEvents = topEvents,
            BookingStatusBreakdown = bookingStatusBreakdown,
            RevenueByMonth = revenueByMonth
        };

        return Result<OrganizerDashboardDto>.Success(dashboard);
    }

    public async Task<Result<AdminDashboardDto>> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        // Get all users with count by role
        var allUsers = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        var totalUsers = allUsers.Count(u => u.IsActive);

        var usersByRole = new Dictionary<string, int>();
        foreach (var role in new[] { RoleNames.Admin, RoleNames.Organizer, RoleNames.Attendee, RoleNames.Scanner })
        {
            usersByRole[role] = 0;
        }

        foreach (var user in allUsers.Where(u => u.IsActive))
        {
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                if (usersByRole.ContainsKey(role))
                    usersByRole[role]++;
            }
        }

        // Get all events
        var allEvents = await _unitOfWork.Events.GetAllAsync(cancellationToken);
        var totalEvents = allEvents.Count(e => !e.IsDeleted);

        // Get all bookings and calculate total revenue
        var allBookings = await _unitOfWork.Bookings.GetAllAsync(cancellationToken);
        var allPayments = await _unitOfWork.Payments.GetAllAsync(cancellationToken);

        var totalRevenue = 0m;
        var totalBookings = allBookings.Count;
        var confirmedBookings = allBookings.Count(b => b.Status == BookingStatus.Confirmed);

        // Filter bookings and payments for valid events only
        var validBookings = allBookings.Where(b => allEvents.Any(e => e.Id == b.EventId && !e.IsDeleted)).ToList();
        var validPayments = allPayments.Where(p => validBookings.Any(b => b.Id == p.BookingId)).ToList();

        totalRevenue = validPayments
            .Where(p => p.Status == PaymentStatus.Completed)
            .Sum(p => p.Amount);

        // Count all tickets
        var allTickets = await _unitOfWork.Tickets.GetAllAsync(cancellationToken);
        var totalTickets = allTickets.Count;

        // Get all reviews and calculate average rating
        var allReviews = await _unitOfWork.Reviews.GetAllAsync(cancellationToken);
        var averageReviewRating = allReviews.Count > 0 
            ? allReviews.Average(r => r.Rating) 
            : 0;

        // Revenue by month
        var revenueByMonth = new Dictionary<string, decimal>();
        for (int i = 11; i >= 0; i--)
        {
            var monthStart = DateTime.UtcNow.AddMonths(-i);
            var monthKey = monthStart.ToString("yyyy-MM");

            var monthRevenue = validPayments
                .Where(p => p.PaymentDate.Year == monthStart.Year && 
                           p.PaymentDate.Month == monthStart.Month && 
                           p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            revenueByMonth[monthKey] = monthRevenue;
        }

        var dashboard = new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            TotalEvents = totalEvents,
            TotalRevenue = totalRevenue,
            TotalBookings = totalBookings,
            TotalTickets = totalTickets,
            AverageReviewRating = averageReviewRating,
            UsersByRole = usersByRole,
            RevenueByMonth = revenueByMonth
        };

        return Result<AdminDashboardDto>.Success(dashboard);
    }
}
