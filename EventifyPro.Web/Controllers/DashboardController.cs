
namespace EventifyPro.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConverter _pdfConverter;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger,
        UserManager<ApplicationUser> userManager,
        IConverter pdfConverter)
    {
        _dashboardService = dashboardService;
        _logger = logger;
        _userManager = userManager;
        _pdfConverter = pdfConverter;
    }

    /// <summary>
    /// GET: Displays the organizer dashboard with event and booking statistics.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.Organizer)]
    [TypeFilter(typeof(VerifiedOrganizerFilter))]
    public async Task<IActionResult> Organizer(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        ViewBag.OrganizerId = userId;

        try
        {
            var result = await _dashboardService.GetOrganizerDashboardAsync(userId, cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogWarning("Dashboard failure: {Error}", result.Error);
                TempData["DashboardError"] = result.Error ?? "Failed to load dashboard data.";
                return View("OrganizerDashboard", new EventifyPro.BLL.DTOs.Dashboard.OrganizerDashboardDto());
            }

            return View("OrganizerDashboard", result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organizer dashboard data for user {UserId}", userId);
            TempData["DashboardError"] = "An unexpected error occurred while loading the dashboard.";
            return View("OrganizerDashboard", new EventifyPro.BLL.DTOs.Dashboard.OrganizerDashboardDto());
        }
    }

    /// <summary>
    /// GET: Displays the admin system dashboard.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    [TypeFilter(typeof(AdminIpWhitelistFilter))]
    public async Task<IActionResult> Admin(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dashboardService.GetAdminDashboardAsync(startDate.UserInputToUtc(), endDate.UserInputToUtc(), cancellationToken);
            if (result.IsFailure)
            {
                TempData["DashboardError"] = result.Error ?? "Failed to load dashboard data.";
                return View("AdminDashboard", new EventifyPro.BLL.DTOs.Dashboard.AdminDashboardDto());
            }

            // Fetch current user details to avoid DB queries in View
            var user = await _userManager.GetUserAsync(User);
            ViewBag.AdminName = user?.FullName ?? "Admin User";
            ViewBag.AdminAvatar = user?.ProfileImageUrl;

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View("AdminDashboard", result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving admin dashboard data");
            TempData["DashboardError"] = "An unexpected error occurred while loading the dashboard.";
            return View("AdminDashboard", new EventifyPro.BLL.DTOs.Dashboard.AdminDashboardDto());
        }
    }

    /// <summary>
    /// GET: Exports admin report to CSV.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    [TypeFilter(typeof(AdminIpWhitelistFilter))]
    public async Task<IActionResult> ExportAdminReport(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dashboardService.GetAdminDashboardAsync(startDate.UserInputToUtc(), endDate.UserInputToUtc(), cancellationToken);
            if (result.IsFailure || result.Data == null)
            {
                TempData["DashboardError"] = "Failed to load dashboard data for export.";
                return RedirectToAction(nameof(Admin), new { startDate, endDate });
            }

            var data = result.Data;
            var csv = new System.Text.StringBuilder();

            csv.AppendLine("Eventify Pro - Admin System Report");
            csv.AppendLine($"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            csv.AppendLine($"Date Range: {startDate?.ToString("yyyy-MM-dd") ?? "Beginning"} to {endDate?.ToString("yyyy-MM-dd") ?? "Today"}");
            csv.AppendLine();

            csv.AppendLine("Overview Metrics");
            csv.AppendLine($"Total Active Users,{data.TotalUsers}");
            csv.AppendLine($"Total Active Events,{data.TotalEvents}");
            csv.AppendLine($"Total Bookings,{data.TotalBookings}");
            csv.AppendLine($"Total Tickets Generated,{data.TotalTickets}");
            csv.AppendLine($"Total Revenue (EGP),{data.TotalRevenue}");
            csv.AppendLine($"Average Event Review Rating,{data.AverageReviewRating:F2}");
            csv.AppendLine();

            csv.AppendLine("Users Distribution by Role");
            csv.AppendLine("Role,Active Users Count");
            foreach (var role in data.UsersByRole)
            {
                csv.AppendLine($"{role.Key},{role.Value}");
            }
            csv.AppendLine();

            csv.AppendLine("Revenue Growth (Last 12 Months)");
            csv.AppendLine("Month,Revenue (EGP)");
            foreach (var month in data.RevenueByMonth)
            {
                csv.AppendLine($"{month.Key},{month.Value}");
            }

            var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv", $"EventifyPro_AdminReport_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting admin report to CSV");
            TempData["DashboardError"] = "An error occurred during report export.";
            return RedirectToAction(nameof(Admin), new { startDate, endDate });
        }
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    [TypeFilter(typeof(AdminIpWhitelistFilter))]
    public async Task<IActionResult> ExportAdminReportExcel(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dashboardService.GetAdminDashboardAsync(startDate.UserInputToUtc(), endDate.UserInputToUtc(), cancellationToken);
            if (result.IsFailure || result.Data == null)
            {
                TempData["DashboardError"] = "Failed to load dashboard data for Excel export.";
                return RedirectToAction(nameof(Admin), new { startDate, endDate });
            }

            var data = result.Data;

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Admin Report");

                // Title
                worksheet.Cell("A1").Value = "Eventify Pro - Admin System Report";
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A1").Style.Font.FontSize = 16;
                worksheet.Range("A1:C1").Merge();

                // Subtitle
                worksheet.Cell("A2").Value = $"Generated At: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC | Date Range: {startDate?.ToString("yyyy-MM-dd") ?? "Beginning"} to {endDate?.ToString("yyyy-MM-dd") ?? "Today"}";
                worksheet.Cell("A2").Style.Font.Italic = true;
                worksheet.Cell("A2").Style.Font.FontSize = 10;
                worksheet.Cell("A2").Style.Font.FontColor = ClosedXML.Excel.XLColor.Gray;
                worksheet.Range("A2:C2").Merge();

                // Section 1: Overview Metrics
                worksheet.Cell("A4").Value = "Overview Metric";
                worksheet.Cell("B4").Value = "Value";
                worksheet.Range("A4:B4").Style.Font.Bold = true;
                worksheet.Range("A4:B4").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#ede9fe");

                worksheet.Cell("A5").Value = "Total Active Users";
                worksheet.Cell("B5").Value = data.TotalUsers;

                worksheet.Cell("A6").Value = "Total Active Events";
                worksheet.Cell("B6").Value = data.TotalEvents;

                worksheet.Cell("A7").Value = "Total Bookings";
                worksheet.Cell("B7").Value = data.TotalBookings;

                worksheet.Cell("A8").Value = "Total Tickets Generated";
                worksheet.Cell("B8").Value = data.TotalTickets;

                worksheet.Cell("A9").Value = "Total Revenue (EGP)";
                worksheet.Cell("B9").Value = data.TotalRevenue;
                worksheet.Cell("B9").Style.NumberFormat.Format = "#,##0.00";

                worksheet.Cell("A10").Value = "Average Event Rating";
                worksheet.Cell("B10").Value = data.AverageReviewRating;
                worksheet.Cell("B10").Style.NumberFormat.Format = "0.00";

                // Section 2: User Distribution
                worksheet.Cell("D4").Value = "User Role";
                worksheet.Cell("E4").Value = "Active Count";
                worksheet.Range("D4:E4").Style.Font.Bold = true;
                worksheet.Range("D4:E4").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#ede9fe");

                int row = 5;
                foreach (var role in data.UsersByRole)
                {
                    worksheet.Cell(row, 4).Value = role.Key;
                    worksheet.Cell(row, 5).Value = role.Value;
                    row++;
                }

                // Section 3: Revenue Growth
                worksheet.Cell("A13").Value = "Revenue Growth (Last 12 Months)";
                worksheet.Cell("A13").Style.Font.Bold = true;
                worksheet.Cell("A13").Style.Font.FontSize = 12;
                worksheet.Range("A13:B13").Merge();

                worksheet.Cell("A14").Value = "Month";
                worksheet.Cell("B14").Value = "Revenue (EGP)";
                worksheet.Range("A14:B14").Style.Font.Bold = true;
                worksheet.Range("A14:B14").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#ede9fe");

                row = 15;
                foreach (var month in data.RevenueByMonth)
                {
                    worksheet.Cell(row, 1).Value = month.Key;
                    worksheet.Cell(row, 2).Value = month.Value;
                    worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var bytes = stream.ToArray();
                    return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"EventifyPro_AdminReport_{DateTime.UtcNow:yyyyMMdd}.xlsx");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting admin report to Excel");
            TempData["DashboardError"] = "An error occurred during Excel report export.";
            return RedirectToAction(nameof(Admin), new { startDate, endDate });
        }
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin)]
    [TypeFilter(typeof(AdminIpWhitelistFilter))]
    public async Task<IActionResult> ExportAdminReportPdf(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dashboardService.GetAdminDashboardAsync(startDate.UserInputToUtc(), endDate.UserInputToUtc(), cancellationToken);
            if (result.IsFailure || result.Data == null)
            {
                TempData["DashboardError"] = "Failed to load dashboard data for PDF export.";
                return RedirectToAction(nameof(Admin), new { startDate, endDate });
            }

            var data = result.Data;

            var startDateStr = startDate?.ToString("yyyy-MM-dd") ?? "Beginning";
            var endDateStr = endDate?.ToString("yyyy-MM-dd") ?? "Today";
            var usersByRoleHtml = string.Join("", data.UsersByRole.Select(role => $"<tr><td>{role.Key}</td><td class=\"metric-value\">{role.Value:N0}</td></tr>"));
            var revenueByMonthHtml = string.Join("", data.RevenueByMonth.Select(m => $"<tr><td>{m.Key}</td><td class=\"metric-value\">{m.Value:N2} EGP</td></tr>"));

            // Build beautiful PDF Template using inline CSS
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{
            font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif;
            margin: 0;
            padding: 30px;
            color: #1f1b2e;
            background-color: #ffffff;
        }}
        .header {{
            border-bottom: 2px solid #5b21b6;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .header h1 {{
            margin: 0;
            color: #5b21b6;
            font-size: 26px;
            font-weight: 800;
        }}
        .header p {{
            margin: 5px 0 0 0;
            color: #6b7280;
            font-size: 13px;
        }}
        .section-title {{
            font-size: 18px;
            font-weight: bold;
            color: #4c1d95;
            margin-top: 30px;
            margin-bottom: 15px;
            border-bottom: 1px solid #ede9fe;
            padding-bottom: 6px;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
        }}
        th, td {{
            padding: 12px 15px;
            text-align: left;
            font-size: 13.5px;
        }}
        th {{
            background-color: #f5f3ff;
            color: #5b21b6;
            font-weight: bold;
            border-bottom: 2px solid #ede9fe;
        }}
        td {{
            border-bottom: 1px solid #ede9fe;
        }}
        tr:hover td {{
            background-color: #fafaff;
        }}
        .metric-value {{
            font-weight: bold;
            color: #1f1b2e;
        }}
        .footer {{
            margin-top: 50px;
            border-top: 1px solid #e5e7eb;
            padding-top: 15px;
            font-size: 11px;
            color: #9ca3af;
            text-align: center;
        }}
        .row {{
            width: 100%;
        }}
        .col-6 {{
            width: 48%;
            float: left;
            margin-right: 2%;
        }}
        .clearfix::after {{
            content: """";
            clear: both;
            display: table;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>Eventify Pro - Admin System Report</h1>
        <p>Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC | Date Range: {startDateStr} to {endDateStr}</p>
    </div>

    <div class=""row clearfix"">
        <div class=""col-6"">
            <div class=""section-title"">Overview Metrics</div>
            <table>
                <thead>
                    <tr>
                        <th>Metric</th>
                        <th>Value</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>Total Active Users</td>
                        <td class=""metric-value"">{data.TotalUsers:N0}</td>
                    </tr>
                    <tr>
                        <td>Total Active Events</td>
                        <td class=""metric-value"">{data.TotalEvents:N0}</td>
                    </tr>
                    <tr>
                        <td>Total Bookings</td>
                        <td class=""metric-value"">{data.TotalBookings:N0}</td>
                    </tr>
                    <tr>
                        <td>Total Tickets Generated</td>
                        <td class=""metric-value"">{data.TotalTickets:N0}</td>
                    </tr>
                    <tr>
                        <td>Total Revenue (EGP)</td>
                        <td class=""metric-value"">{data.TotalRevenue:N2} EGP</td>
                    </tr>
                    <tr>
                        <td>Average Event Review Rating</td>
                        <td class=""metric-value"">{data.AverageReviewRating:F2} / 5.0</td>
                    </tr>
                </tbody>
            </table>
        </div>

        <div class=""col-6"">
            <div class=""section-title"">Users Distribution</div>
            <table>
                <thead>
                    <tr>
                        <th>Role</th>
                        <th>Active Users</th>
                    </tr>
                </thead>
                <tbody>
                    {usersByRoleHtml}
                </tbody>
            </table>
        </div>
    </div>

    <div class=""section-title"" style=""margin-top: 10px;"">Revenue Growth (Last 12 Months)</div>
    <table>
        <thead>
            <tr>
                <th>Month</th>
                <th>Revenue (EGP)</th>
            </tr>
        </thead>
        <tbody>
            {revenueByMonthHtml}
        </tbody>
    </table>

    <div class=""footer"">
        Eventify Pro Dashboard Management Systems &copy; {DateTime.UtcNow.Year}. All rights reserved.
    </div>
</body>
</html>
";

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
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            var pdfBytes = await Task.Run(() => _pdfConverter.Convert(doc));
            return File(pdfBytes, "application/pdf", $"EventifyPro_AdminReport_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting admin report to PDF");
            TempData["DashboardError"] = "An error occurred during PDF report export.";
            return RedirectToAction(nameof(Admin), new { startDate, endDate });
        }
    }

    /// <summary>
    /// GET: Displays the main user dashboard.
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        if (roles.Contains(RoleNames.Admin))
        {
            return RedirectToAction(nameof(Admin));
        }

        if (roles.Contains(RoleNames.Organizer))
        {
            return RedirectToAction(nameof(Organizer));
        }

        return View();
    }
}
