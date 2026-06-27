namespace EventifyPro.Web.Helpers
{
    public static class SystemErrorLogger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "errors.jsonl");
        private static readonly object LogLock = new();

        public static void LogError(Exception exception, string? path = null, string? userId = null)
        {
            try
            {
                var directory = Path.GetDirectoryName(LogFilePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var entry = new
                {
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    Source = exception.Source,
                    Path = path,
                    UserId = userId
                };

                var jsonLine = JsonSerializer.Serialize(entry);
                lock (LogLock)
                {
                    File.AppendAllText(LogFilePath, jsonLine + Environment.NewLine);
                }
            }
            catch
            {
                // Fail silently to avoid interrupting the application
            }
        }

        public static string GetLogFilePath() => LogFilePath;

        public static System.Collections.Generic.List<string> ReadLastLines(int count)
        {
            lock (LogLock)
            {
                if (!File.Exists(LogFilePath))
                {
                    return new System.Collections.Generic.List<string>();
                }
                // Read lines safely, reverse to get latest first, and take required count
                return System.IO.File.ReadAllLines(LogFilePath).Reverse().Take(count).ToList();
            }
        }

        public static void ClearLogs()
        {
            try
            {
                lock (LogLock)
                {
                    if (File.Exists(LogFilePath))
                    {
                        File.Delete(LogFilePath);
                    }
                }
            }
            catch
            {
                // Fail silently
            }
        }
    }
}
