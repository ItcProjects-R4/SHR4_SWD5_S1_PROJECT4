namespace EventifyPro.BLL.DTOs.Admin
{
    public class AdminUserDetailsDto
    {
        public ApplicationUser User { get; set; } = null!;
        public string PrimaryRole { get; set; } = string.Empty;
        public List<Eventify.Domain.Entities.Booking> Bookings { get; set; } = new();
        public List<Eventify.Domain.Entities.Payment> Payments { get; set; } = new();
        public List<Eventify.Domain.Entities.Review> Reviews { get; set; } = new();
        public List<AuditLog> AuditLogs { get; set; } = new();
    }
}
