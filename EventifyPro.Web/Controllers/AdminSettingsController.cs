

namespace EventifyPro.Web.Controllers
{
    [Route("Admin")]
    public class AdminSettingsController : AdminBaseController
    {
        private readonly IAdminService _adminService;
        private readonly ISystemSettingService _systemSettingService;
        private readonly SecuritySettingsCache _securitySettingsCache;
        private readonly IOptions<IdentityOptions> _identityOptions;
        private readonly ILogger<AdminSettingsController> _logger;

        public AdminSettingsController(
            UserManager<ApplicationUser> userManager,
            IAdminService adminService,
            ISystemSettingService systemSettingService,
            SecuritySettingsCache securitySettingsCache,
            IOptions<IdentityOptions> identityOptions,
            ILogger<AdminSettingsController> logger) : base(userManager)
        {
            _adminService = adminService;
            _systemSettingService = systemSettingService;
            _securitySettingsCache = securitySettingsCache;
            _identityOptions = identityOptions;
            _logger = logger;
        }

        [HttpGet("Settings")]
        public async Task<IActionResult> Settings(CancellationToken cancellationToken = default)
        {
            try
            {
                await PopulateAdminViewDataAsync();

                var model = new AdminSettingsViewModel
                {
                    PlatformName = await _systemSettingService.GetSettingValueAsync("PlatformName", "Eventify Pro", cancellationToken),
                    SupportEmail = await _systemSettingService.GetSettingValueAsync("SupportEmail", "support@eventifypro.com", cancellationToken),
                    SupportWhatsApp = await _systemSettingService.GetSettingValueAsync("SupportWhatsApp", "01064665247", cancellationToken),
                    TicketCommissionRate = await _systemSettingService.GetSettingValueAsync<decimal>("TicketCommissionRate", 5.0m, cancellationToken),
                    EnableMaintenanceMode = await _systemSettingService.GetSettingValueAsync<bool>("EnableMaintenanceMode", false, cancellationToken),
                    MaxTicketsPerBooking = await _systemSettingService.GetSettingValueAsync<int>("MaxTicketsPerBooking", 10, cancellationToken),
                    AllowedUploadExtensions = await _systemSettingService.GetSettingValueAsync("AllowedUploadExtensions", ".jpg,.jpeg,.png,.pdf", cancellationToken),
                    GeminiApiKey = await _systemSettingService.GetSettingValueAsync("GeminiApiKey", "", cancellationToken)
                };

                return View("~/Views/Admin/Settings.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading system settings");
                TempData["AdminError"] = "Error loading system settings.";
                return View("~/Views/Admin/Settings.cshtml", new AdminSettingsViewModel());
            }
        }

        [HttpPost("Settings")]
        [ValidateAntiForgeryToken]
        [RateLimit(5, 10)]
        [AuditLog]
        public async Task<IActionResult> Settings(AdminSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateAdminViewDataAsync();
                TempData["AdminError"] = "Invalid settings data.";
                return View("~/Views/Admin/Settings.cshtml", model);
            }

            try
            {
                var settings = new Dictionary<string, string>
                {
                    { "PlatformName", model.PlatformName ?? string.Empty },
                    { "SupportEmail", model.SupportEmail ?? string.Empty },
                    { "SupportWhatsApp", model.SupportWhatsApp ?? string.Empty },
                    { "TicketCommissionRate", model.TicketCommissionRate.ToString() },
                    { "EnableMaintenanceMode", model.EnableMaintenanceMode.ToString().ToLower() },
                    { "MaxTicketsPerBooking", model.MaxTicketsPerBooking.ToString() },
                    { "AllowedUploadExtensions", model.AllowedUploadExtensions ?? string.Empty },
                    { "GeminiApiKey", model.GeminiApiKey ?? string.Empty }
                };

                await _systemSettingService.SaveSettingsAsync(settings);

                TempData["AdminSuccess"] = "System settings updated successfully.";
                return RedirectToAction(nameof(Settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
                TempData["AdminError"] = "Error saving system settings.";
                await PopulateAdminViewDataAsync();
                return View("~/Views/Admin/Settings.cshtml", model);
            }
        }

        [HttpGet("Security")]
        public async Task<IActionResult> Security(CancellationToken cancellationToken = default)
        {
            try
            {
                await PopulateAdminViewDataAsync();

                var model = new AdminSecurityViewModel
                {
                    RequireTwoFactorForAdmins = await _systemSettingService.GetSettingValueAsync<bool>("RequireTwoFactorForAdmins", true, cancellationToken),
                    PasswordMinLength = await _systemSettingService.GetSettingValueAsync<int>("PasswordMinLength", 8, cancellationToken),
                    RequirePasswordUppercase = await _systemSettingService.GetSettingValueAsync<bool>("RequirePasswordUppercase", true, cancellationToken),
                    RequirePasswordDigits = await _systemSettingService.GetSettingValueAsync<bool>("RequirePasswordDigits", true, cancellationToken),
                    SessionTimeoutMinutes = await _systemSettingService.GetSettingValueAsync<int>("SessionTimeoutMinutes", 30, cancellationToken),
                    MaxFailedLoginsBeforeLockout = await _systemSettingService.GetSettingValueAsync<int>("MaxFailedLoginsBeforeLockout", 5, cancellationToken),
                    AdminIpWhitelist = await _systemSettingService.GetSettingValueAsync("AdminIpWhitelist", "*", cancellationToken)
                };

                return View("~/Views/Admin/Security.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading security settings");
                TempData["AdminError"] = "Error loading security settings.";
                return View("~/Views/Admin/Security.cshtml", new AdminSecurityViewModel());
            }
        }

        [HttpPost("Security")]
        [ValidateAntiForgeryToken]
        [RateLimit(5, 10)]
        [AuditLog]
        public async Task<IActionResult> Security(AdminSecurityViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateAdminViewDataAsync();
                TempData["AdminError"] = "Invalid security data.";
                return View("~/Views/Admin/Security.cshtml", model);
            }

            try
            {
                // Prevent self-lockout by checking if current admin's IP is allowed by the new whitelist
                if (model.AdminIpWhitelist != "*")
                {
                    var remoteIp = HttpContext.Connection.RemoteIpAddress;
                    if (remoteIp != null)
                    {
                        var clientIp = remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4().ToString() : remoteIp.ToString();
                        var ipList = (model.AdminIpWhitelist ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ip => ip.Trim());
                        
                        bool isIpAllowed = ipList.Contains(clientIp) || 
                                           (System.Net.IPAddress.IsLoopback(remoteIp) && (ipList.Contains("127.0.0.1") || ipList.Contains("::1") || ipList.Contains("localhost")));
                        
                        if (!isIpAllowed)
                        {
                            ModelState.AddModelError("AdminIpWhitelist", $"Saving this whitelist would lock you out. Your current IP ({clientIp}) is not included in the list.");
                            await PopulateAdminViewDataAsync();
                            return View("~/Views/Admin/Security.cshtml", model);
                        }
                    }
                }

                var settings = new Dictionary<string, string>
                {
                    { "RequireTwoFactorForAdmins", model.RequireTwoFactorForAdmins.ToString().ToLower() },
                    { "PasswordMinLength", model.PasswordMinLength.ToString() },
                    { "RequirePasswordUppercase", model.RequirePasswordUppercase.ToString().ToLower() },
                    { "RequirePasswordDigits", model.RequirePasswordDigits.ToString().ToLower() },
                    { "SessionTimeoutMinutes", model.SessionTimeoutMinutes.ToString() },
                    { "MaxFailedLoginsBeforeLockout", model.MaxFailedLoginsBeforeLockout.ToString() },
                    { "AdminIpWhitelist", model.AdminIpWhitelist ?? "*" }
                };

                await _systemSettingService.SaveSettingsAsync(settings);

                _securitySettingsCache.PasswordMinLength = model.PasswordMinLength;
                _securitySettingsCache.RequirePasswordUppercase = model.RequirePasswordUppercase;
                _securitySettingsCache.RequirePasswordDigits = model.RequirePasswordDigits;
                _securitySettingsCache.MaxFailedLoginsBeforeLockout = model.MaxFailedLoginsBeforeLockout;
                _securitySettingsCache.SessionTimeoutMinutes = model.SessionTimeoutMinutes;

                var identityOptions = _identityOptions.Value;
                identityOptions.Lockout.MaxFailedAccessAttempts = model.MaxFailedLoginsBeforeLockout;
                identityOptions.Password.RequiredLength = model.PasswordMinLength;
                identityOptions.Password.RequireUppercase = model.RequirePasswordUppercase;
                identityOptions.Password.RequireDigit = model.RequirePasswordDigits;

                TempData["AdminSuccess"] = "Security policies updated successfully.";
                return RedirectToAction(nameof(Security));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving security settings");
                TempData["AdminError"] = "Error saving security settings.";
                await PopulateAdminViewDataAsync();
                return View("~/Views/Admin/Security.cshtml", model);
            }
        }

        [HttpGet("AuditLogs")]
        public async Task<IActionResult> AuditLogs(string tableNameFilter, string actionFilter, string userIdFilter, DateTime? startDate, DateTime? endDate, int? page, CancellationToken cancellationToken)
        {
            try
            {
                await PopulateAdminViewDataAsync();

                // Default date filter to last 30 days if no filters are specified
                if (!startDate.HasValue && !endDate.HasValue && string.IsNullOrEmpty(tableNameFilter) && string.IsNullOrEmpty(actionFilter) && string.IsNullOrEmpty(userIdFilter))
                {
                    startDate = DateTime.UtcNow.ToEgyptTime().Date.AddDays(-30);
                }

                var result = await _adminService.GetAuditLogsPageAsync(tableNameFilter, actionFilter, userIdFilter, startDate.UserInputToUtc(), endDate.UserInputToUtc(), page, cancellationToken);
                if (result.IsFailure || result.Data == null)
                {
                    TempData["AdminError"] = result.Error ?? "Error loading audit logs.";
                    return RedirectToAction("Index", "Admin");
                }

                var data = result.Data;
                ViewBag.TableNameFilter = tableNameFilter;
                ViewBag.ActionFilter = actionFilter;
                ViewBag.UserIdFilter = userIdFilter;
                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
                ViewBag.PageNumber = page ?? 1;
                ViewBag.TotalPages = data.PagedLogs.TotalPages;
                ViewBag.TotalCount = data.PagedLogs.TotalCount;
                ViewBag.UniqueTables = data.UniqueTables;
                ViewBag.UniqueActions = data.UniqueActions;
                ViewBag.UserMap = data.UserMap;

                return View("~/Views/Admin/AuditLogs.cshtml", data.PagedLogs.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading audit logs");
                TempData["AdminError"] = "Error loading audit logs.";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpGet("SystemHealth")]
        public async Task<IActionResult> SystemHealth(CancellationToken cancellationToken)
        {
            try
            {
                await PopulateAdminViewDataAsync();

                var viewModel = new SystemHealthViewModel
                {
                    OsVersion = RuntimeInformation.OSDescription,
                    DotNetVersion = RuntimeInformation.FrameworkDescription
                };

                // 1. Process CPU Usage (over 100ms sample)
                try
                {
                    var startTime = DateTime.UtcNow;
                    var startCpuTime = Process.GetCurrentProcess().TotalProcessorTime;
                    await Task.Delay(100, cancellationToken);
                    var endTime = DateTime.UtcNow;
                    var endCpuTime = Process.GetCurrentProcess().TotalProcessorTime;
                    var cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;
                    var totalMs = (endTime - startTime).TotalMilliseconds;
                    var cpuUsage = (cpuUsedMs / (Environment.ProcessorCount * totalMs)) * 100;
                    viewModel.CpuUsage = Math.Min(100.0, Math.Max(0.0, cpuUsage));
                }
                catch
                {
                    viewModel.CpuUsage = 0.0;
                }

                // 2. Process Memory
                try
                {
                    var process = Process.GetCurrentProcess();
                    viewModel.MemoryUsageMB = (double)process.WorkingSet64 / (1024 * 1024);
                    viewModel.PrivateMemoryMB = (double)process.PrivateMemorySize64 / (1024 * 1024);
                    viewModel.SystemUptime = (DateTime.UtcNow - process.StartTime.ToUniversalTime()).ToString(@"dd\.hh\:mm\:ss");
                }
                catch
                {
                    viewModel.SystemUptime = "N/A";
                }

                // 3. Database status
                try
                {
                    viewModel.DatabaseConnected = await _systemSettingService.CanConnectDatabaseAsync(cancellationToken);
                }
                catch
                {
                    viewModel.DatabaseConnected = false;
                }

                // 4. Load Error Logs
                var errorLogs = new List<ErrorLogEntry>();
                try
                {
                    var lines = SystemErrorLogger.ReadLastLines(50);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var entry = JsonSerializer.Deserialize<ErrorLogEntry>(line);
                            if (entry != null)
                            {
                                errorLogs.Add(entry);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading system health error logs");
                    }
                    viewModel.ErrorLogs = errorLogs;

                return View("~/Views/Admin/SystemHealth.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading system health metrics");
                TempData["AdminError"] = "Error loading system health metrics.";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost("ClearSystemErrors")]
        [ValidateAntiForgeryToken]
        [RateLimit(3, 10)]
        [AuditLog]
        public IActionResult ClearSystemErrors()
        {
            try
            {
                SystemErrorLogger.ClearLogs();
                TempData["AdminSuccess"] = "System error log cleared successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing system error logs");
                TempData["AdminError"] = "Failed to clear system error logs.";
            }
            return RedirectToAction(nameof(SystemHealth));
        }
    }
}
