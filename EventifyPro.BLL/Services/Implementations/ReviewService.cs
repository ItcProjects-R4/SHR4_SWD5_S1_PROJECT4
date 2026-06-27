namespace EventifyPro.BLL.Services.Implementations;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<ReviewCreateDto> _createValidator;
    private readonly IMemoryCache _cache;

    public ReviewService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<ReviewCreateDto> createValidator,
        IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _cache = cache;
    }

    public async Task<Result<ReviewResponseDto>> CreateAsync(ReviewCreateDto dto, string userId, CancellationToken cancellationToken = default)
    {
        // 1. Perform DTO validation check
        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<ReviewResponseDto>.Failure(errors);
        }

        // 2. Verify Event exists
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(dto.EventId, cancellationToken);
        if (eventEntity == null || eventEntity.IsDeleted)
        {
            return Result<ReviewResponseDto>.Failure("Event not found.");
        }


        // 4. Verify user has a confirmed booking for the event (attended)
        var hasConfirmedBooking = await _unitOfWork.Bookings.HasConfirmedBookingAsync(userId, dto.EventId, cancellationToken);
        if (!hasConfirmedBooking)
        {
            return Result<ReviewResponseDto>.Failure("You must have a confirmed booking to write a review.");
        }

        // 5. Verify user has not already reviewed this event
        var existingReview = await _unitOfWork.Reviews.GetUserEventReviewAsync(userId, dto.EventId, cancellationToken);
        if (existingReview != null)
        {
            return Result<ReviewResponseDto>.Failure("You have already reviewed this event.");
        }

        // 6. Create Review
        var review = new Review
        {
            UserId = userId,
            EventId = dto.EventId,
            Rating = dto.Rating,
            Comment = dto.Comment?.Trim(),
            IsHidden = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Reviews.AddAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Evict caches
        _cache.Remove($"AttendeeDashboard_{userId}");
        _cache.Remove($"OrganizerDashboard_{eventEntity.OrganizerId}");

        // 7. Retrieve full review details to map response correctly
        var createdReview = await _unitOfWork.Reviews.GetUserEventReviewAsync(userId, dto.EventId, cancellationToken);
        var resultDto = _mapper.Map<ReviewResponseDto>(createdReview!);

        return Result<ReviewResponseDto>.Success(resultDto);
    }

    public async Task<Result<IReadOnlyList<ReviewResponseDto>>> GetByEventAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var reviews = await _unitOfWork.Reviews.GetApprovedReviewsAsync(eventId, cancellationToken);
        var dtos = _mapper.Map<IReadOnlyList<ReviewResponseDto>>(reviews);
        return Result<IReadOnlyList<ReviewResponseDto>>.Success(dtos);
    }

    public async Task<Result> HideAsync(int id, string adminId, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetQuery()
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (review == null)
        {
            return Result.Failure("Review not found.");
        }

        review.IsHidden = true;
        _unitOfWork.Reviews.Update(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Evict caches
        _cache.Remove($"AttendeeDashboard_{review.UserId}");
        _cache.Remove($"OrganizerDashboard_{review.Event.OrganizerId}");

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var review = await _unitOfWork.Reviews.GetQuery()
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (review == null)
        {
            return Result.Failure("Review not found.");
        }

        if (review.UserId != userId && review.Event.OrganizerId != userId)
        {
            return Result.Failure("You are not authorized to delete this review.");
        }

        _unitOfWork.Reviews.Delete(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Evict caches
        _cache.Remove($"AttendeeDashboard_{review.UserId}");
        _cache.Remove($"OrganizerDashboard_{review.Event.OrganizerId}");

        return Result.Success();
    }

    public async Task<Result<OrganizerReviewsSummaryDto>> GetOrganizerReviewsAsync(
        string organizerId,
        string? searchTerm,
        int? ratingFilter,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var baseQuery = _unitOfWork.Reviews.GetQuery()
                .Where(r => r.Event.OrganizerId == organizerId && !r.Event.IsDeleted && !r.IsHidden);

            // Compute statistics
            var totalReviews = await baseQuery.CountAsync(cancellationToken);
            var averageRating = totalReviews > 0
                ? await baseQuery.AverageAsync(r => (double)r.Rating, cancellationToken)
                : 0.0;

            // Rating distribution (1 to 5)
            var distributionRaw = await baseQuery
                .GroupBy(r => (int)r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var ratingDistribution = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } };
            foreach (var dist in distributionRaw)
            {
                if (ratingDistribution.ContainsKey(dist.Rating))
                {
                    ratingDistribution[dist.Rating] = dist.Count;
                }
            }

            // Apply filters
            var filteredQuery = baseQuery;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim().ToLower();
                filteredQuery = filteredQuery.Where(r =>
                    (r.Comment != null && r.Comment.ToLower().Contains(search)) ||
                    r.Event.Title.ToLower().Contains(search) ||
                    (r.User != null && r.User.FullName != null && r.User.FullName.ToLower().Contains(search)));
            }

            if (ratingFilter.HasValue)
            {
                filteredQuery = filteredQuery.Where(r => r.Rating == ratingFilter.Value);
            }

            if (startDate.HasValue)
            {
                filteredQuery = filteredQuery.Where(r => r.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                filteredQuery = filteredQuery.Where(r => r.CreatedAt <= endOfDay);
            }

            var totalFilteredReviews = await filteredQuery.CountAsync(cancellationToken);

            var reviewsList = await filteredQuery
                .AsNoTracking()
                .Include(r => r.Event)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = reviewsList.Select(r =>
            {
                var reviewerName = r.User?.FullName ?? "Anonymous Attendee";
                var initials = string.Join("", reviewerName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(s => s[0])).ToUpper();
                if (string.IsNullOrEmpty(initials)) initials = "A";

                return new OrganizerReviewItemDto
                {
                    Id = r.Id,
                    EventId = r.EventId,
                    EventTitle = r.Event.Title,
                    AttendeeName = reviewerName,
                    AttendeeEmail = r.User?.Email ?? string.Empty,
                    AttendeeInitials = initials,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    OrganizerReply = r.OrganizerReply,
                    RepliedAt = r.RepliedAt,
                    IsFlagged = r.IsFlagged,
                    FlaggedReason = r.FlaggedReason
                };
            }).ToList();

            var summary = new OrganizerReviewsSummaryDto
            {
                SearchTerm = searchTerm,
                RatingFilter = ratingFilter,
                StartDate = startDate,
                EndDate = endDate,
                TotalReviews = totalReviews,
                AverageRating = Math.Round(averageRating, 1),
                RatingDistribution = ratingDistribution,
                Reviews = items,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalFilteredReviews / pageSize)
            };

            return Result<OrganizerReviewsSummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            return Result<OrganizerReviewsSummaryDto>.Failure($"Failed to retrieve organizer reviews: {ex.Message}");
        }
    }

    public async Task<Result> ReplyToReviewAsync(
        int reviewId,
        string organizerId,
        string replyContent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var review = await _unitOfWork.Reviews.GetQuery()
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.Id == reviewId && !r.IsHidden, cancellationToken);

            if (review == null || review.Event.OrganizerId != organizerId || review.Event.IsDeleted)
            {
                return Result.Failure("Review not found or unauthorized.");
            }

            if (string.IsNullOrWhiteSpace(replyContent))
            {
                return Result.Failure("Reply content cannot be empty.");
            }

            if (replyContent.Length > 1000)
            {
                return Result.Failure("Reply content cannot exceed 1000 characters.");
            }

            review.OrganizerReply = replyContent.Trim();
            review.RepliedAt = DateTime.UtcNow;
            review.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Reviews.Update(review);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to post reply: {ex.Message}");
        }
    }

    public async Task<Result> FlagReviewAsync(
        int reviewId,
        string organizerId,
        string flaggedReason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var review = await _unitOfWork.Reviews.GetQuery()
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.Id == reviewId && !r.IsHidden, cancellationToken);

            if (review == null || review.Event.OrganizerId != organizerId || review.Event.IsDeleted)
            {
                return Result.Failure("Review not found or unauthorized.");
            }

            if (string.IsNullOrWhiteSpace(flaggedReason))
            {
                return Result.Failure("Flag reason must be specified.");
            }

            if (flaggedReason.Length > 500)
            {
                return Result.Failure("Flag reason cannot exceed 500 characters.");
            }

            review.IsFlagged = true;
            review.FlaggedReason = flaggedReason.Trim();
            review.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Reviews.Update(review);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to flag review: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<OrganizerReviewItemDto>>> GetOrganizerReviewsForExportAsync(
        string organizerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var reviews = await _unitOfWork.Reviews.GetQuery()
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Event)
                .Where(r => r.Event.OrganizerId == organizerId && !r.Event.IsDeleted && !r.IsHidden)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            var list = reviews.Select(r =>
            {
                var reviewerName = r.User?.FullName ?? "Anonymous Attendee";
                var initials = string.Join("", reviewerName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(s => s[0])).ToUpper();
                if (string.IsNullOrEmpty(initials)) initials = "A";

                return new OrganizerReviewItemDto
                {
                    Id = r.Id,
                    EventId = r.EventId,
                    EventTitle = r.Event.Title,
                    AttendeeName = reviewerName,
                    AttendeeEmail = r.User?.Email ?? "N/A",
                    AttendeeInitials = initials,
                    Rating = r.Rating,
                    Comment = r.Comment ?? "No Comment",
                    CreatedAt = r.CreatedAt,
                    OrganizerReply = r.OrganizerReply ?? "No Reply",
                    RepliedAt = r.RepliedAt
                };
            }).ToList();

            return Result<IReadOnlyList<OrganizerReviewItemDto>>.Success(list.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<OrganizerReviewItemDto>>.Failure($"Failed to export reviews: {ex.Message}");
        }
    }

    public async Task<Result<bool>> CanUserReviewAsync(string userId, int eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
            if (eventEntity == null || eventEntity.IsDeleted)
            {
                return Result<bool>.Success(false);
            }


            var hasConfirmedBooking = await _unitOfWork.Bookings.GetQuery()
                .AnyAsync(b => b.UserId == userId && b.EventId == eventId && b.Status == Eventify.Domain.Enums.BookingStatus.Confirmed, cancellationToken);

            var alreadyReviewed = await _unitOfWork.Reviews.GetQuery()
                .AnyAsync(r => r.UserId == userId && r.EventId == eventId, cancellationToken);

            return Result<bool>.Success(hasConfirmedBooking && !alreadyReviewed);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to check review permission: {ex.Message}");
        }
    }

    public async Task<Result<EventReviewsSummaryDto>> GetEventReviewsSummaryAsync(int eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            var reviewsQuery = _unitOfWork.Reviews.GetQuery().Where(r => r.EventId == eventId && !r.IsHidden);
            var totalReviews = await reviewsQuery.CountAsync(cancellationToken);
            double averageRating = 0.0;
            var ratingDistribution = new Dictionary<int, int> { { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 } };
            var ratingPercentages = new Dictionary<int, int> { { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 } };

            if (totalReviews > 0)
            {
                averageRating = await reviewsQuery.AverageAsync(r => (double)r.Rating, cancellationToken);
                var starCounts = await reviewsQuery
                    .GroupBy(r => r.Rating)
                    .Select(g => new { Rating = (int)g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                foreach (var item in starCounts)
                {
                    if (ratingDistribution.ContainsKey(item.Rating))
                    {
                        ratingDistribution[item.Rating] = item.Count;
                        ratingPercentages[item.Rating] = (int)Math.Round((double)item.Count / totalReviews * 100);
                    }
                }
            }

            var summary = new EventReviewsSummaryDto
            {
                TotalReviews = totalReviews,
                AverageRating = averageRating,
                RatingDistribution = ratingDistribution,
                RatingPercentages = ratingPercentages
            };

            return Result<EventReviewsSummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            return Result<EventReviewsSummaryDto>.Failure($"Failed to retrieve event reviews summary: {ex.Message}");
        }
    }
}

