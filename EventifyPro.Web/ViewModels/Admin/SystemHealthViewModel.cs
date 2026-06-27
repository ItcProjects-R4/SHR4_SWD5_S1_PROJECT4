namespace EventifyPro.Web.ViewModels.Admin
{
    public class SystemHealthViewModel
    {
        public double CpuUsage { get; set; }
        public double MemoryUsageMB { get; set; }
        public double PrivateMemoryMB { get; set; }
        public bool DatabaseConnected { get; set; }
        public string SystemUptime { get; set; } = string.Empty;
        public string OsVersion { get; set; } = string.Empty;
        public string DotNetVersion { get; set; } = string.Empty;
        public List<ErrorLogEntry> ErrorLogs { get; set; } = new();
    }

    public class ErrorLogEntry
    {
        public string Timestamp { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? Source { get; set; }
        public string? Path { get; set; }
        public string? UserId { get; set; }
    }
}
