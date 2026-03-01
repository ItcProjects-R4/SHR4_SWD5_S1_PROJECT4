using Microsoft.Data.SqlClient;

namespace LibraryMangementDAL
{
    public class BorrowDAL
    {
        // ================================================
        // Borrow Book — SP_BorrowBook + Trigger
        // ================================================
        public (bool Success, string Message) BorrowBook(int bookId, int memberId)
        {
            try
            {
                using SqlConnection conn = DBHelper.GetConnection();
                conn.Open();

                using SqlCommand cmd = new SqlCommand("SP_BorrowBook", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BookID",   bookId);
                cmd.Parameters.AddWithValue("@MemberID", memberId);
                cmd.ExecuteNonQuery();

                return (true, "Book borrowed successfully.");
            }
            catch (SqlException ex)
            {
                return (false, ex.Message);
            }
        }

        // ================================================
        // Return Book — SP_ReturnBook
        // ================================================
        public (bool Success, string Message) ReturnBook(int borrowId)
        {
            try
            {
                using SqlConnection conn = DBHelper.GetConnection();
                conn.Open();

                using SqlCommand cmd = new SqlCommand("SP_ReturnBook", conn);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@BorrowID", borrowId);
                cmd.ExecuteNonQuery();

                return (true, "Book returned successfully.");
            }
            catch (SqlException ex)
            {
                return (false, ex.Message);
            }
        }

        // ================================================
        // Get Active Borrows
        // ================================================
        public List<(int BorrowId, string BookTitle, string MemberName, DateTime BorrowDate)> GetActiveBorrows()
        {
            var list = new List<(int, string, string, DateTime)>();

            using SqlConnection conn = DBHelper.GetConnection();
            conn.Open();

            string query = @"
                SELECT br.BorrowID, b.Title, m.FullName, br.BorrowDate
                FROM   Borrows br
                JOIN   Books   b ON br.BookID   = b.BookID
                JOIN   Members m ON br.MemberID = m.MemberID
                WHERE  br.ReturnDate IS NULL";

            using SqlCommand    cmd    = new SqlCommand(query, conn);
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
                list.Add((reader.GetInt32(0), reader.GetString(1),
                          reader.GetString(2), reader.GetDateTime(3)));

            return list;
        }
    }
}
