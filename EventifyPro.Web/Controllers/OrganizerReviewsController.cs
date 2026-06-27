
namespace EventifyPro.Web.Controllers;

[Authorize(Roles = RoleNames.Organizer)]
[TypeFilter(typeof(VerifiedOrganizerFilter))]
public class OrganizerReviewsController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly IConverter _pdfConverter;

    public OrganizerReviewsController(IReviewService reviewService, IConverter pdfConverter)
    {
        _reviewService = reviewService;
        _pdfConverter = pdfConverter;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm, 
        int? ratingFilter, 
        DateTime? startDate, 
        DateTime? endDate, 
        int page = 1, 
        int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var result = await _reviewService.GetOrganizerReviewsAsync(
            organizerId,
            searchTerm,
            ratingFilter,
            startDate.UserInputToUtc(),
            endDate.UserInputToUtc(),
            page,
            pageSize,
            cancellationToken);

        if (!result.IsSuccess || result.Data == null)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to retrieve reviews.";
            return RedirectToAction("Index", "Dashboard");
        }

        var data = result.Data;

        var viewModel = new OrganizerReviewsListViewModel
        {
            SearchTerm = data.SearchTerm,
            RatingFilter = data.RatingFilter,
            StartDate = data.StartDate,
            EndDate = data.EndDate,
            TotalReviews = data.TotalReviews,
            AverageRating = Math.Round(data.AverageRating, 1),
            RatingDistribution = data.RatingDistribution,
            Reviews = data.Reviews.Select(r => new OrganizerReviewItemViewModel
            {
                Id = r.Id,
                EventId = r.EventId,
                EventTitle = r.EventTitle,
                AttendeeName = r.AttendeeName,
                AttendeeEmail = r.AttendeeEmail,
                AttendeeInitials = r.AttendeeInitials,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                OrganizerReply = r.OrganizerReply,
                RepliedAt = r.RepliedAt,
                IsFlagged = r.IsFlagged,
                FlaggedReason = r.FlaggedReason
            }).ToList(),
            Page = data.Page,
            PageSize = data.PageSize,
            TotalPages = data.TotalPages
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int reviewId, string replyContent, CancellationToken cancellationToken)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(replyContent))
        {
            TempData["ErrorMessage"] = "Reply content cannot be empty.";
            return RedirectToAction(nameof(Index));
        }

        if (replyContent.Length > 1000)
        {
            TempData["ErrorMessage"] = "Reply content cannot exceed 1000 characters.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _reviewService.ReplyToReviewAsync(reviewId, organizerId, replyContent, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to post reply.";
        }
        else
        {
            TempData["SuccessMessage"] = "Your reply has been posted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Flag(int reviewId, string flaggedReason, CancellationToken cancellationToken)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(flaggedReason))
        {
            TempData["ErrorMessage"] = "Flag reason must be specified.";
            return RedirectToAction(nameof(Index));
        }

        if (flaggedReason.Length > 500)
        {
            TempData["ErrorMessage"] = "Flag reason cannot exceed 500 characters.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _reviewService.FlagReviewAsync(reviewId, organizerId, flaggedReason, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to flag review.";
        }
        else
        {
            TempData["SuccessMessage"] = "Review has been flagged and submitted for admin review.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(organizerId))
        {
            return Forbid();
        }

        var result = await _reviewService.DeleteAsync(id, organizerId, cancellationToken);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error ?? "Failed to delete review.";
        }
        else
        {
            TempData["SuccessMessage"] = "Review deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportReviews(string format, CancellationToken cancellationToken)
    {
        try
        {
            var organizerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Forbid();
            }

            var result = await _reviewService.GetOrganizerReviewsForExportAsync(organizerId, cancellationToken);
            if (!result.IsSuccess || result.Data == null)
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to retrieve reviews for export.";
                return RedirectToAction(nameof(Index));
            }

            var list = result.Data;

            Func<string?, string> escapeCsvField = val => 
            {
                if (string.IsNullOrEmpty(val)) return "\"\"";
                // If it starts with formula characters, prepend a single quote to prevent CSV injection
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
                csv.AppendLine("Review ID,Event Title,Attendee Name,Attendee Email,Rating,Comment,Created At,Organizer Reply,Replied At");
                foreach (var item in list)
                {
                    csv.AppendLine($"{item.Id},{escapeCsvField(item.EventTitle)},{escapeCsvField(item.AttendeeName)},{escapeCsvField(item.AttendeeEmail)},{item.Rating},{escapeCsvField(item.Comment)},{item.CreatedAt.ToEgyptTime():yyyy-MM-dd HH:mm:ss},{escapeCsvField(item.OrganizerReply)},{item.RepliedAt?.ToEgyptTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}");
                }
                return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"Reviews_{DateTime.UtcNow:yyyyMMdd}.csv");
            }
            else if (format.ToLower() == "excel")
            {
                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Attendee Reviews");
                    worksheet.Cell("A1").Value = "Attendee Reviews List";
                    worksheet.Cell("A1").Style.Font.Bold = true;
                    worksheet.Cell("A1").Style.Font.FontSize = 14;
                    worksheet.Range("A1:I1").Merge();

                    worksheet.Cell("A2").Value = $"Generated At: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
                    worksheet.Cell("A2").Style.Font.Italic = true;

                    var headers = new[] { "Review ID", "Event Title", "Attendee Name", "Attendee Email", "Rating", "Comment", "Created At", "Organizer Reply", "Replied At" };
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
                        worksheet.Cell(row, 1).Value = item.Id;
                        worksheet.Cell(row, 2).Value = item.EventTitle;
                        worksheet.Cell(row, 3).Value = item.AttendeeName;
                        worksheet.Cell(row, 4).Value = item.AttendeeEmail;
                        worksheet.Cell(row, 5).Value = item.Rating;
                        worksheet.Cell(row, 6).Value = item.Comment;
                        worksheet.Cell(row, 7).Value = item.CreatedAt.ToEgyptTime().ToString("yyyy-MM-dd HH:mm:ss");
                        worksheet.Cell(row, 8).Value = item.OrganizerReply;
                        worksheet.Cell(row, 9).Value = item.RepliedAt?.ToEgyptTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();

                    using (var stream = new System.IO.MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reviews_{DateTime.UtcNow:yyyyMMdd}.xlsx");
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
        th {{ background-color: #ede9fe; color: #5b21b6; text-align: left; padding: 10px; font-size: 11px; text-transform: uppercase; border: 1px solid #ddd; }}
        td {{ padding: 10px; border: 1px solid #ddd; font-size: 11px; }}
        tr:nth-child(even) {{ background-color: #fcfbfe; }}
        .meta {{ font-size: 11px; color: #666; margin-bottom: 20px; }}
        .stars {{ color: #f59e0b; font-weight: bold; }}
    </style>
</head>
<body>
    <h2>Attendee Reviews List</h2>
    <div class='meta'>Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</div>
    <table>
        <thead>
            <tr>
                <th>ID</th>
                <th>Event</th>
                <th>Attendee</th>
                <th>Rating</th>
                <th>Comment</th>
                <th>Created At</th>
                <th>Reply</th>
            </tr>
        </thead>
        <tbody>");

                foreach (var item in list)
                {
                    var starString = new string('★', item.Rating) + new string('☆', 5 - item.Rating);
                    html.Append($@"
            <tr>
                <td>{item.Id}</td>
                <td>{System.Net.WebUtility.HtmlEncode(item.EventTitle)}</td>
                <td>{System.Net.WebUtility.HtmlEncode(item.AttendeeName)}</td>
                <td><span class='stars'>{starString} ({item.Rating}/5)</span></td>
                <td>{System.Net.WebUtility.HtmlEncode(item.Comment)}</td>
                <td>{item.CreatedAt:yyyy-MM-dd HH:mm}</td>
                <td>{System.Net.WebUtility.HtmlEncode(item.OrganizerReply)}</td>
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
                        Orientation = Orientation.Landscape,
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
                return File(pdfBytes, "application/pdf", $"Reviews_{DateTime.UtcNow:yyyyMMdd}.pdf");
            }

            return BadRequest("Invalid export format.");
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "An error occurred during report export.";
            return RedirectToAction(nameof(Index));
        }
    }
}
