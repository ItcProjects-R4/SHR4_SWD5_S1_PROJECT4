namespace EventifyPro.BLL.Services.Interfaces;

public interface ITicketTypeService
{
    Task<Result<TicketTypeResponseDto>> AddToEventAsync(int eventId, TicketTypeCreateDto dto, string organizerId, CancellationToken cancellationToken = default);
    Task<Result<TicketTypeResponseDto>> UpdateAsync(int id, TicketTypeUpdateDto dto, string organizerId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TicketTypeResponseDto>>> GetByEventAsync(int eventId, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, string organizerId, CancellationToken cancellationToken = default);
    Task<Result<TicketTypeResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int eventId, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> GetSumOfTotalQuantityByEventAsync(int eventId, int? excludeTicketTypeId = null, CancellationToken cancellationToken = default);
}

