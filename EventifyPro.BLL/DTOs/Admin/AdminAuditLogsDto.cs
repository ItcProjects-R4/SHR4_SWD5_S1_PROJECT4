namespace EventifyPro.BLL.DTOs.Admin
{
    public class AdminAuditLogsDto
    {
        public PagedResult<AuditLog> PagedLogs { get; set; } = null!;
        public List<string> UniqueTables { get; set; } = new();
        public List<string> UniqueActions { get; set; } = new();
        public Dictionary<string, string> UserMap { get; set; } = new();
    }
}
