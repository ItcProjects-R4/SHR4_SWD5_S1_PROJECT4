namespace EventifyPro.BLL.Services.Implementations;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IQRService _qrService;
    private readonly IScanLogService _scanLogService;

    public TicketService(IUnitOfWork unitOfWork, IMapper mapper, IQRService qrService, IScanLogService scanLogService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _qrService = qrService;
        _scanLogService = scanLogService;
    }

    public async Task<Result<IReadOnlyList<TicketResponseDto>>> GenerateForBookingAsync(int bookingId, CancellationToken cancellationToken = default)
    {
        // Get the booking with its items
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId, cancellationToken);
        if (booking == null)
            return Result<IReadOnlyList<TicketResponseDto>>.Failure("Booking not found.");

        var bookingItems = await _unitOfWork.BookingItems.GetByBookingIdAsync(bookingId, cancellationToken);

        var generatedTickets = new List<Ticket>();

        foreach (var item in bookingItems)
        {
            for (int i = 0; i < item.Quantity; i++)
            {
                generatedTickets.Add(new Ticket
                {
                    EventId = booking.EventId,
                    BookingId = bookingId,
                    TicketTypeId = item.TicketTypeId,
                    QRCode = string.Empty,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        foreach (var ticket in generatedTickets)
        {
            await _unitOfWork.Tickets.AddAsync(ticket, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dtos = new List<TicketResponseDto>();
        foreach (var ticket in generatedTickets)
        {
            ticket.QRCode = _qrService.GenerateToken(ticket.Id, bookingId);
            _unitOfWork.Tickets.Update(ticket);

            var ticketType = await _unitOfWork.TicketTypes.GetByIdAsync(ticket.TicketTypeId, cancellationToken);
            var dto = _mapper.Map<TicketResponseDto>(ticket);
            if (ticketType != null)
                dto = dto with { TicketTypeName = ticketType.Name };
            dtos.Add(dto);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<IReadOnlyList<TicketResponseDto>>.Success(dtos.AsReadOnly());
    }

    public async Task<Result<QRScanResultDto>> ValidateAndUseAsync(QRScanRequestDto dto, string scannerId, CancellationToken cancellationToken = default)
    {
        // Parse and verify QR token
        if (!_qrService.VerifyToken(dto.QRCode, out int ticketId, out int bookingId))
        {
            // Log the failed scan
            await _scanLogService.LogAsync(dto.EventId, null, null, nameof(ScanResult.InvalidToken), scannerId, dto.QRCode, cancellationToken);
            return Result<QRScanResultDto>.Success(new QRScanResultDto
            {
                IsValid = false,
                Message = "Invalid QR code token.",
                TicketId = null
            });
        }

        // Get the ticket
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            await _scanLogService.LogAsync(dto.EventId, null, null, nameof(ScanResult.InvalidToken), scannerId, dto.QRCode, cancellationToken);
            return Result<QRScanResultDto>.Success(new QRScanResultDto
            {
                IsValid = false,
                Message = "Ticket not found.",
                TicketId = null
            });
        }

        // Check if already used
        if (ticket.IsUsed)
        {
            await _scanLogService.LogAsync(dto.EventId, ticketId, ticket.EventId, nameof(ScanResult.AlreadyUsed), scannerId, dto.QRCode, cancellationToken);
            return Result<QRScanResultDto>.Success(new QRScanResultDto
            {
                IsValid = false,
                IsAlreadyUsed = true,
                Message = "This ticket has already been used.",
                TicketId = ticketId,
                FirstUsedAt = ticket.UsedAt
            });
        }

        // Check if ticket belongs to the correct event (fraud detection)
        if (ticket.EventId != dto.EventId)
        {
            await _scanLogService.LogAsync(dto.EventId, ticketId, ticket.EventId, nameof(ScanResult.WrongEvent), scannerId, dto.QRCode, cancellationToken);
            return Result<QRScanResultDto>.Success(new QRScanResultDto
            {
                IsValid = false,
                Message = "This ticket is for a different event.",
                TicketId = ticketId
            });
        }

        // Get ticket type and booking details
        var ticketType = await _unitOfWork.TicketTypes.GetByIdAsync(ticket.TicketTypeId, cancellationToken);
        var booking = await _unitOfWork.Bookings.GetByIdAsync(ticket.BookingId, cancellationToken);

        if (booking == null || booking.Status != BookingStatus.Confirmed)
        {
            await _scanLogService.LogAsync(dto.EventId, ticketId, ticket.EventId, nameof(ScanResult.InvalidToken), scannerId, dto.QRCode, cancellationToken);
            return Result<QRScanResultDto>.Success(new QRScanResultDto
            {
                IsValid = false,
                Message = "This ticket's booking is not confirmed.",
                TicketId = ticketId
            });
        }

        var user = await _unitOfWork.Users.GetByIdAsync(booking.UserId, cancellationToken);

        // Mark ticket as used
        ticket.IsUsed = true;
        ticket.UsedAt = DateTime.UtcNow;
        ticket.ScannedById = scannerId;

        _unitOfWork.Tickets.Update(ticket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Log successful scan
        await _scanLogService.LogAsync(dto.EventId, ticketId, ticket.EventId, nameof(ScanResult.Valid), scannerId, dto.QRCode, cancellationToken);

        // Return success with ticket details
        return Result<QRScanResultDto>.Success(new QRScanResultDto
        {
            IsValid = true,
            Message = "Ticket validated successfully.",
            TicketId = ticketId,
            TicketTypeName = ticketType?.Name,
            AttendeeEmail = user?.Email,
            IsAlreadyUsed = false,
            FirstUsedAt = DateTime.UtcNow
        });
    }

    public async Task<Result<TicketResponseDto>> GetByIdAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(id, cancellationToken);
        if (ticket == null)
            return Result<TicketResponseDto>.Failure("Ticket not found.");

        // Verify access: user can view their own tickets
        var booking = await _unitOfWork.Bookings.GetByIdAsync(ticket.BookingId, cancellationToken);
        if (booking?.UserId != userId)
            return Result<TicketResponseDto>.Failure("You are not authorized to view this ticket.");

        // Get scanner name if ticket was used
        var scannerName = ticket.ScannedById != null 
            ? (await _unitOfWork.Users.GetByIdAsync(ticket.ScannedById, cancellationToken))?.FullName 
            : null;

        // Get ticket type name
        var ticketType = await _unitOfWork.TicketTypes.GetByIdAsync(ticket.TicketTypeId, cancellationToken);

        var dto = _mapper.Map<TicketResponseDto>(ticket);
        dto = dto with 
        { 
            TicketTypeName = ticketType?.Name ?? string.Empty,
            UsedByName = scannerName
        };

        return Result<TicketResponseDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<TicketResponseDto>>> GetTicketsByBookingAsync(int bookingId, string userId, CancellationToken cancellationToken = default)
    {
        // Verify booking belongs to user
        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId, cancellationToken);
        if (booking == null)
            return Result<IReadOnlyList<TicketResponseDto>>.Failure("Booking not found.");

        if (booking.UserId != userId)
            return Result<IReadOnlyList<TicketResponseDto>>.Failure("You are not authorized to view these tickets.");

        // Get all tickets for booking
        var tickets = await _unitOfWork.Tickets.GetTicketsByBookingAsync(bookingId, cancellationToken);

        // Map with ticket type and scanner names
        var dtos = new List<TicketResponseDto>();
        foreach (var ticket in tickets)
        {
            var ticketType = await _unitOfWork.TicketTypes.GetByIdAsync(ticket.TicketTypeId, cancellationToken);
            var scannerName = ticket.ScannedById != null 
                ? (await _unitOfWork.Users.GetByIdAsync(ticket.ScannedById, cancellationToken))?.FullName 
                : null;

            var dto = _mapper.Map<TicketResponseDto>(ticket);
            dto = dto with 
            { 
                TicketTypeName = ticketType?.Name ?? string.Empty,
                UsedByName = scannerName
            };
            dtos.Add(dto);
        }

        return Result<IReadOnlyList<TicketResponseDto>>.Success(dtos.AsReadOnly());
    }
}
