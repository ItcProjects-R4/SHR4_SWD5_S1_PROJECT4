

namespace EventifyPro.Web.ViewModels.Scanner
{
    public class EventLogsViewModel
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public PagedResult<ScanLogResponseDto> Logs { get; set; } = null!;
    }
}
