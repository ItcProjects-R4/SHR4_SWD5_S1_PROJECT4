namespace EventifyPro.BLL.Services.Implementations;

public class QRService : IQRService
{
    private readonly IConfiguration _configuration;

    public QRService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(int ticketId, int bookingId)
    {
        if (ticketId <= 0)
        {
            throw new ArgumentException("Ticket ID must be greater than zero.", nameof(ticketId));
        }

        if (bookingId <= 0)
        {
            throw new ArgumentException("Booking ID must be greater than zero.", nameof(bookingId));
        }

        var secret = GetAppSecret();
        var payload = $"{ticketId}:{bookingId}";

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();
            return $"{payload}:{signature}";
        }
    }

    public byte[] GeneratePngBytes(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));
        }

        // Validate the token to make sure we don't render QR codes for forged tokens
        if (!VerifyToken(token, out _, out _))
        {
            throw new ArgumentException("Token is invalid or has an incorrect signature.", nameof(token));
        }

        using (var qrGenerator = new QRCodeGenerator())
        using (var qrCodeData = qrGenerator.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q))
        {
            var qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }
    }

    public bool VerifyToken(string token, out int ticketId, out int bookingId)
    {
        ticketId = 0;
        bookingId = 0;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split(':');
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var parsedTicketId) || parsedTicketId <= 0)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var parsedBookingId) || parsedBookingId <= 0)
        {
            return false;
        }

        string secret;
        try
        {
            secret = GetAppSecret();
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        var payload = $"{parsedTicketId}:{parsedBookingId}";

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var tokenSigBytes = Encoding.UTF8.GetBytes(parts[2].ToLowerInvariant());
            var computedSigBytes = Encoding.UTF8.GetBytes(computedSignature);

            if (tokenSigBytes.Length == computedSigBytes.Length && 
                CryptographicOperations.FixedTimeEquals(tokenSigBytes, computedSigBytes))
            {
                ticketId = parsedTicketId;
                bookingId = parsedBookingId;
                return true;
            }
        }

        return false;
    }

    private string GetAppSecret()
    {
        var secret = _configuration["AppSecret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("AppSecret is not configured.");
        }

        return secret;
    }
}