namespace EventifyPro.BLL.Services.Implementations;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IQRService _qrService;
    private readonly IScanLogService _scanLogService;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<EventifyPro.BLL.Hubs.NotificationHub> _hubContext;
    private readonly IMemoryCache _cache;

    public TicketService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IQRService qrService,
        IScanLogService scanLogService,
        Microsoft.AspNetCore.SignalR.IHubContext<EventifyPro.BLL.Hubs.NotificationHub> hubContext,
        IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _qrService = qrService;
        _scanLogService = scanLogService;
        _hubContext = hubContext;
        _cache = cache;
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
                    QRCode = $"TEMP-{Guid.NewGuid():N}",
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
        int ticketId;
        int bookingId;
        bool isVerified = _qrService.VerifyToken(dto.QRCode, out ticketId, out bookingId);

        if (!isVerified)
        {
            // Fallback: Check if it's a manual entry (e.g., EVT-1001 or just 1001)
            var code = dto.QRCode.Trim();
            if (code.StartsWith("EVT-", StringComparison.OrdinalIgnoreCase))
            {
                code = code.Substring(4);
            }

            if (int.TryParse(code, out int parsedTicketId) && parsedTicketId > 0)
            {
                var ticketEntity = await _unitOfWork.Tickets.GetByIdAsync(parsedTicketId, cancellationToken);
                if (ticketEntity != null)
                {
                    ticketId = ticketEntity.Id;
                    bookingId = ticketEntity.BookingId;
                    isVerified = true;
                }
            }
        }

        if (!isVerified)
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

        // Start serializable transaction to prevent race conditions on ticket check-in
        using var transaction = await _unitOfWork.DbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        // Get the ticket
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            await _scanLogService.LogAsync(dto.EventId, null, null, nameof(ScanResult.InvalidToken), scannerId, dto.QRCode, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<QRScanResultDto>.Success(new QRScanResultDto
            {
                IsValid = false,
                Message = "Ticket not found.",
                TicketId = null
            });
        }

        // Check if scanner is authorized to scan for this event
        var scanner = await _unitOfWork.Users.GetByIdAsync(scannerId, cancellationToken);
        var eventEntity = await _unitOfWork.Events.GetByIdAsync(ticket.EventId, cancellationToken);
        if (scanner == null || !scanner.IsActive || eventEntity == null || scanner.ScannerCreatedByOrganizerId != eventEntity.OrganizerId)
        {
            await _scanLogService.LogAsync(dto.EventId, ticketId, ticket.EventId, nameof(ScanResult.InvalidToken), scannerId, dto.QRCode, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<QRScanResultDto>.Success(new QRScanResultDto
            {
                IsValid = false,
                Message = "Scanner is not authorized to scan for this event.",
                TicketId = ticketId
            });
        }

        // Check if event is cancelled
        if (eventEntity.Status == EventStatus.Cancelled)
        {
            await _scanLogService.LogAsync(dto.EventId, ticketId, ticket.EventId, nameof(ScanResult.InvalidToken), scannerId, dto.QRCode, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<QRScanResultDto>.Success(new QRScanResultDto
            {
                IsValid = false,
                Message = "This event has been cancelled.",
                TicketId = ticketId
            });
        }

        // Check if scanner is assigned to this specific event
        var isAssigned = await _unitOfWork.DbContext.EventScanners
            .AnyAsync(es => es.ScannerId == scannerId && es.EventId == ticket.EventId, cancellationToken);
        if (!isAssigned)
        {
            await _scanLogService.LogAsync(dto.EventId, ticketId, ticket.EventId, nameof(ScanResult.InvalidToken), scannerId, dto.QRCode, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<QRScanResultDto>.Success(new QRScanResultDto
            {
                IsValid = false,
                Message = "Scanner is not assigned to this event.",
                TicketId = ticketId
            });
        }

        // Check if already used
        if (ticket.IsUsed)
        {
            await _scanLogService.LogAsync(dto.EventId, ticketId, ticket.EventId, nameof(ScanResult.AlreadyUsed), scannerId, dto.QRCode, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
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
            await transaction.CommitAsync(cancellationToken);
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
            await transaction.CommitAsync(cancellationToken);
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

        // Add Ticket Scanned Notification
        var eventTitle = eventEntity.Title;
        var scannedNotification = new Notification
        {
            UserId = booking.UserId,
            Title = "Ticket Scanned",
            Message = $"Welcome! Your ticket for '{eventTitle}' was successfully scanned at the entrance gate. Enjoy the event!",
            Type = NotificationType.TicketScanned,
            IsRead = false,
            RedirectUrl = "/Attendee/Dashboard",
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Notifications.AddAsync(scannedNotification, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Log successful scan
        await _scanLogService.LogAsync(dto.EventId, ticketId, ticket.EventId, nameof(ScanResult.Valid), scannerId, dto.QRCode, cancellationToken);

        // Commit transaction
        await transaction.CommitAsync(cancellationToken);

        try
        {
            await _hubContext.Clients.Group($"organizer_{eventEntity.OrganizerId.ToLowerInvariant()}")
                .SendAsync("ReceiveTicketScanned", new
                {
                    EventId = eventEntity.Id,
                    EventTitle = eventEntity.Title,
                    TicketId = ticket.Id,
                    TicketTypeName = ticketType?.Name ?? "General Admission",
                    AttendeeName = user?.FullName ?? "Someone",
                    ScannedAt = ticket.UsedAt
                }, cancellationToken);
        }
        catch
        {
            // Ignore SignalR errors to prevent breaking check-in process
        }

        // Clear dashboard caches for instant updates
        _cache.Remove($"AttendeeDashboard_{booking.UserId}");
        _cache.Remove($"OrganizerDashboard_{eventEntity.OrganizerId}");

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

    public async Task<Result<UserTicketsSummaryDto>> GetUserTicketsAsync(string userId, string status, string search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseQuery = _unitOfWork.Tickets.GetQuery()
                .AsNoTracking()
                .Where(t => t.Booking.UserId == userId);

            // Calculate global status counts (before filtering by status/search)
            var allTickets = await baseQuery
                .Select(t => new { t.IsUsed, BookingStatus = t.Booking.Status, t.Event.EndDate })
                .ToListAsync(cancellationToken);

            int activeCount = 0;
            int usedCount = 0;
            int expiredCount = 0;
            int cancelledCount = 0;

            var now = DateTime.UtcNow;

            foreach (var ticket in allTickets)
            {
                if (ticket.BookingStatus == BookingStatus.Cancelled || ticket.BookingStatus == BookingStatus.Refunded)
                {
                    cancelledCount++;
                }
                else if (ticket.IsUsed)
                {
                    usedCount++;
                }
                else if (now > ticket.EndDate)
                {
                    expiredCount++;
                }
                else
                {
                    activeCount++;
                }
            }

            // Apply filters
            var filteredQuery = baseQuery;

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchClean = search.Trim().ToLower();
                filteredQuery = filteredQuery.Where(t => 
                    t.Event.Title.ToLower().Contains(searchClean) || 
                    t.Id.ToString().Contains(searchClean));
            }

            // Status filter
            filteredQuery = status.ToLower() switch
            {
                "cancelled" => filteredQuery.Where(t => t.Booking.Status == BookingStatus.Cancelled || t.Booking.Status == BookingStatus.Refunded),
                "used" => filteredQuery.Where(t => t.Booking.Status != BookingStatus.Cancelled && t.Booking.Status != BookingStatus.Refunded && t.IsUsed),
                "expired" => filteredQuery.Where(t => t.Booking.Status != BookingStatus.Cancelled && t.Booking.Status != BookingStatus.Refunded && !t.IsUsed && now > t.Event.EndDate),
                _ => filteredQuery.Where(t => t.Booking.Status != BookingStatus.Cancelled && t.Booking.Status != BookingStatus.Refunded && !t.IsUsed && now <= t.Event.EndDate) // "active"
            };

            var totalCount = await filteredQuery.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var paginatedTickets = await filteredQuery
                .Include(t => t.Event)
                .Include(t => t.Booking)
                .Include(t => t.TicketType)
                .Include(t => t.Scanner)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var mappedData = paginatedTickets.Select(t => {
                var dto = _mapper.Map<TicketResponseDto>(t);
                // Ensure correct status and type name are set
                return dto;
            }).ToList();

            var pagedResult = PagedResult<TicketResponseDto>.Create(mappedData, totalCount, page, pageSize);

            var summary = new UserTicketsSummaryDto
            {
                Tickets = pagedResult,
                ActiveCount = activeCount,
                UsedCount = usedCount,
                ExpiredCount = expiredCount,
                CancelledCount = cancelledCount
            };

            return Result<UserTicketsSummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            return Result<UserTicketsSummaryDto>.Failure($"Failed to retrieve user tickets: {ex.Message}");
        }
    }
}
