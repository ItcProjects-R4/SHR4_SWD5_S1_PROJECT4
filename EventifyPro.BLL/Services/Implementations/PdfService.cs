namespace EventifyPro.BLL.Services.Implementations;

public class PdfService : IPdfService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQRService _qrService;

    public PdfService(IUnitOfWork unitOfWork, IQRService qrService)
    {
        _unitOfWork = unitOfWork;
        _qrService = qrService;
    }

    public Task<byte[]> GenerateTicketPdfAsync(int ticketId, CancellationToken cancellationToken = default) => 
        throw new NotImplementedException();
}
