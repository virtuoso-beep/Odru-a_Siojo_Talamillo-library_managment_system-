using System;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;

namespace Library_Management_System.Helpers
{
    public static class DatabaseHelper
    {
        private static string _connectionString;

        public static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    _connectionString = ConfigurationManager.ConnectionStrings["LibraryDB"]?.ConnectionString;
                    
                    if (string.IsNullOrEmpty(_connectionString))
                    {
                        throw new Exception("Database connection string not found in App.config. Please configure 'LibraryDB' connection string.");
                    }
                }
                return _connectionString;
            }
        }

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public static bool TestConnection()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

