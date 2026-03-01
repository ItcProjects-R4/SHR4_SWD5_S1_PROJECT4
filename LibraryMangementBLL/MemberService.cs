using LibraryMangementDAL;

namespace LibraryMangementBLL
{
    public class MemberService
    {
        private readonly MemberDAL _memberDAL = new MemberDAL();

        // ================================================
        // Get All Members
        // ================================================
        public List<(int Id, string FullName, string Phone)> GetAllMembers()
        {
            return _memberDAL.GetAllMembers();
        }

        // ================================================
        // Add Member with validation
        // ================================================
        public (bool Success, string Message) AddMember(string fullName, string phone)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Full name cannot be empty.");

            if (string.IsNullOrWhiteSpace(phone))
                return (false, "Phone cannot be empty.");

            bool result = _memberDAL.AddMember(fullName, phone);
            return result
                ? (true,  "Member added successfully.")
                : (false, "Failed to add member.");
        }
    }
}
