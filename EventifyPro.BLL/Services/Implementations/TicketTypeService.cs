namespace EventifyPro.BLL.Services.Implementations;

public class TicketTypeService : ITicketTypeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IValidator<TicketTypeCreateDto> _createValidator;
    private readonly IValidator<TicketTypeUpdateDto> _updateValidator;

    public TicketTypeService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidator<TicketTypeCreateDto> createValidator,
        IValidator<TicketTypeUpdateDto> updateValidator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Result<TicketTypeResponseDto>> AddToEventAsync(int eventId, TicketTypeCreateDto dto, string organizerId, CancellationToken cancellationToken = default)
    {
        var validationError = await _createValidator.GetValidationErrorAsync(dto with { EventId = eventId }, cancellationToken);
        if (validationError is not null)
            return Result<TicketTypeResponseDto>.Failure(validationError);

        // Verify event exists and belongs to organizer
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken);
        if (eventEntity == null)
            return Result<TicketTypeResponseDto>.Failure("Event not found.");

        if (eventEntity.OrganizerId != organizerId)
            return Result<TicketTypeResponseDto>.Failure("You are not authorized to add ticket types to this event.");

        // Check for duplicate ticket type name within the event
        var existingTicketTypes = await _unitOfWork.TicketTypes.GetTicketTypesByEventAsync(eventId, cancellationToken);
        if (existingTicketTypes.Any(tt => tt.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase)))
            return Result<TicketTypeResponseDto>.Failure("A ticket type with this name already exists for this event.");

        // Create the ticket type entity
        var ticketType = new TicketType
        {
            EventId = eventId,
            Name = dto.Name.Trim(),
            Price = dto.Price,
            TotalQuantity = dto.TotalQuantity,
            SoldQuantity = 0,
            Description = dto.Description?.Trim(),
            SaleStartDate = dto.SaleStartDate,
            SaleEndDate = dto.SaleEndDate,
            CreatedAt = DateTime.UtcNow
        };

        // Add and save
        await _unitOfWork.TicketTypes.AddAsync(ticketType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map and return
        var result = _mapper.Map<TicketTypeResponseDto>(ticketType);
        return Result<TicketTypeResponseDto>.Success(result);
    }

    public async Task<Result<TicketTypeResponseDto>> UpdateAsync(int id, TicketTypeUpdateDto dto, string organizerId, CancellationToken cancellationToken = default)
    {
        var validationError = await _updateValidator.GetValidationErrorAsync(dto with { Id = id }, cancellationToken);
        if (validationError is not null)
            return Result<TicketTypeResponseDto>.Failure(validationError);

        // Get the ticket type
        var ticketType = await _unitOfWork.TicketTypes.GetByIdAsync(id, cancellationToken);
        if (ticketType == null)
            return Result<TicketTypeResponseDto>.Failure("Ticket type not found.");

        // Verify event and authorization
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(ticketType.EventId, cancellationToken);
        if (eventEntity == null)
            return Result<TicketTypeResponseDto>.Failure("Associated event not found.");

        if (eventEntity.OrganizerId != organizerId)
            return Result<TicketTypeResponseDto>.Failure("You are not authorized to update this ticket type.");

        // Check for duplicate name (excluding current ticket type)
        var existingTicketTypes = await _unitOfWork.TicketTypes.GetTicketTypesByEventAsync(ticketType.EventId, cancellationToken);
        if (existingTicketTypes.Any(tt => tt.Id != id && tt.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase)))
            return Result<TicketTypeResponseDto>.Failure("Another ticket type with this name already exists for this event.");

        // Cannot reduce total quantity below what's already sold
        if (dto.TotalQuantity < ticketType.SoldQuantity)
            return Result<TicketTypeResponseDto>.Failure($"Cannot reduce total quantity below {ticketType.SoldQuantity} sold tickets.");

        // Update the ticket type
        ticketType.Name = dto.Name.Trim();
        ticketType.Price = dto.Price;
        ticketType.TotalQuantity = dto.TotalQuantity;
        ticketType.Description = dto.Description?.Trim();
        ticketType.SaleStartDate = dto.SaleStartDate;
        ticketType.SaleEndDate = dto.SaleEndDate;
        ticketType.UpdatedAt = DateTime.UtcNow;

        try
        {
            _unitOfWork.TicketTypes.Update(ticketType);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<TicketTypeResponseDto>.Failure("This ticket type was modified by another user. Please refresh and try again.");
        }

        // Map and return
        var result = _mapper.Map<TicketTypeResponseDto>(ticketType);
        return Result<TicketTypeResponseDto>.Success(result);
    }

    public async Task<Result<IReadOnlyList<TicketTypeResponseDto>>> GetByEventAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var ticketTypes = await _unitOfWork.TicketTypes.GetTicketTypesByEventAsync(eventId, cancellationToken);
        var dtos = _mapper.Map<IReadOnlyList<TicketTypeResponseDto>>(ticketTypes);
        return Result<IReadOnlyList<TicketTypeResponseDto>>.Success(dtos);
    }
}
