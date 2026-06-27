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
        var eventDate = eventEntity.StartDate.ToEgyptTime().ToString("dd MMM yyyy, hh:mm tt") + " - " + eventEntity.EndDate.ToEgyptTime().ToString("hh:mm tt");
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
    <link href=""https://fonts.googleapis.com/css2?family=Outfit:wght@400;600;700;800&family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap"" rel=""stylesheet"">
    <style>
        body {{
            font-family: 'Plus Jakarta Sans', sans-serif;
            margin: 0;
            padding: 30px;
            background-color: #f1f5f9;
            color: #1e293b;
            -webkit-print-color-adjust: exact;
        }}
        .ticket-container {{
            position: relative;
            width: 700px;
            margin: 40px auto;
        }}
        .ticket-card {{
            width: 100%;
            height: 380px;
            background-color: #ffffff;
            border: 1px solid #e2e8f0;
            border-radius: 20px;
            box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.05), 0 8px 10px -6px rgba(0, 0, 0, 0.05);
            border-collapse: separate;
            border-spacing: 0;
            overflow: hidden;
        }}
        .left-section {{
            width: 480px;
            vertical-align: top;
            background-color: #ffffff;
            border-top-left-radius: 19px;
            border-bottom-left-radius: 19px;
            padding: 0;
        }}
        .header-banner {{
            background-color: #4f46e5;
            background: -webkit-linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%);
            background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%);
            padding: 15px 30px;
            color: #ffffff;
            height: 100px;
            box-sizing: border-box;
            position: relative;
            border-top-left-radius: 19px;
        }}
        .brand {{
            font-family: 'Outfit', sans-serif;
            font-size: 10px;
            font-weight: 800;
            text-transform: uppercase;
            letter-spacing: 1.5px;
            color: rgba(255, 255, 255, 0.8);
            margin-bottom: 4px;
        }}
        .event-title {{
            font-family: 'Outfit', sans-serif;
            font-size: 20px;
            font-weight: 800;
            margin: 0 0 6px 0;
            line-height: 1.25;
            word-wrap: break-word;
            max-height: 50px;
            overflow: hidden;
        }}
        .category-badge {{
            background-color: rgba(255, 255, 255, 0.18);
            padding: 3px 10px;
            border-radius: 50px;
            font-size: 9px;
            text-transform: uppercase;
            font-weight: 700;
            display: inline-block;
            letter-spacing: 0.5px;
        }}
        .details-section {{
            padding: 20px 30px;
            box-sizing: border-box;
        }}
        .details-row {{
            margin-bottom: 15px;
            width: 100%;
        }}
        .col-58 {{
            width: 58%;
            float: left;
        }}
        .col-42 {{
            width: 42%;
            float: left;
        }}
        .col-100 {{
            width: 100%;
            float: left;
        }}
        .col-33 {{
            width: 33%;
            float: left;
        }}
        .col-34 {{
            width: 34%;
            float: left;
        }}
        .clear {{
            clear: both;
        }}
        .label {{
            font-size: 9px;
            color: #64748b;
            text-transform: uppercase;
            font-weight: 700;
            letter-spacing: 0.8px;
            margin-bottom: 2px;
        }}
        .value {{
            font-size: 12px;
            font-weight: 600;
            color: #0f172a;
        }}
        .right-section {{
            width: 220px;
            vertical-align: top;
            background-color: #f8fafc;
            border-left: 2px dashed #cbd5e1;
            padding: 24px 20px;
            text-align: center;
            position: relative;
            border-top-right-radius: 19px;
            border-bottom-right-radius: 19px;
        }}
        .qr-wrapper {{
            background: #ffffff;
            padding: 10px;
            border-radius: 12px;
            display: inline-block;
            border: 1px solid #e2e8f0;
            margin-top: 15px;
            margin-bottom: 15px;
        }}
        .qr-image {{
            width: 130px;
            height: 130px;
            display: block;
        }}
        .scan-text {{
            font-family: 'Outfit', sans-serif;
            font-size: 11px;
            font-weight: 700;
            color: #7c3aed;
            text-transform: uppercase;
            letter-spacing: 1px;
            margin-bottom: 5px;
        }}
        .scan-desc {{
            font-size: 9px;
            color: #64748b;
            line-height: 1.3;
        }}
        .ticket-id {{
            font-family: 'Outfit', sans-serif;
            font-size: 11px;
            font-weight: 700;
            color: #0f172a;
            position: absolute;
            bottom: 24px;
            width: 100%;
            left: 0;
            text-align: center;
        }}
        .notch-top {{
            position: absolute;
            top: -11px;
            right: 209px;
            width: 22px;
            height: 22px;
            background-color: #f1f5f9;
            border-radius: 50%;
            border: 1px solid #cbd5e1;
            z-index: 10;
        }}
        .notch-bottom {{
            position: absolute;
            bottom: -11px;
            right: 209px;
            width: 22px;
            height: 22px;
            background-color: #f1f5f9;
            border-radius: 50%;
            border: 1px solid #cbd5e1;
            z-index: 10;
        }}
    </style>
</head>
<body>
    <div class=""ticket-container"">
        <div class=""notch-top""></div>
        <div class=""notch-bottom""></div>
        <table class=""ticket-card"" cellpadding=""0"" cellspacing=""0"" border=""0"">
            <tr>
                <td class=""left-section"">
                    <div class=""header-banner"">
                        <div class=""brand"">Eventify Pro Ticket</div>
                        <h1 class=""event-title"">{eventTitle}</h1>
                        <span class=""category-badge"">{categoryName}</span>
                    </div>
                    
                    <div class=""details-section"">
                        <!-- Row 1 -->
                        <div class=""details-row"">
                            <div class=""col-58"">
                                <div class=""label"">Attendee Name</div>
                                <div class=""value"">{attendeeName}</div>
                            </div>
                            <div class=""col-42"">
                                <div class=""label"">Date & Time</div>
                                <div class=""value"">{eventDate}</div>
                            </div>
                            <div class=""clear""></div>
                        </div>
                        
                        <!-- Row 2 -->
                        <div class=""details-row"">
                            <div class=""col-100"">
                                <div class=""label"">Venue / Location</div>
                                <div class=""value"">{location}, {city}</div>
                            </div>
                            <div class=""clear""></div>
                        </div>
                        
                        <!-- Row 3 -->
                        <div class=""details-row"">
                            <div class=""col-33"">
                                <div class=""label"">Ticket Type</div>
                                <div class=""value"">{ticketTypeName}</div>
                            </div>
                            <div class=""col-33"">
                                <div class=""label"">Price</div>
                                <div class=""value"">{price} EGP</div>
                            </div>
                            <div class=""col-34"">
                                <div class=""label"">Booking Ref</div>
                                <div class=""value"">#{bookingRef}</div>
                            </div>
                            <div class=""clear""></div>
                        </div>
                    </div>
                </td>
                
                <td class=""right-section"">
                    <div class=""scan-text"">Entry Pass</div>
                    <div class=""qr-wrapper"">
                        <img class=""qr-image"" src=""{qrCodeImgSrc}"" alt=""QR Code"" />
                    </div>
                    <div class=""scan-desc"">Present this QR code for gate check-in verification</div>
                    <div class=""ticket-id"">Ticket ID: #{ticketId}</div>
                </td>
            </tr>
        </table>
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

        var pdfBytes = await Task.Run(() => _pdfConverter.Convert(doc), cancellationToken);
        return pdfBytes;
    }

    public async Task<byte[]> GenerateBookingPdfAsync(int bookingId, CancellationToken cancellationToken = default)
    {
        var tickets = await _unitOfWork.Tickets.FindAsync(t => t.BookingId == bookingId, cancellationToken);
        var firstTicket = tickets.FirstOrDefault();
        if (firstTicket == null)
        {
            throw new ArgumentException($"No tickets found for booking ID {bookingId} to generate PDF.");
        }

        return await GenerateTicketPdfAsync(firstTicket.Id, cancellationToken);
    }
}
