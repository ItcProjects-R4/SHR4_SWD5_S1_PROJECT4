using Microsoft.Data.SqlClient;

namespace LibraryMangementDAL
{
    public class MemberDAL
    {
        // ================================================
        // Get All Members
        // ================================================
        public List<(int Id, string FullName, string Phone)> GetAllMembers()
        {
            var list = new List<(int, string, string)>();

            using SqlConnection conn = DBHelper.GetConnection();
            conn.Open();

            using SqlCommand cmd = new SqlCommand(
                "SELECT MemberID, FullName, Phone FROM Members", conn);
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
                list.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));

            return list;
        }

        // ================================================
        // Add Member
        // ================================================
        public bool AddMember(string fullName, string phone)
        {
            using SqlConnection conn = DBHelper.GetConnection();
            conn.Open();

            using SqlCommand cmd = new SqlCommand(
                "INSERT INTO Members (FullName, Phone) VALUES (@FullName, @Phone)", conn);
            cmd.Parameters.AddWithValue("@FullName", fullName);
            cmd.Parameters.AddWithValue("@Phone",    phone);

            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
