using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Threading;
using MySql.Data.MySqlClient;
namespace Library_Management_System.Helper
{
    public class MYSqlHelper
    {
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 1000;
        private const int ConnectionTimeoutSeconds = 30;
        public static MySqlConnection CreateConnection()
        {
            string connectionString = GetConnectionString();
            return CreateConnectionWithRetry(connectionString);
        }
        private static MySqlConnection CreateConnectionWithRetry(string connectionString, int retryCount = 0)
        {
            try
            {
                var connection = new MySqlConnection(connectionString);
                connection.Open();
                return connection;
            }
            catch (MySqlException ex) when (retryCount < MaxRetryAttempts)
            {
                if (ex.Number == 1042 || ex.Number == 2003 || ex.Number == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Database connection attempt {retryCount + 1} failed: {ex.Message}. Retrying...");
                    Thread.Sleep(RetryDelayMs * (retryCount + 1));
                    return CreateConnectionWithRetry(connectionString, retryCount + 1);
                }
                throw;
            }
        }
        public static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["MySQLConnection"].ConnectionString;
        }
        public static string GetBaseConnectionString()
        {
            var builder = new MySql.Data.MySqlClient.MySqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["MySQLConnection"].ConnectionString);
            builder.Database = "";
            return builder.ConnectionString;
        }
    }
}
