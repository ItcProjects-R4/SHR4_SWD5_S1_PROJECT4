using Microsoft.Data.SqlClient;

namespace LibraryMangementDAL
{
    public static class DBHelper
    {
        
        private static readonly string _connectionString =
            "Server=.;Database=LibraryDB;Integrated Security=True;TrustServerCertificate=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
