using DinkToPdf;
using DinkToPdf.Contracts;
using EventifyPro.BLL.Services.Interfaces;
using EventifyPro.DAL.Repositories.Interfaces;

namespace EventifyPro.BLL.Services.Implementations;

public class PdfService : IPdfService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQRService _qrService;
    private readonly IConverter _pdfConverter;

    public PdfService(IUnitOfWork unitOfWork, IQRService qrService, IConverter pdfConverter)
    {
        _unitOfWork = unitOfWork;
        _qrService = qrService;
        _pdfConverter = pdfConverter;
    }

    public async Task<byte[]> GenerateTicketPdfAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        // 1. Retrieve ticket and related data
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            throw new ArgumentException($"Ticket not found with ID {ticketId}.");
        }

        var eventEntity = await _unitOfWork.Events.GetByIdAsync(ticket.EventId, cancellationToken);
        if (eventEntity == null)
        {
            throw new ArgumentException($"Event not found with ID {ticket.EventId}.");
        }

        var category = await _unitOfWork.Categories.GetByIdAsync(eventEntity.CategoryId, cancellationToken);
        var ticketType = await _unitOfWork.TicketTypes.GetByIdAsync(ticket.TicketTypeId, cancellationToken);
        var booking = await _unitOfWork.Bookings.GetByIdAsync(ticket.BookingId, cancellationToken);
        var user = booking != null ? await _unitOfWork.Users.GetByIdAsync(booking.UserId, cancellationToken) : null;

        // 2. Generate QR Code image and encode to base64 Data URI
        var qrToken = string.IsNullOrWhiteSpace(ticket.QRCode)
            ? _qrService.GenerateToken(ticket.Id, ticket.BookingId)
            : ticket.QRCode;

        var qrCodeBytes = _qrService.GeneratePngBytes(qrToken);
        var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
        var qrCodeImgSrc = $"data:image/png;base64,{qrCodeBase64}";

        // 3. Populate ticket metadata
        var eventTitle = eventEntity.Title;
        var categoryName = category?.Name ?? "General";
        var attendeeName = user?.FullName ?? "Valued Attendee";
        var eventDate = eventEntity.StartDate.ToString("dd MMM yyyy, hh:mm tt") + " - " + eventEntity.EndDate.ToString("hh:mm tt");
        var location = eventEntity.Location;
        var city = eventEntity.City;
        var ticketTypeName = ticketType?.Name ?? "Standard";
        var price = ticketType?.Price.ToString("F2") ?? "0.00";
        var bookingRef = booking?.BookingReference ?? "N/A";

        // 4. Construct beautiful HTML layout with inline CSS
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{
            font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #fafaff;
            color: #1f1b2e;
        }}
        .ticket-container {{
            max-width: 700px;
            margin: 40px auto;
            border-radius: 16px;
            overflow: hidden;
            border: 1px solid #ede9fe;
            background-color: #ffffff;
        }}
        .header {{
            background: linear-gradient(135deg, #5b21b6 0%, #7c3aed 100%);
            padding: 30px;
            color: #ffffff;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: 800;
        }}
        .header .category-badge {{
            background-color: rgba(255, 255, 255, 0.2);
            padding: 6px 12px;
            border-radius: 50px;
            font-size: 11px;
            text-transform: uppercase;
            font-weight: bold;
            display: inline-block;
            margin-top: 8px;
        }}
        .ticket-body {{
            padding: 30px;
            width: 100%;
        }}
        .info-col {{
            width: 60%;
            float: left;
        }}
        .qr-col {{
            width: 35%;
            float: right;
            text-align: center;
            border-left: 2px dashed #ede9fe;
            padding-left: 20px;
        }}
        .detail-row {{
            margin-bottom: 20px;
        }}
        .detail-label {{
            font-size: 11px;
            color: #6b7280;
            text-transform: uppercase;
            font-weight: bold;
            margin-bottom: 4px;
        }}
        .detail-value {{
            font-size: 15px;
            font-weight: 600;
        }}
        .qr-img {{
            width: 150px;
            height: 150px;
            border: 4px solid #ffffff;
            border-radius: 8px;
            margin-bottom: 12px;
        }}
        .qr-tip {{
            font-size: 10px;
            color: #6b7280;
            text-align: center;
            line-height: 1.4;
        }}
        .footer {{
            background-color: #f5f3ff;
            padding: 15px 30px;
            border-top: 1px solid #ede9fe;
            font-size: 11px;
            color: #6b7280;
            clear: both;
        }}
        .footer-left {{
            float: left;
        }}
        .footer-right {{
            float: right;
        }}
        .clearfix::after {{
            content: """";
            clear: both;
            display: table;
        }}
    </style>
</head>
<body>
    <div class=""ticket-container"">
        <div class=""header"">
            <h1>{eventTitle}</h1>
            <span class=""category-badge"">{categoryName}</span>
        </div>
        <div class=""ticket-body clearfix"">
            <div class=""info-col"">
                <div class=""detail-row"">
                    <div class=""detail-label"">Attendee Name</div>
                    <div class=""detail-value"">{attendeeName}</div>
                </div>
                <div class=""detail-row"">
                    <div class=""detail-label"">Date & Time</div>
                    <div class=""detail-value"">{eventDate}</div>
                </div>
                <div class=""detail-row"">
                    <div class=""detail-label"">Location</div>
                    <div class=""detail-value"">{location}, {city}</div>
                </div>
                <div class=""clearfix"">
                    <div class=""detail-row"" style=""width: 50%; float: left;"">
                        <div class=""detail-label"">Ticket Type</div>
                        <div class=""detail-value"">{ticketTypeName}</div>
                    </div>
                    <div class=""detail-row"" style=""width: 50%; float: left;"">
                        <div class=""detail-label"">Price</div>
                        <div class=""detail-value"">{price} EGP</div>
                    </div>
                </div>
            </div>
            <div class=""qr-col"">
                <img class=""qr-img"" src=""{qrCodeImgSrc}"" alt=""Ticket QR Code"" />
                <div class=""qr-tip"">Present this QR code at the door for entry verification</div>
            </div>
        </div>
        <div class=""footer clearfix"">
            <div class=""footer-left"">
                Booking Reference: <strong>{bookingRef}</strong>
            </div>
            <div class=""footer-right"">
                Ticket ID: <strong>{ticketId}</strong>
            </div>
        </div>
    </div>
</body>
</html>
";

        // 5. Convert HTML template to PDF bytes using DinkToPdf Converter
        var doc = new HtmlToPdfDocument
        {
            GlobalSettings =
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
            Objects =
            {
                new ObjectSettings
                {
                    PagesCount = true,
                    HtmlContent = html,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
        };

        var pdfBytes = _pdfConverter.Convert(doc);
        return pdfBytes;
    }
}
