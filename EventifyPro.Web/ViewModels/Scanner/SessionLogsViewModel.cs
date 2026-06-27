
namespace EventifyPro.Web.ViewModels.Scanner
{
    public class SessionLogsViewModel
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime SessionStart { get; set; }
        public PagedResult<ScanLogResponseDto> Logs { get; set; } = null!;
    }
}
