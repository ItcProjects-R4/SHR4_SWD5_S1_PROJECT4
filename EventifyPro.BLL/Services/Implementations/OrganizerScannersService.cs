namespace EventifyPro.BLL.Services.Implementations
{
    public class OrganizerScannersService : IOrganizerScannersService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuthService _authService;
        private readonly IMapper _mapper;

        public OrganizerScannersService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            IAuthService authService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _authService = authService;
            _mapper = mapper;
        }

        public async Task<Result<PagedResult<ScannerSummaryDto>>> GetScannersListAsync(string organizerId, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _userManager.Users
                    .Where(u => u.ScannerCreatedByOrganizerId == organizerId);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var search = searchTerm.Trim().ToLower();
                    query = query.Where(u => u.FullName.ToLower().Contains(search) || (u.Email != null && u.Email.ToLower().Contains(search)));
                }

                var totalScanners = await query.CountAsync(cancellationToken);
                var scannersList = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var scannerIds = scannersList.Select(s => s.Id).ToList();

                // Solve N+1: Batch load scan counts
                var scanCounts = await _unitOfWork.DbContext.Set<ScanLog>()
                    .Where(s => scannerIds.Contains(s.ScannedById))
                    .GroupBy(s => s.ScannedById)
                    .Select(g => new { ScannerId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ScannerId, x => x.Count, cancellationToken);

                // Solve N+1: Batch load latest scan details
                var latestScanLogs = await _unitOfWork.DbContext.Set<ScanLog>()
                    .AsNoTracking()
                    .Include(s => s.ScanEvent)
                    .Where(s => scannerIds.Contains(s.ScannedById))
                    .Where(s => s.Id == _unitOfWork.DbContext.Set<ScanLog>()
                        .Where(sub => sub.ScannedById == s.ScannedById)
                        .OrderByDescending(sub => sub.ScannedAt)
                        .Select(sub => sub.Id)
                        .FirstOrDefault())
                    .ToDictionaryAsync(s => s.ScannedById, s => s, cancellationToken);

                var dtos = scannersList.Select(scanner =>
                {
                    var totalScans = scanCounts.TryGetValue(scanner.Id, out var count) ? count : 0;
                    var lastScan = latestScanLogs.TryGetValue(scanner.Id, out var scan) ? scan : null;

                    return new ScannerSummaryDto
                    {
                        Id = scanner.Id,
                        FullName = scanner.FullName,
                        Email = scanner.Email ?? string.Empty,
                        CreatedAt = scanner.CreatedAt,
                        IsActive = scanner.IsActive,
                        TotalScans = totalScans,
                        LastScannedEventTitle = lastScan?.ScanEvent?.Title,
                        LastScannedAt = lastScan?.ScannedAt,
                        LastScanStatus = lastScan != null ? lastScan.Result.ToString() : null
                    };
                }).ToList();

                var pagedResult = PagedResult<ScannerSummaryDto>.Create(dtos, totalScanners, page, pageSize);
                return Result<PagedResult<ScannerSummaryDto>>.Success(pagedResult);
            }
            catch (Exception)
            {
                return Result<PagedResult<ScannerSummaryDto>>.Failure("Failed to retrieve scanners list.");
            }
        }

        public async Task<Result<List<ScannerAssignmentDto>>> GetScannerAssignmentsAsync(string scannerId, string organizerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var scanner = await _userManager.FindByIdAsync(scannerId);
                if (scanner == null || scanner.ScannerCreatedByOrganizerId != organizerId)
                {
                    return Result<List<ScannerAssignmentDto>>.Failure("Scanner not found.");
                }

                var events = await _unitOfWork.Events.GetQuery()
                    .AsNoTracking()
                    .Where(e => e.OrganizerId == organizerId && !e.IsDeleted && e.Status != EventStatus.Cancelled)
                    .OrderByDescending(e => e.StartDate)
                    .ToListAsync(cancellationToken);

                var assignedEventIds = await _unitOfWork.DbContext.Set<EventScanner>()
                    .Where(es => es.ScannerId == scannerId)
                    .Select(es => es.EventId)
                    .ToListAsync(cancellationToken);

                var list = events.Select(e => new ScannerAssignmentDto
                {
                    EventId = e.Id,
                    EventTitle = e.Title,
                    EventStartDate = e.StartDate,
                    IsAssigned = assignedEventIds.Contains(e.Id)
                }).ToList();

                return Result<List<ScannerAssignmentDto>>.Success(list);
            }
            catch (Exception)
            {
                return Result<List<ScannerAssignmentDto>>.Failure("Failed to retrieve scanner assignments.");
            }
        }

        public async Task<Result<bool>> AssignScannerToEventsAsync(string scannerId, List<int> eventIds, string organizerId, CancellationToken cancellationToken = default)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var scanner = await _userManager.FindByIdAsync(scannerId);
                if (scanner == null || scanner.ScannerCreatedByOrganizerId != organizerId)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<bool>.Failure("Scanner not found.");
                }

                var validEventIds = await _unitOfWork.Events.GetQuery()
                    .Where(e => e.OrganizerId == organizerId && !e.IsDeleted && e.Status != EventStatus.Cancelled)
                    .Select(e => e.Id)
                    .ToListAsync(cancellationToken);

                var filteredEventIds = eventIds.Where(eid => validEventIds.Contains(eid)).ToList();

                var oldAssignments = await _unitOfWork.DbContext.Set<EventScanner>()
                    .Where(es => es.ScannerId == scannerId)
                    .ToListAsync(cancellationToken);
                _unitOfWork.DbContext.Set<EventScanner>().RemoveRange(oldAssignments);

                foreach (var eventId in filteredEventIds)
                {
                    await _unitOfWork.DbContext.Set<EventScanner>().AddAsync(new EventScanner
                    {
                        ScannerId = scannerId,
                        EventId = eventId,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<bool>.Failure($"Failed to update scanner assignments: {ex.Message}");
            }
        }

        public async Task<Result> CreateScannerAccountAsync(CreateScannerDto dto, string organizerId, CancellationToken cancellationToken = default)
        {
            return await _authService.CreateScannerForOrganizerAsync(dto, organizerId);
        }

        public async Task<Result<bool>> ToggleScannerActiveStatusAsync(string scannerId, string organizerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var scanner = await _userManager.FindByIdAsync(scannerId);
                if (scanner == null || scanner.ScannerCreatedByOrganizerId != organizerId)
                {
                    return Result<bool>.Failure("Scanner not found.");
                }

                scanner.IsActive = !scanner.IsActive;
                scanner.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(scanner);
                if (!result.Succeeded)
                {
                    return Result<bool>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                await _userManager.UpdateSecurityStampAsync(scanner);
                return Result<bool>.Success(scanner.IsActive);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to toggle scanner status: {ex.Message}");
            }
        }

        public async Task<Result<ScannerDetailsDto>> GetScannerDetailsAsync(string scannerId, string organizerId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                var scanner = await _userManager.FindByIdAsync(scannerId);
                if (scanner == null || scanner.ScannerCreatedByOrganizerId != organizerId)
                {
                    return Result<ScannerDetailsDto>.Failure("Scanner not found.");
                }

                var scanLogsQuery = _unitOfWork.DbContext.Set<ScanLog>()
                    .AsNoTracking()
                    .Include(s => s.ScanEvent)
                    .Include(s => s.Ticket)
                        .ThenInclude(t => t!.Booking)
                            .ThenInclude(b => b.User)
                    .Where(s => s.ScannedById == scannerId);

                var totalScans = await scanLogsQuery.CountAsync(cancellationToken);
                var validScans = await scanLogsQuery.CountAsync(s => s.Result == ScanResult.Valid, cancellationToken);
                var invalidScans = totalScans - validScans;

                var logsList = await scanLogsQuery
                    .OrderByDescending(s => s.ScannedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var mappedLogs = logsList.Select(s => _mapper.Map<ScanLogResponseDto>(s)).ToList();
                var pagedLogs = PagedResult<ScanLogResponseDto>.Create(mappedLogs, totalScans, page, pageSize);

                var details = new ScannerDetailsDto
                {
                    Id = scanner.Id,
                    FullName = scanner.FullName,
                    Email = scanner.Email ?? string.Empty,
                    IsActive = scanner.IsActive,
                    CreatedAt = scanner.CreatedAt,
                    TotalScans = totalScans,
                    ValidScans = validScans,
                    InvalidScans = invalidScans,
                    ScanLogs = pagedLogs
                };

                return Result<ScannerDetailsDto>.Success(details);
            }
            catch (Exception ex)
            {
                return Result<ScannerDetailsDto>.Failure($"Failed to retrieve scanner details: {ex.Message}");
            }
        }

        public async Task<Result<bool>> UpdateScannerAsync(string scannerId, string fullName, string? newPassword, string organizerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var scanner = await _userManager.FindByIdAsync(scannerId);
                if (scanner == null || scanner.ScannerCreatedByOrganizerId != organizerId)
                {
                    return Result<bool>.Failure("Scanner not found.");
                }

                scanner.FullName = fullName;
                scanner.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(scanner);
                if (!result.Succeeded)
                {
                    return Result<bool>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(scanner);
                    var resetResult = await _userManager.ResetPasswordAsync(scanner, token, newPassword);
                    if (!resetResult.Succeeded)
                    {
                        return Result<bool>.Failure(string.Join(", ", resetResult.Errors.Select(e => e.Description)));
                    }
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to update scanner: {ex.Message}");
            }
        }
    }
}
