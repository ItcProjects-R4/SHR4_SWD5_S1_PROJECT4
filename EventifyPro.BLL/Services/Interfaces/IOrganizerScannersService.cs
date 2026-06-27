using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventifyPro.BLL.DTOs.Scanner;
using EventifyPro.BLL.DTOs.Auth;
using Eventify.Shared.Wrappers;

namespace EventifyPro.BLL.Services.Interfaces
{
    public interface IOrganizerScannersService
    {
        Task<Result<PagedResult<ScannerSummaryDto>>> GetScannersListAsync(string organizerId, string? searchTerm, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<Result<List<ScannerAssignmentDto>>> GetScannerAssignmentsAsync(string scannerId, string organizerId, CancellationToken cancellationToken = default);
        Task<Result<bool>> AssignScannerToEventsAsync(string scannerId, List<int> eventIds, string organizerId, CancellationToken cancellationToken = default);
        Task<Result> CreateScannerAccountAsync(CreateScannerDto dto, string organizerId, CancellationToken cancellationToken = default);
        Task<Result<bool>> ToggleScannerActiveStatusAsync(string scannerId, string organizerId, CancellationToken cancellationToken = default);
        Task<Result<ScannerDetailsDto>> GetScannerDetailsAsync(string scannerId, string organizerId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<Result<bool>> UpdateScannerAsync(string scannerId, string fullName, string? newPassword, string organizerId, CancellationToken cancellationToken = default);
    }
}
