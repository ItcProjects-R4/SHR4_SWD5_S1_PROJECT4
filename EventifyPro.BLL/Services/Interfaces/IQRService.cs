namespace EventifyPro.BLL.Services.Interfaces;

public interface IQRService
{
    string GenerateToken(int ticketId, int bookingId);
    byte[] GeneratePngBytes(string token);
    bool VerifyToken(string token, out int ticketId, out int bookingId);
}