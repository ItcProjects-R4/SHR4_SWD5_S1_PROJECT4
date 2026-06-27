namespace EventifyPro.BLL.Services.Interfaces;

public interface IPdfService
{
    Task<byte[]> GenerateTicketPdfAsync(int ticketId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateBookingPdfAsync(int bookingId, CancellationToken cancellationToken = default);
}
