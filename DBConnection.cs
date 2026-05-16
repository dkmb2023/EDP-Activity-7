using MySql.Data.MySqlClient;

namespace ClinicSystem
{
    public static class DBConnection
    {
        private static readonly string connectionString =
            "Server=127.0.0.1;" +
            "Port=3306;" +
            "Database=clinic_management_db;" +
            "Uid=root;" +
            "Pwd=1234;" +          // ← put your MySQL root password here
            "Connect Timeout=10;";

        public static MySqlConnection GetConnection()
        {
            var conn = new MySqlConnection(connectionString);
            conn.Open();
            return conn;
        }
    }
}