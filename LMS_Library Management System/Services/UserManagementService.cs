using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using LMS_Library_Management_System.Helper;
using LMS_Library_Management_System.Models;
using LMS_Library_Management_System.Interfaces;

namespace LMS_Library_Management_System.Service
{
    /// <summary>
    /// Service class for managing users (Admin functionality)
    /// Handles CRUD operations for Librarian and Staff accounts
    /// </summary>
    public class UserManagementService : IUserManagementService
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
                                var userInfo = new UserInfo
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

                                // Safely read Department if column exists
                                try
                                {
                                    int deptIndex = reader.GetOrdinal("Department");
                                    if (!reader.IsDBNull(deptIndex))
                                        userInfo.Department = reader.GetString(deptIndex);
                                }
                                catch (ArgumentException)
                                {
                                    // Column doesn't exist in result set - stored procedure might not be updated
                                    System.Diagnostics.Debug.WriteLine("Warning: Department column not found in sp_GetUsersByRole result set");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error reading Department: {ex.Message}");
                                }

                                // Safely read LastLogin if column exists
                                try
                                {
                                    int loginIndex = reader.GetOrdinal("LastLogin");
                                    if (!reader.IsDBNull(loginIndex))
                                        userInfo.LastLogin = reader.GetString(loginIndex);
                                }
                                catch (ArgumentException)
                                {
                                    // Column doesn't exist in result set - stored procedure might not be updated
                                    System.Diagnostics.Debug.WriteLine("Warning: LastLogin column not found in sp_GetUsersByRole result set");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error reading LastLogin: {ex.Message}");
                                }

                                // Safely read Phone if column exists
                                try
                                {
                                    int phoneIndex = reader.GetOrdinal("Phone");
                                    if (!reader.IsDBNull(phoneIndex))
                                        userInfo.Phone = reader.GetString(phoneIndex);
                                }
                                catch (ArgumentException)
                                {
                                    System.Diagnostics.Debug.WriteLine("Warning: Phone column not found in sp_GetUsersByRole result set");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error reading Phone: {ex.Message}");
                                }

                                users.Add(userInfo);
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
                                var userInfo = new UserInfo
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

                                // Safely read Department if column exists
                                try
                                {
                                    int deptIndex = reader.GetOrdinal("Department");
                                    if (!reader.IsDBNull(deptIndex))
                                        userInfo.Department = reader.GetString(deptIndex);
                                }
                                catch (ArgumentException)
                                {
                                    System.Diagnostics.Debug.WriteLine("Warning: Department column not found in sp_GetUserById result set");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error reading Department: {ex.Message}");
                                }

                                // Safely read LastLogin if column exists
                                try
                                {
                                    int loginIndex = reader.GetOrdinal("LastLogin");
                                    if (!reader.IsDBNull(loginIndex))
                                        userInfo.LastLogin = reader.GetString(loginIndex);
                                }
                                catch (ArgumentException)
                                {
                                    System.Diagnostics.Debug.WriteLine("Warning: LastLogin column not found in sp_GetUserById result set");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error reading LastLogin: {ex.Message}");
                                }

                                // Safely read Phone if column exists
                                try
                                {
                                    int phoneIndex = reader.GetOrdinal("Phone");
                                    if (!reader.IsDBNull(phoneIndex))
                                        userInfo.Phone = reader.GetString(phoneIndex);
                                }
                                catch (ArgumentException)
                                {
                                    System.Diagnostics.Debug.WriteLine("Warning: Phone column not found in sp_GetUserById result set");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error reading Phone: {ex.Message}");
                                }

                                return userInfo;
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
                                var userInfo = new UserInfo
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

                                // Safely read Department if column exists
                                try
                                {
                                    int deptIndex = reader.GetOrdinal("Department");
                                    if (!reader.IsDBNull(deptIndex))
                                        userInfo.Department = reader.GetString(deptIndex);
                                }
                                catch (ArgumentException)
                                {
                                    System.Diagnostics.Debug.WriteLine("Warning: Department column not found in sp_GetUserByEmail result set");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error reading Department: {ex.Message}");
                                }

                                // Safely read LastLogin if column exists
                                try
                                {
                                    int loginIndex = reader.GetOrdinal("LastLogin");
                                    if (!reader.IsDBNull(loginIndex))
                                        userInfo.LastLogin = reader.GetString(loginIndex);
                                }
                                catch (ArgumentException)
                                {
                                    System.Diagnostics.Debug.WriteLine("Warning: LastLogin column not found in sp_GetUserByEmail result set");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error reading LastLogin: {ex.Message}");
                                }

                                // Safely read Phone if column exists
                                try
                                {
                                    int phoneIndex = reader.GetOrdinal("Phone");
                                    if (!reader.IsDBNull(phoneIndex))
                                        userInfo.Phone = reader.GetString(phoneIndex);
                                }
                                catch (ArgumentException)
                                {
                                    System.Diagnostics.Debug.WriteLine("Warning: Phone column not found in sp_GetUserByEmail result set");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error reading Phone: {ex.Message}");
                                }

                                return userInfo;
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
        /// Returns: true if successful, false if user already exists, throws exception on other errors
        /// </summary>
        public bool CreateUser(string email, string password, string firstName, string lastName, UserRole role, string department = null, string phone = null)
        {
            try
            {
                // Check if user already exists (case-insensitive email check)
                if (_authService.UserExists(email))
                {
                    System.Diagnostics.Debug.WriteLine($"User with email {email} already exists");
                    return false; // User already exists - return false to indicate duplicate
                }

                string hashedPassword = _authService.HashPassword(password);

                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Try stored procedure first
                    try
                    {
                        using (var command = new MySqlCommand("sp_CreateUser", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("p_Email", email.Trim().ToLower());
                            command.Parameters.AddWithValue("p_PasswordHash", hashedPassword);
                            command.Parameters.AddWithValue("p_FirstName", firstName.Trim());
                            command.Parameters.AddWithValue("p_LastName", lastName.Trim());
                            command.Parameters.AddWithValue("p_Role", (int)role);
                            command.Parameters.AddWithValue("p_Department", string.IsNullOrWhiteSpace(department) ? (object)DBNull.Value : department.Trim());
                            command.Parameters.AddWithValue("p_Phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone.Trim());

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int userId = Convert.ToInt32(reader["UserId"]);
                                    System.Diagnostics.Debug.WriteLine($"User created via stored procedure: UserId = {userId}");
                                    return userId > 0;
                                }
                            }
                        }
                    }
                    catch (MySqlException spEx)
                    {
                        // If stored procedure doesn't exist (error 1305) or doesn't support Phone, fall back to direct SQL
                        if (spEx.Number == 1305 || spEx.Message.Contains("Phone"))
                        {
                            System.Diagnostics.Debug.WriteLine("sp_CreateUser stored procedure not found or doesn't support Phone, using direct SQL");
                            using (var command = new MySqlCommand(
                                "INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role, Department, Phone) VALUES (@Email, @PasswordHash, @FirstName, @LastName, @Role, @Department, @Phone); SELECT LAST_INSERT_ID() AS UserId;", 
                                connection))
                            {
                                command.Parameters.AddWithValue("@Email", email.Trim().ToLower());
                                command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                                command.Parameters.AddWithValue("@FirstName", firstName.Trim());
                                command.Parameters.AddWithValue("@LastName", lastName.Trim());
                                command.Parameters.AddWithValue("@Role", (int)role);
                                command.Parameters.AddWithValue("@Department", string.IsNullOrWhiteSpace(department) ? (object)DBNull.Value : department.Trim());
                                command.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone.Trim());
                                
                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        int userId = Convert.ToInt32(reader["UserId"]);
                                        System.Diagnostics.Debug.WriteLine($"User created via direct SQL: UserId = {userId}");
                                        return userId > 0;
                                    }
                                }
                            }
                        }
                        throw;
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                // Check for duplicate entry error (MySQL error code 1062)
                if (mysqlEx.Number == 1062)
                {
                    // Check if it's a duplicate email error
                    if (mysqlEx.Message.Contains("Email") || mysqlEx.Message.Contains("email") || mysqlEx.Message.Contains("Duplicate email"))
                    {
                        System.Diagnostics.Debug.WriteLine($"Duplicate email when creating user: {email}");
                        return false; // User already exists - duplicate email
                    }
                    System.Diagnostics.Debug.WriteLine($"Duplicate user entry: {email}");
                    return false; // User already exists
                }
                System.Diagnostics.Debug.WriteLine($"Error creating user: {mysqlEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating user: {ex.Message}");
                throw;
            }

            return false;
        }

        /// <summary>
        /// Updates user information (name, email, phone, and department)
        /// </summary>
        public bool UpdateUser(int userId, string email, string firstName, string lastName, string department = null, string phone = null)
        {
            try
            {
                // Check if email already exists for a different user
                var existingUser = GetUserByEmail(email);
                if (existingUser != null && existingUser.UserId != userId)
                {
                    System.Diagnostics.Debug.WriteLine($"Email {email} already exists for user {existingUser.UserId}");
                    throw new InvalidOperationException($"Email address '{email}' is already in use by another user.");
                }

                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Try stored procedure first
                    try
                    {
                        using (var command = new MySqlCommand("sp_UpdateUser", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("p_UserId", userId);
                            command.Parameters.AddWithValue("p_Email", email.Trim().ToLower());
                            command.Parameters.AddWithValue("p_FirstName", firstName.Trim());
                            command.Parameters.AddWithValue("p_LastName", lastName.Trim());
                            command.Parameters.AddWithValue("p_Department", string.IsNullOrWhiteSpace(department) ? (object)DBNull.Value : department.Trim());
                            command.Parameters.AddWithValue("p_Phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone.Trim());

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int rowsAffected = Convert.ToInt32(reader["RowsAffected"]);
                                    System.Diagnostics.Debug.WriteLine($"User updated via stored procedure: {rowsAffected} rows affected");
                                    return rowsAffected > 0;
                                }
                            }
                        }
                    }
                    catch (MySqlException spEx)
                    {
                        // Handle duplicate email error (MySQL error code 1062)
                        if (spEx.Number == 1062 && spEx.Message.Contains("Email"))
                        {
                            System.Diagnostics.Debug.WriteLine($"Duplicate email entry: {email}");
                            throw new InvalidOperationException($"Email address '{email}' is already in use by another user.");
                        }
                        
                        // If stored procedure doesn't exist (error 1305), fall back to direct SQL
                        if (spEx.Number == 1305)
                        {
                            System.Diagnostics.Debug.WriteLine("sp_UpdateUser stored procedure not found, using direct SQL");
                            using (var command = new MySqlCommand(
                                "UPDATE Users SET Email = @Email, FirstName = @FirstName, LastName = @LastName, Department = @Department, Phone = @Phone WHERE UserId = @UserId", 
                                connection))
                            {
                                command.Parameters.AddWithValue("@UserId", userId);
                                command.Parameters.AddWithValue("@Email", email.Trim().ToLower());
                                command.Parameters.AddWithValue("@FirstName", firstName.Trim());
                                command.Parameters.AddWithValue("@LastName", lastName.Trim());
                                command.Parameters.AddWithValue("@Department", string.IsNullOrWhiteSpace(department) ? (object)DBNull.Value : department.Trim());
                                command.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone.Trim());
                                
                                try
                                {
                                    int rowsAffected = command.ExecuteNonQuery();
                                    System.Diagnostics.Debug.WriteLine($"User updated via direct SQL: {rowsAffected} rows affected");
                                    return rowsAffected > 0;
                                }
                                catch (MySqlException sqlEx)
                                {
                                    // Handle duplicate email error in direct SQL
                                    if (sqlEx.Number == 1062 && sqlEx.Message.Contains("Email"))
                                    {
                                        throw new InvalidOperationException($"Email address '{email}' is already in use by another user.");
                                    }
                                    throw;
                                }
                            }
                        }
                        throw;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Re-throw InvalidOperationException (duplicate email) as-is
                throw;
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
                string normalizedEmail = email.Trim().ToLower();

                using (var connection = MYSqlHelper.CreateConnection())
                {
                    try
                    {
                        // Try to use stored procedure first
                        using (var command = new MySqlCommand("sp_UpdateUserPassword", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("p_Email", normalizedEmail);
                            command.Parameters.AddWithValue("p_PasswordHash", hashedPassword);

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int rowsAffected = Convert.ToInt32(reader["RowsAffected"]);
                                    if (rowsAffected > 0)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Password updated successfully for {normalizedEmail}");
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    catch (MySqlException sqlEx) when (sqlEx.Number == 1305) // Procedure doesn't exist
                    {
                        // Fallback to direct SQL if stored procedure doesn't exist
                        System.Diagnostics.Debug.WriteLine("sp_UpdateUserPassword not found, using direct SQL");
                        
                        string updateQuery = @"
                            UPDATE Users 
                            SET PasswordHash = @PasswordHash 
                            WHERE LOWER(Email) = @Email";
                        
                        using (var command = new MySqlCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@Email", normalizedEmail);
                            command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                            
                            int rowsAffected = command.ExecuteNonQuery();
                            if (rowsAffected > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"Password updated successfully for {normalizedEmail} (using direct SQL)");
                                return true;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"No user found with email: {normalizedEmail}");
                                return false;
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
                    try
                    {
                        // Try to use stored procedure first
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
                                    if (rowsAffected > 0)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Role updated successfully for user ID {userId}");
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    catch (MySqlException sqlEx) when (sqlEx.Number == 1305) // Procedure doesn't exist
                    {
                        // Fallback to direct SQL if stored procedure doesn't exist
                        System.Diagnostics.Debug.WriteLine("sp_UpdateUserRole not found, using direct SQL");
                        
                        string updateQuery = @"
                            UPDATE Users 
                            SET Role = @Role 
                            WHERE UserId = @UserId";
                        
                        using (var command = new MySqlCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@UserId", userId);
                            command.Parameters.AddWithValue("@Role", (int)newRole);
                            
                            int rowsAffected = command.ExecuteNonQuery();
                            if (rowsAffected > 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"Role updated successfully for user ID {userId} (using direct SQL)");
                                return true;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"No user found with ID: {userId}");
                                return false;
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
        /// Deletes a user account completely from the database
        /// </summary>
        public bool DeactivateUser(int userId)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var command = new MySqlCommand("sp_DeleteUser", connection))
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
                System.Diagnostics.Debug.WriteLine($"Error deleting user: {ex.Message}");
                throw;
            }

            return false;
        }

        /// <summary>
        /// Updates user's last login timestamp
        /// </summary>
        public bool UpdateLastLogin(string email)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Try stored procedure first
                    try
                    {
                        using (var command = new MySqlCommand("sp_UpdateLastLogin", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            command.Parameters.AddWithValue("p_Email", email.Trim().ToLower());

                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int rowsAffected = Convert.ToInt32(reader["RowsAffected"]);
                                    System.Diagnostics.Debug.WriteLine($"LastLogin updated for {email}: {rowsAffected} rows affected");
                                    return rowsAffected > 0;
                                }
                            }
                        }
                    }
                    catch (MySqlException spEx)
                    {
                        // If stored procedure doesn't exist, try direct SQL update
                        if (spEx.Number == 1305) // Procedure doesn't exist
                        {
                            System.Diagnostics.Debug.WriteLine($"Stored procedure sp_UpdateLastLogin not found, using direct SQL update");
                            using (var command = new MySqlCommand("UPDATE Users SET LastLogin = NOW() WHERE LOWER(Email) = LOWER(@Email)", connection))
                            {
                                command.Parameters.AddWithValue("@Email", email.Trim().ToLower());
                                int rowsAffected = command.ExecuteNonQuery();
                                System.Diagnostics.Debug.WriteLine($"LastLogin updated via direct SQL for {email}: {rowsAffected} rows affected");
                                return rowsAffected > 0;
                            }
                        }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating last login for {email}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                // Don't throw - last login update failure shouldn't prevent login
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

        /// <summary>
        /// Updates user's IsActive status in the database
        /// </summary>
        public bool UpdateUserStatus(int userId, bool isActive)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var command = new MySqlCommand("sp_UpdateUserStatus", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_UserId", userId);
                        command.Parameters.AddWithValue("p_IsActive", isActive);

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
                System.Diagnostics.Debug.WriteLine($"Error updating user status: {ex.Message}");
                // Fallback to direct SQL if stored procedure doesn't exist
                try
                {
                    using (var connection = MYSqlHelper.CreateConnection())
                    {
                        using (var command = new MySqlCommand("UPDATE Users SET IsActive = @IsActive WHERE UserId = @UserId", connection))
                        {
                            command.Parameters.AddWithValue("@UserId", userId);
                            command.Parameters.AddWithValue("@IsActive", isActive);
                            int rowsAffected = command.ExecuteNonQuery();
                            return rowsAffected > 0;
                        }
                    }
                }
                catch
                {
                    throw;
                }
            }

            return false;
        }

        /// <summary>
        /// Automatically sets users to Inactive if they haven't logged in for specified days (default 90 days)
        /// </summary>
        public int AutoDeactivateInactiveUsers(int daysInactive = 90)
        {
            int deactivatedCount = 0;
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Update users who haven't logged in for the specified number of days
                    string sql = @"
                        UPDATE Users 
                        SET IsActive = FALSE 
                        WHERE IsActive = TRUE 
                        AND (
                            LastLogin IS NULL 
                            OR LastLogin < DATE_SUB(NOW(), INTERVAL @DaysInactive DAY)
                        )";
                    
                    using (var command = new MySqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@DaysInactive", daysInactive);
                        deactivatedCount = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error auto-deactivating inactive users: {ex.Message}");
            }
            return deactivatedCount;
        }
    }

    /// <summary>
    /// Data class for user information in user management
    /// </summary>
}
