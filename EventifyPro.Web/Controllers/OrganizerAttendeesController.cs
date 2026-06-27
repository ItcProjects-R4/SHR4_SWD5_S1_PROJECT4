namespace EventifyPro.Web.Controllers
{
    [Authorize(Roles = RoleNames.Organizer)]
    [TypeFilter(typeof(VerifiedOrganizerFilter))]
    public class OrganizerAttendeesController : Controller
    {
        private readonly IEventService _eventService;
        private readonly IConverter _pdfConverter;

        public OrganizerAttendeesController(IEventService eventService, IConverter pdfConverter)
        {
            _eventService = eventService;
            _pdfConverter = pdfConverter;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int eventId, string? searchTerm, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Forbid();
            }

            var eventResult = await _eventService.GetDetailAsync(eventId, cancellationToken);
            if (eventResult.IsFailure || eventResult.Data == null)
            {
                return NotFound("Event not found.");
            }

            var eventEntity = eventResult.Data;
            if (eventEntity.OrganizerId != organizerId)
            {
                return Forbid();
            }

            var attendeesResult = await _eventService.GetEventAttendeesPageAsync(eventId, organizerId, searchTerm, pageNumber, pageSize, cancellationToken);
            if (attendeesResult.IsFailure || attendeesResult.Data == null)
            {
                TempData["ErrorMessage"] = attendeesResult.Error ?? "Failed to load attendees.";
                return View(new List<OrganizerAttendeeViewModel>());
            }

            var pagedData = attendeesResult.Data;

            var viewModels = pagedData.Data.Select(b => new OrganizerAttendeeViewModel
            {
                BookingId = b.BookingId,
                AttendeeName = b.AttendeeName,
                AttendeeEmail = b.AttendeeEmail,
                ProfileImageUrl = b.ProfileImageUrl,
                TotalAmount = b.TotalAmount,
                BookingDate = b.BookingDate,
                Tickets = b.Tickets.Select(t => new AttendeeTicketViewModel
                {
                    TicketId = t.TicketId,
                    TicketTypeName = t.TicketTypeName,
                    IsUsed = t.IsUsed,
                    UsedAt = t.UsedAt
                }).ToList()
            }).ToList();

            ViewBag.EventId = eventId;
            ViewBag.EventTitle = eventEntity.Title;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = pagedData.TotalPages;
            ViewBag.TotalCount = pagedData.TotalCount;
            ViewBag.SearchTerm = searchTerm;

            return View(viewModels);
        }

        [HttpGet]
        public async Task<IActionResult> ExportAttendees(int eventId, string format, CancellationToken cancellationToken)
        {
            try
            {
                var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(organizerId))
                {
                    return Forbid();
                }

                var eventResult = await _eventService.GetDetailAsync(eventId, cancellationToken);
                if (eventResult.IsFailure || eventResult.Data == null)
                {
                    return NotFound("Event not found.");
                }

                var eventEntity = eventResult.Data;
                if (eventEntity.OrganizerId != organizerId)
                {
                    return Forbid();
                }

                var exportResult = await _eventService.GetEventAttendeesForExportAsync(eventId, organizerId, cancellationToken);
                if (exportResult.IsFailure || exportResult.Data == null)
                {
                    TempData["ErrorMessage"] = exportResult.Error ?? "Failed to export attendees.";
                    return RedirectToAction(nameof(Index), new { eventId });
                }

                var attendees = exportResult.Data;

                var list = attendees.SelectMany(b => b.Tickets.Select(t => new
                {
                    BookingId = b.BookingId,
                    AttendeeName = b.AttendeeName,
                    AttendeeEmail = b.AttendeeEmail,
                    TicketType = t.TicketTypeName,
                    Price = t.Price,
                    BookingDate = b.BookingDate.ToEgyptTime(),
                    CheckedIn = t.IsUsed ? "Yes" : "No",
                    CheckedInTime = t.UsedAt?.ToEgyptTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"
                })).ToList();

                Func<string?, string> escapeCsvField = val => 
                {
                    if (string.IsNullOrEmpty(val)) return "\"\"";
                    var escaped = val.Replace("\"", "\"\"");
                    if (escaped.StartsWith("=") || escaped.StartsWith("+") || escaped.StartsWith("-") || escaped.StartsWith("@"))
                    {
                        escaped = "'" + escaped;
                    }
                    return $"\"{escaped}\"";
                };

                if (format.ToLower() == "csv")
                {
                    var csv = new System.Text.StringBuilder();
                    csv.AppendLine("Booking ID,Attendee Name,Attendee Email,Ticket Type,Price (EGP),Booking Date,Checked In,Checked In Time");
                    foreach (var item in list)
                    {
                        csv.AppendLine($"\"#{item.BookingId.ToString("D6")}\",{escapeCsvField(item.AttendeeName)},{escapeCsvField(item.AttendeeEmail)},{escapeCsvField(item.TicketType)},{item.Price:0.00},{item.BookingDate:yyyy-MM-dd HH:mm:ss},{escapeCsvField(item.CheckedIn)},{escapeCsvField(item.CheckedInTime)}");
                    }
                    return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"Attendees_{eventEntity.Title.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.csv");
                }
                else if (format.ToLower() == "excel")
                {
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Guest List");
                        worksheet.Cell("A1").Value = $"Guest List for: {eventEntity.Title}";
                        worksheet.Cell("A1").Style.Font.Bold = true;
                        worksheet.Cell("A1").Style.Font.FontSize = 14;
                        worksheet.Range("A1:H1").Merge();

                        worksheet.Cell("A2").Value = $"Generated At: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
                        worksheet.Cell("A2").Style.Font.Italic = true;

                        // Headers
                        var headers = new[] { "Booking ID", "Attendee Name", "Attendee Email", "Ticket Type", "Price (EGP)", "Booking Date", "Checked In", "Checked In Time" };
                        for (int i = 0; i < headers.Length; i++)
                        {
                            var cell = worksheet.Cell(4, i + 1);
                            cell.Value = headers[i];
                            cell.Style.Font.Bold = true;
                            cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#ede9fe");
                        }

                        int row = 5;
                        foreach (var item in list)
                        {
                            worksheet.Cell(row, 1).Value = $"#{item.BookingId.ToString("D6")}";
                            worksheet.Cell(row, 2).Value = item.AttendeeName;
                            worksheet.Cell(row, 3).Value = item.AttendeeEmail;
                            worksheet.Cell(row, 4).Value = item.TicketType;
                            worksheet.Cell(row, 5).Value = item.Price;
                            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                            worksheet.Cell(row, 6).Value = item.BookingDate.ToString("yyyy-MM-dd HH:mm:ss");
                            worksheet.Cell(row, 7).Value = item.CheckedIn;
                            worksheet.Cell(row, 8).Value = item.CheckedInTime;
                            row++;
                        }

                        worksheet.Columns().AdjustToContents();

                        using (var stream = new System.IO.MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            var content = stream.ToArray();
                            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Attendees_{eventEntity.Title.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.xlsx");
                        }
                    }
                }
                else if (format.ToLower() == "pdf")
                {
                    var html = new System.Text.StringBuilder();
                    html.Append($@"
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; }}
        h2 {{ color: #5b21b6; border-bottom: 2px solid #5b21b6; padding-bottom: 8px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
        th {{ background-color: #ede9fe; color: #5b21b6; text-align: left; padding: 10px; font-size: 12px; text-transform: uppercase; border: 1px solid #ddd; }}
        td {{ padding: 10px; border: 1px solid #ddd; font-size: 12px; }}
        tr:nth-child(even) {{ background-color: #fcfbfe; }}
        .meta {{ font-size: 11px; color: #666; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <h2>Guest List: {eventEntity.Title}</h2>
    <div class='meta'>Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</div>
    <table>
        <thead>
            <tr>
                <th>Booking ID</th>
                <th>Attendee</th>
                <th>Email</th>
                <th>Ticket Type</th>
                <th>Price</th>
                <th>Booking Date</th>
                <th>Scanned?</th>
                <th>Scanned At</th>
            </tr>
        </thead>
        <tbody>");

                    foreach (var item in list)
                    {
                        html.Append($@"
            <tr>
                <td>#{item.BookingId.ToString("D6")}</td>
                <td>{System.Net.WebUtility.HtmlEncode(item.AttendeeName)}</td>
                <td>{System.Net.WebUtility.HtmlEncode(item.AttendeeEmail)}</td>
                <td>{System.Net.WebUtility.HtmlEncode(item.TicketType)}</td>
                <td>{item.Price:0.00} EGP</td>
                <td>{item.BookingDate:yyyy-MM-dd HH:mm}</td>
                <td>{item.CheckedIn}</td>
                <td>{item.CheckedInTime}</td>
            </tr>");
                    }

                    html.Append(@"
        </tbody>
    </table>
</body>
</html>");

                    var doc = new HtmlToPdfDocument
                    {
                        GlobalSettings =
                        {
                            ColorMode = ColorMode.Color,
                            Orientation = Orientation.Portrait,
                            PaperSize = PaperKind.A4,
                            Margins = new MarginSettings { Top = 15, Bottom = 15, Left = 15, Right = 15 }
                        },
                        Objects =
                        {
                            new ObjectSettings
                            {
                                PagesCount = true,
                                HtmlContent = html.ToString(),
                                WebSettings = { DefaultEncoding = "utf-8" }
                            }
                        }
                    };

                    var pdfBytes = await Task.Run(() => _pdfConverter.Convert(doc));
                    return File(pdfBytes, "application/pdf", $"Attendees_{eventEntity.Title.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.pdf");
                }

                return BadRequest("Invalid export format.");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred during report export.";
                return RedirectToAction(nameof(Index), new { eventId });
            }
        }
    }
}
