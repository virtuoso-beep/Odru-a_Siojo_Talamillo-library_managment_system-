using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using LMS_Library_Management_System.Helper;
using LMS_Library_Management_System.Models;

namespace LMS_Library_Management_System.Service
{
    /// <summary>
    /// Service class for managing users (Admin functionality)
    /// Handles CRUD operations for Librarian and Staff accounts
    /// </summary>
    public class UserManagementService
    {
        private readonly AuthenticationService _authService;

        public UserManagementService()
        {
            _authService = new AuthenticationService();
        }

        /// <summary>
        /// Gets all users by role (Administrator or Staff)
        /// </summary>
        public List<UserInfo> GetUsersByRole(UserRole role)
        {
            var users = new List<UserInfo>();

            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var command = new MySqlCommand("sp_GetUsersByRole", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_Role", (int)role);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                users.Add(new UserInfo
                                {
                                    UserId = Convert.ToInt32(reader["UserId"]),
                                    Email = reader["Email"].ToString(),
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader["LastName"].ToString(),
                                    FullName = reader["FullName"].ToString(),
                                    Role = (UserRole)Convert.ToInt32(reader["Role"]),
                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting users by role: {ex.Message}");
                throw;
            }

            return users;
        }

        /// <summary>
        /// Gets a user by UserId
        /// </summary>
        public UserInfo GetUserById(int userId)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var command = new MySqlCommand("sp_GetUserById", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_UserId", userId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new UserInfo
                                {
                                    UserId = Convert.ToInt32(reader["UserId"]),
                                    Email = reader["Email"].ToString(),
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader["LastName"].ToString(),
                                    FullName = reader["FullName"].ToString(),
                                    Role = (UserRole)Convert.ToInt32(reader["Role"]),
                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user by ID: {ex.Message}");
                throw;
            }

            return null;
        }

        /// <summary>
        /// Gets a user by email
        /// </summary>
        public UserInfo GetUserByEmail(string email)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var command = new MySqlCommand("sp_GetUserByEmail", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_Email", email.Trim().ToLower());

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new UserInfo
                                {
                                    UserId = Convert.ToInt32(reader["UserId"]),
                                    Email = reader["Email"].ToString(),
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader["LastName"].ToString(),
                                    FullName = $"{reader["FirstName"]} {reader["LastName"]}",
                                    Role = (UserRole)Convert.ToInt32(reader["Role"]),
                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting user by email: {ex.Message}");
                throw;
            }

            return null;
        }

        /// <summary>
        /// Creates a new user (Librarian/Admin or Staff)
        /// </summary>
        public bool CreateUser(string email, string password, string firstName, string lastName, UserRole role)
        {
            try
            {
                // Check if user already exists
                if (_authService.UserExists(email))
                {
                    return false;
                }

                string hashedPassword = _authService.HashPassword(password);

                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var command = new MySqlCommand("sp_CreateUser", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_Email", email.Trim().ToLower());
                        command.Parameters.AddWithValue("p_PasswordHash", hashedPassword);
                        command.Parameters.AddWithValue("p_FirstName", firstName.Trim());
                        command.Parameters.AddWithValue("p_LastName", lastName.Trim());
                        command.Parameters.AddWithValue("p_Role", (int)role);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = Convert.ToInt32(reader["UserId"]);
                                return userId > 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating user: {ex.Message}");
                throw;
            }

            return false;
        }

        /// <summary>
        /// Updates user information (name and email)
        /// </summary>
        public bool UpdateUser(int userId, string email, string firstName, string lastName)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var command = new MySqlCommand("sp_UpdateUser", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_UserId", userId);
                        command.Parameters.AddWithValue("p_Email", email.Trim().ToLower());
                        command.Parameters.AddWithValue("p_FirstName", firstName.Trim());
                        command.Parameters.AddWithValue("p_LastName", lastName.Trim());

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int rowsAffected = Convert.ToInt32(reader["RowsAffected"]);
                                return rowsAffected > 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating user: {ex.Message}");
                throw;
            }

            return false;
        }

        /// <summary>
        /// Updates user password
        /// </summary>
        public bool UpdateUserPassword(string email, string newPassword)
        {
            try
            {
                string hashedPassword = _authService.HashPassword(newPassword);

                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var command = new MySqlCommand("sp_UpdateUserPassword", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_Email", email.Trim().ToLower());
                        command.Parameters.AddWithValue("p_PasswordHash", hashedPassword);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int rowsAffected = Convert.ToInt32(reader["RowsAffected"]);
                                return rowsAffected > 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating password: {ex.Message}");
                throw;
            }

            return false;
        }

        /// <summary>
        /// Updates user role
        /// </summary>
        public bool UpdateUserRole(int userId, UserRole newRole)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var command = new MySqlCommand("sp_UpdateUserRole", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_UserId", userId);
                        command.Parameters.AddWithValue("p_Role", (int)newRole);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int rowsAffected = Convert.ToInt32(reader["RowsAffected"]);
                                return rowsAffected > 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating user role: {ex.Message}");
                throw;
            }

            return false;
        }

        /// <summary>
        /// Deactivates a user account
        /// </summary>
        public bool DeactivateUser(int userId)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var command = new MySqlCommand("sp_DeactivateUser", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_UserId", userId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int rowsAffected = Convert.ToInt32(reader["RowsAffected"]);
                                return rowsAffected > 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deactivating user: {ex.Message}");
                throw;
            }

            return false;
        }

        /// <summary>
        /// Checks if a user exists by email
        /// </summary>
        public bool UserExists(string email)
        {
            return _authService.UserExists(email);
        }
    }

    /// <summary>
    /// Data class for user information in user management
    /// </summary>
    public class UserInfo
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Department { get; set; } // Optional field for display
        public string LastLogin { get; set; } // Optional field for display
    }
}

