namespace EventifyPro.BLL.Services.Implementations;

public class QRService : IQRService
{
    public QRService()
    {
    }

    public string GenerateToken(int ticketId, int bookingId) => 
        throw new NotImplementedException();

    public byte[] GeneratePngBytes(string token) => 
        throw new NotImplementedException();
}
