using System;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using LMS_Library_Management_System.Helper;
using LMS_Library_Management_System.Models;

namespace LMS_Library_Management_System.Helper
{
    /// <summary>
    /// Helper class to create users in the database
    /// This can be used to manually create additional users
    /// </summary>
    public static class UserCreationHelper
    {
        /// <summary>
        /// Creates a new user in the database
        /// </summary>
        /// <param name="email">User email</param>
        /// <param name="password">Plain text password</param>
        /// <param name="firstName">First name</param>
        /// <param name="lastName">Last name</param>
        /// <param name="role">User role (1=Admin, 2=Staff, 3=Member)</param>
        /// <returns>True if user was created successfully</returns>
        public static bool CreateUser(string email, string password, string firstName, string lastName, UserRole role)
        {
            try
            {
                string hashedPassword = HashPassword(password);
                
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    
                    using (var command = new MySqlCommand("sp_CreateUser", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_Email", email.Trim().ToLower());
                        command.Parameters.AddWithValue("p_PasswordHash", hashedPassword);
                        command.Parameters.AddWithValue("p_FirstName", firstName);
                        command.Parameters.AddWithValue("p_LastName", lastName);
                        command.Parameters.AddWithValue("p_Role", (int)role);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = Convert.ToInt32(reader["UserId"]);
                                return userId > 0;
                            }
                        }
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating user: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Generates a password hash (same method used by AuthenticationService)
        /// </summary>
        private static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty");
            
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}

