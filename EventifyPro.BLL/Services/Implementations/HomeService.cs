namespace EventifyPro.BLL.Services.Implementations
{
    public class HomeService : IHomeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventService _eventService;
        private readonly IMapper _mapper;

        public HomeService(IUnitOfWork unitOfWork, IEventService eventService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _eventService = eventService;
            _mapper = mapper;
        }

        public async Task<Result<LandingPageDataDto>> GetLandingPageDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Get approved feedback
                var feedback = await _unitOfWork.DbContext.Set<Feedback>()
                    .AsNoTracking()
                    .Where(f => f.IsApproved)
                    .OrderByDescending(f => f.ApprovedAt ?? f.CreatedAt)
                    .Select(f => new LandingFeedbackDto
                    {
                        DisplayName = string.IsNullOrWhiteSpace(f.Name) ? "Eventify Pro User" : f.Name,
                        Message = f.Message,
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync(cancellationToken);

                // 2. Get categories with event count
                var categoriesList = await _unitOfWork.Categories.GetQuery()
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ProjectToType<CategoryDto>()
                    .ToListAsync(cancellationToken);

                // 3. Get featured events using EventService Search
                var featuredFilter = new EventFilterDto
                {
                    Status = "Published",
                    IsFeatured = true,
                    PageNumber = 1,
                    PageSize = 3,
                    SortBy = "StartDate",
                    IsDescending = false
                };
                var featuredResult = await _eventService.SearchAsync(featuredFilter, cancellationToken);
                var featuredEvents = featuredResult.Data;

                // 4. Load database stats
                var totalTickets = await _unitOfWork.Tickets.GetQuery()
                    .CountAsync(t => t.Booking.Status == BookingStatus.Confirmed, cancellationToken);
                var totalOrganizers = await _unitOfWork.OrganizerProfiles.GetQuery().CountAsync(cancellationToken);
                var totalEvents = await _unitOfWork.Events.GetQuery()
                    .CountAsync(e => e.Status == EventStatus.Published && !e.IsDeleted, cancellationToken);

                var data = new LandingPageDataDto
                {
                    ApprovedFeedback = feedback,
                    Categories = categoriesList,
                    FeaturedEvents = featuredEvents.ToList(),
                    TotalTickets = totalTickets,
                    TotalOrganizers = totalOrganizers,
                    TotalEvents = totalEvents
                };

                return Result<LandingPageDataDto>.Success(data);
            }
            catch (Exception)
            {
                return Result<LandingPageDataDto>.Failure("Failed to retrieve landing page data.");
            }
        }

        public async Task<Result<bool>> SubmitFeedbackAsync(FeedbackCreateDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var feedback = new Feedback
                {
                    Name = System.Net.WebUtility.HtmlEncode(dto.Name?.Trim() ?? string.Empty),
                    Email = System.Net.WebUtility.HtmlEncode(dto.Email?.Trim() ?? string.Empty),
                    Message = System.Net.WebUtility.HtmlEncode(dto.Message?.Trim() ?? string.Empty),
                    IsApproved = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.DbContext.Set<Feedback>().AddAsync(feedback, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<bool>.Success(true);
            }
            catch (Exception)
            {
                return Result<bool>.Failure("Failed to submit feedback.");
            }
        }
    }
}
