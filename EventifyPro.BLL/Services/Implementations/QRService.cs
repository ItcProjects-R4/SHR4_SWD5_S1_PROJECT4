namespace EventifyPro.BLL.Services.Implementations;

public class QRService : IQRService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<QRService> _logger;

    public QRService(IConfiguration configuration, ILogger<QRService> logger)
    {
        _configuration = configuration;
        _logger = logger;
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

        _logger.LogInformation("Generating QR token for Ticket ID: {TicketId}, Booking ID: {BookingId}", ticketId, bookingId);

        var payload = $"{ticketId}:{bookingId}";
        var encryptedPayload = EncryptPayload(payload);
        var secret = GetAppSecret();

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(encryptedPayload));
            var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();
            return $"{encryptedPayload}:{signature}";
        }
    }

    public byte[] GeneratePngBytes(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));
        }

        _logger.LogInformation("Generating QR PNG bytes from token.");

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
            _logger.LogWarning("QR Token verification failed: token is empty.");
            return false;
        }

        var parts = token.Split(':');
        if (parts.Length != 2)
        {
            _logger.LogWarning("QR Token verification failed: incorrect number of segments.");
            return false;
        }

        var encryptedPayload = parts[0];
        var signature = parts[1];

        string secret;
        try
        {
            secret = GetAppSecret();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "QR Token verification failed: AppSecret is not configured.");
            return false;
        }

        // Verify HMAC signature first to prevent decryption oracle attacks
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(encryptedPayload));
            var computedSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var tokenSigBytes = Encoding.UTF8.GetBytes(signature.ToLowerInvariant());
            var computedSigBytes = Encoding.UTF8.GetBytes(computedSignature);

            if (tokenSigBytes.Length != computedSigBytes.Length || 
                !CryptographicOperations.FixedTimeEquals(tokenSigBytes, computedSigBytes))
            {
                _logger.LogWarning("QR Token verification failed: HMAC signature mismatch.");
                return false;
            }
        }

        // Decrypt the payload after validating signature
        try
        {
            var decryptedPayload = DecryptPayload(encryptedPayload);
            var payloadParts = decryptedPayload.Split(':');
            if (payloadParts.Length != 2)
            {
                _logger.LogWarning("QR Token verification failed: invalid decrypted payload structure.");
                return false;
            }

            if (!int.TryParse(payloadParts[0], out var parsedTicketId) || parsedTicketId <= 0)
            {
                _logger.LogWarning("QR Token verification failed: invalid decrypted Ticket ID.");
                return false;
            }

            if (!int.TryParse(payloadParts[1], out var parsedBookingId) || parsedBookingId <= 0)
            {
                _logger.LogWarning("QR Token verification failed: invalid decrypted Booking ID.");
                return false;
            }

            ticketId = parsedTicketId;
            bookingId = parsedBookingId;
            _logger.LogInformation("QR Token verification succeeded for Ticket ID: {TicketId}, Booking ID: {BookingId}.", ticketId, bookingId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QR Token verification failed: decryption error.");
            return false;
        }
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

    private byte[] GetAesKey()
    {
        var secret = GetAppSecret();
        return SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    private string EncryptPayload(string plainText)
    {
        var key = GetAesKey();
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.GenerateIV();
            var iv = aes.IV;

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream())
            {
                // Write IV first
                ms.Write(iv, 0, iv.Length);

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }

                var cipherBytes = ms.ToArray();
                // Return URL-safe base64 without padding
                return Convert.ToBase64String(cipherBytes)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('=');
            }
        }
    }

    private string DecryptPayload(string cipherText)
    {
        var key = GetAesKey();
        // Convert URL-safe base64 back to normal base64
        var incoming = cipherText.Replace('-', '+').Replace('_', '/');
        var padding = (4 - (incoming.Length % 4)) % 4;
        incoming += new string('=', padding);
        var cipherBytes = Convert.FromBase64String(incoming);

        if (cipherBytes.Length < 16)
        {
            throw new CryptographicException("Ciphertext is too short.");
        }

        using (var aes = Aes.Create())
        {
            aes.Key = key;
            var iv = new byte[16];
            Array.Copy(cipherBytes, 0, iv, 0, 16);
            aes.IV = iv;

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream(cipherBytes, 16, cipherBytes.Length - 16))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
}