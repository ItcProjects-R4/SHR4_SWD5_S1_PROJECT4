namespace EventifyPro.BLL.Services.Interfaces;

public interface ITicketTypeService
{
    Task<Result<TicketTypeResponseDto>> AddToEventAsync(int eventId, TicketTypeCreateDto dto, string organizerId, CancellationToken cancellationToken = default);
    Task<Result<TicketTypeResponseDto>> UpdateAsync(int id, TicketTypeUpdateDto dto, string organizerId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TicketTypeResponseDto>>> GetByEventAsync(int eventId, CancellationToken cancellationToken = default);
}
