namespace EventifyPro.BLL.DTOs.Admin
{
    public class AdminUsersPageDto
    {
        public List<ApplicationUser> Users { get; set; } = new();
        public Dictionary<string, string> UserRoles { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalUsersCount { get; set; }
        public int ActiveUsersCount { get; set; }
        public int InactiveUsersCount { get; set; }
        public int OrganizersCount { get; set; }
        public int AdminsCount { get; set; }
        public int ScannersCount { get; set; }
        public int AttendeesCount { get; set; }
        public int TotalPages { get; set; }
    }
}
