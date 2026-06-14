using Eventify.Domain.Entities;
using EventifyPro.BLL.DTOs.Review;
using EventifyPro.BLL.Services.Interfaces;
using EventifyPro.DAL.Repositories.Interfaces;
using FluentValidation;
using Mapster;

namespace EventifyPro.BLL.Services.Implementations;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<ReviewCreateDto> _createValidator;

    public ReviewService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<ReviewCreateDto> createValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
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

        // 3. Verify Event has completed
        if (eventEntity.EndDate > DateTime.UtcNow)
        {
            return Result<ReviewResponseDto>.Failure("Cannot review an event that has not ended yet.");
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
        var review = await _unitOfWork.Reviews.GetByIdAsync(id, cancellationToken);
        if (review == null)
        {
            return Result.Failure("Review not found.");
        }

        review.IsHidden = true;
        _unitOfWork.Reviews.Update(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
