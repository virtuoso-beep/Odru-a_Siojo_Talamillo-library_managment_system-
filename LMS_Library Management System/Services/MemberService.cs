using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using LMS_Library_Management_System.Helper;
using LMS_Library_Management_System.Models;

namespace LMS_Library_Management_System.Service
{
    /// <summary>
    /// Service class for managing library members
    /// Handles CRUD operations for Member accounts
    /// </summary>
    public class MemberService
    {
        private readonly AuthenticationService _authService;
        private readonly UserManagementService _userManagementService;

        public MemberService()
        {
            _authService = new AuthenticationService();
            _userManagementService = new UserManagementService();
        }

        /// <summary>
        /// Gets a single member by member number
        /// </summary>
        public MemberData GetMemberByNumber(string memberNumber)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT 
                            m.MemberId,
                            m.MemberNumber,
                            m.MemberType,
                            m.Phone,
                            m.Address,
                            m.Department,
                            m.Status,
                            m.RegistrationDate,
                            u.FirstName,
                            u.LastName,
                            u.Email
                        FROM Members m
                        INNER JOIN Users u ON m.UserId = u.UserId
                        WHERE m.MemberNumber = @MemberNumber";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MemberNumber", memberNumber);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new MemberData
                                {
                                    MemberId = reader.GetInt32("MemberId"),
                                    MemberNumber = reader.GetString("MemberNumber"),
                                    FirstName = reader.GetString("FirstName"),
                                    LastName = reader.GetString("LastName"),
                                    Email = reader.GetString("Email"),
                                    MemberType = reader.GetString("MemberType"),
                                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? "" : reader.GetString("Phone"),
                                    Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? "" : reader.GetString("Address"),
                                    Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? "" : reader.GetString("Department"),
                                    Status = reader.GetInt32("Status"),
                                    RegistrationDate = reader.GetDateTime("RegistrationDate")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting member: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Updates an existing member in the database
        /// </summary>
        public bool UpdateMember(string memberNumber, string firstName, string lastName, string email, string phone, string address, string department, string memberType, int status)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // First, get the UserId from MemberNumber
                            int userId = -1;
                            using (var command = new MySqlCommand("SELECT UserId FROM Members WHERE MemberNumber = @MemberNumber", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@MemberNumber", memberNumber);
                                object result = command.ExecuteScalar();
                                if (result == null || result == DBNull.Value)
                                {
                                    transaction.Rollback();
                                    return false;
                                }
                                userId = Convert.ToInt32(result);
                            }

                            // Update Users table
                            using (var command = new MySqlCommand(@"
                                UPDATE Users 
                                SET FirstName = @FirstName,
                                    LastName = @LastName,
                                    Email = @Email
                                WHERE UserId = @UserId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@FirstName", firstName.Trim());
                                command.Parameters.AddWithValue("@LastName", lastName.Trim());
                                command.Parameters.AddWithValue("@Email", email.Trim());
                                command.Parameters.AddWithValue("@UserId", userId);
                                command.ExecuteNonQuery();
                            }

                            // Update Members table
                            using (var command = new MySqlCommand(@"
                                UPDATE Members 
                                SET Phone = @Phone,
                                    Address = @Address,
                                    Department = @Department,
                                    MemberType = @MemberType,
                                    Status = @Status
                                WHERE MemberNumber = @MemberNumber", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone.Trim());
                                command.Parameters.AddWithValue("@Address", string.IsNullOrWhiteSpace(address) ? (object)DBNull.Value : address.Trim());
                                command.Parameters.AddWithValue("@Department", string.IsNullOrWhiteSpace(department) ? (object)DBNull.Value : department.Trim());
                                command.Parameters.AddWithValue("@MemberType", memberType);
                                command.Parameters.AddWithValue("@Status", status);
                                command.Parameters.AddWithValue("@MemberNumber", memberNumber);
                                command.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Error updating member: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating member: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Deletes a member permanently from the database
        /// </summary>
        public bool DeleteMember(string memberNumber)
        {
            using (var connection = MYSqlHelper.CreateConnection())
            {
                // Connection is already opened by MYSqlHelper.CreateConnection()
                MySqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    int userId = -1;
                    int memberId = -1;

                    // Get UserId and MemberId first
                    string getIdsQuery = "SELECT UserId, MemberId FROM Members WHERE MemberNumber = @MemberNumber";
                    using (var cmd = new MySqlCommand(getIdsQuery, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@MemberNumber", memberNumber);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userId = reader.GetInt32("UserId");
                                memberId = reader.GetInt32("MemberId");
                            }
                            reader.Close();
                        }
                    }

                    if (userId == -1 || memberId == -1)
                    {
                        System.Diagnostics.Debug.WriteLine($"Member with number {memberNumber} not found for deletion.");
                        transaction.Rollback();
                        return false;
                    }

                    // Delete from Members table
                    string deleteMemberQuery = "DELETE FROM Members WHERE MemberId = @MemberId";
                    using (var cmd = new MySqlCommand(deleteMemberQuery, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@MemberId", memberId);
                        cmd.ExecuteNonQuery();
                    }

                    // Delete from Users table
                    string deleteUserQuery = "DELETE FROM Users WHERE UserId = @UserId";
                    using (var cmd = new MySqlCommand(deleteUserQuery, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    System.Diagnostics.Debug.WriteLine($"Successfully deleted member {memberNumber} and associated user record.");
                    return true;
                }
                catch (MySqlException ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"MySQL Error deleting member: {ex.Message}");
                    
                    // Specific error handling for foreign key constraint violation
                    if (ex.Number == 1451) // Foreign key constraint violation
                    {
                        throw new InvalidOperationException(
                            "Cannot delete member due to existing related records (e.g., active borrowings, fines, or reservations). " +
                            "Please resolve these dependencies first.", ex);
                    }
                    throw; // Re-throw other exceptions
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"Error deleting member: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a new member in the database
        /// </summary>
        /// <param name="email">Email address (must be @umindanao.edu.ph for Student/Faculty)</param>
        /// <param name="firstName">First name</param>
        /// <param name="lastName">Last name</param>
        /// <param name="memberType">Member type (Student, Faculty, Staff, Guest)</param>
        /// <param name="phone">Phone number (11 digits starting with 09)</param>
        /// <param name="address">Address</param>
        /// <param name="department">Department</param>
        /// <returns>MemberId if successful, -1 if failed</returns>
        public int CreateMember(string email, string firstName, string lastName, string memberType, string phone = null, string address = null, string department = null)
        {
            try
            {
                // Check if user already exists
                if (_authService.UserExists(email))
                {
                    System.Diagnostics.Debug.WriteLine($"User with email {email} already exists");
                    return -1; // User already exists
                }

                // Generate a default password (members can reset it later)
                string defaultPassword = "Member@123"; // Default password, should be changed on first login
                string hashedPassword = _authService.HashPassword(defaultPassword);

                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Start transaction
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int userId = -1;

                            // First, create the user account
                            using (var command = new MySqlCommand("sp_CreateUser", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("p_Email", email.Trim().ToLower());
                                command.Parameters.AddWithValue("p_PasswordHash", hashedPassword);
                                command.Parameters.AddWithValue("p_FirstName", firstName.Trim());
                                command.Parameters.AddWithValue("p_LastName", lastName.Trim());
                                command.Parameters.AddWithValue("p_Role", (int)UserRole.Member);
                                command.Parameters.AddWithValue("p_Department", DBNull.Value);
                                command.Parameters.AddWithValue("p_Phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone.Trim());

                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        userId = Convert.ToInt32(reader["UserId"]);
                                    }
                                }
                            }

                            if (userId <= 0)
                            {
                                transaction.Rollback();
                                System.Diagnostics.Debug.WriteLine("Failed to create user account");
                                return -1;
                            }

                            // Generate member number
                            string memberNumber = GenerateMemberNumber(memberType);

                            // Create member record
                            int memberId = -1;
                            using (var command = new MySqlCommand(@"
                                INSERT INTO Members (UserId, MemberNumber, MemberType, Phone, Address, Department, Status, RegistrationDate)
                                VALUES (@UserId, @MemberNumber, @MemberType, @Phone, @Address, @Department, 1, NOW())", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", userId);
                                command.Parameters.AddWithValue("@MemberNumber", memberNumber);
                                command.Parameters.AddWithValue("@MemberType", memberType);
                                command.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone.Trim());
                                command.Parameters.AddWithValue("@Address", string.IsNullOrWhiteSpace(address) ? (object)DBNull.Value : address.Trim());
                                command.Parameters.AddWithValue("@Department", string.IsNullOrWhiteSpace(department) ? (object)DBNull.Value : department.Trim());

                                command.ExecuteNonQuery();
                            }

                            // Get the last inserted ID
                            using (var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection, transaction))
                            {
                                object result = command.ExecuteScalar();
                                if (result != null && result != DBNull.Value)
                                {
                                    memberId = Convert.ToInt32(result);
                                }
                            }

                            if (memberId <= 0)
                            {
                                transaction.Rollback();
                                System.Diagnostics.Debug.WriteLine("Failed to create member record");
                                return -1;
                            }

                            // Commit transaction
                            transaction.Commit();
                            System.Diagnostics.Debug.WriteLine($"Member created successfully: MemberId = {memberId}, UserId = {userId}");
                            return memberId;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            System.Diagnostics.Debug.WriteLine($"Error creating member: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                // Check for duplicate entry error (MySQL error code 1062)
                if (mysqlEx.Number == 1062)
                {
                    System.Diagnostics.Debug.WriteLine($"Duplicate member entry: {email}");
                    return -1; // Member already exists
                }
                System.Diagnostics.Debug.WriteLine($"Error creating member: {mysqlEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating member: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generates a unique member number based on member type
        /// Format: MEM-YYYY-XXXX where XXXX is a sequential number
        /// </summary>
        private string GenerateMemberNumber(string memberType)
        {
            try
            {
                string prefix = "MEM";
                string year = DateTime.Now.Year.ToString();
                
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Get the last member number for this year
                    using (var command = new MySqlCommand(@"
                        SELECT MemberNumber 
                        FROM Members 
                        WHERE MemberNumber LIKE @Pattern 
                        ORDER BY MemberNumber DESC 
                        LIMIT 1", connection))
                    {
                        string pattern = $"{prefix}-{year}-%";
                        command.Parameters.AddWithValue("@Pattern", pattern);
                        
                        object result = command.ExecuteScalar();
                        int nextNumber = 1;
                        
                        if (result != null && result != DBNull.Value)
                        {
                            string lastMemberNumber = result.ToString();
                            // Extract the number part (last 4 digits)
                            if (lastMemberNumber.Length >= 4)
                            {
                                string numberPart = lastMemberNumber.Substring(lastMemberNumber.Length - 4);
                                if (int.TryParse(numberPart, out int lastNumber))
                                {
                                    nextNumber = lastNumber + 1;
                                }
                            }
                        }
                        
                        // Format: MEM-YYYY-XXXX (4 digits with leading zeros)
                        return $"{prefix}-{year}-{nextNumber:D4}";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating member number: {ex.Message}");
                // Fallback: use timestamp-based number
                return $"MEM-{DateTime.Now.Year}-{DateTime.Now.Ticks.ToString().Substring(Math.Max(0, DateTime.Now.Ticks.ToString().Length - 4))}";
            }
        }

        /// <summary>
        /// Validates email format for Student and Faculty members
        /// </summary>
        public bool IsValidMemberEmail(string email, string memberType)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim().ToLower();

            // Student and Faculty must have @umindanao.edu.ph email
            if (memberType == "Student" || memberType == "Faculty")
            {
                if (!email.EndsWith("@umindanao.edu.ph"))
                    return false;

                // Basic email format validation
                try
                {
                    var addr = new System.Net.Mail.MailAddress(email);
                    return addr.Address == email;
                }
                catch
                {
                    return false;
                }
            }

            // Staff and Guest can have any valid email format
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates phone number format (11 digits starting with 09)
        /// </summary>
        public bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return true; // Phone is optional

            phone = phone.Trim();
            
            // Must be exactly 11 digits
            if (phone.Length != 11)
                return false;

            // Must start with "09"
            if (!phone.StartsWith("09"))
                return false;

            // Must be all digits
            return System.Text.RegularExpressions.Regex.IsMatch(phone, @"^[0-9]{11}$");
        }

        /// <summary>
        /// Gets all members from the database
        /// </summary>
        public System.Collections.Generic.List<MemberData> GetAllMembers()
        {
            var members = new System.Collections.Generic.List<MemberData>();
            
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT 
                            m.MemberId,
                            m.MemberNumber,
                            m.MemberType,
                            m.Phone,
                            m.Address,
                            m.Department,
                            m.Status,
                            m.RegistrationDate,
                            u.FirstName,
                            u.LastName,
                            u.Email
                        FROM Members m
                        INNER JOIN Users u ON m.UserId = u.UserId
                        ORDER BY m.RegistrationDate DESC";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var member = new MemberData
                                {
                                    MemberId = reader.GetInt32("MemberId"),
                                    MemberNumber = reader.GetString("MemberNumber"),
                                    FirstName = reader.GetString("FirstName"),
                                    LastName = reader.GetString("LastName"),
                                    Email = reader.GetString("Email"),
                                    MemberType = reader.GetString("MemberType"),
                                    Phone = reader.IsDBNull(reader.GetOrdinal("Phone")) ? "" : reader.GetString("Phone"),
                                    Address = reader.IsDBNull(reader.GetOrdinal("Address")) ? "" : reader.GetString("Address"),
                                    Department = reader.IsDBNull(reader.GetOrdinal("Department")) ? "" : reader.GetString("Department"),
                                    Status = reader.GetInt32("Status"),
                                    RegistrationDate = reader.GetDateTime("RegistrationDate")
                                };
                                
                                members.Add(member);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting members: {ex.Message}");
                throw;
            }
            
            return members;
        }

        /// <summary>
        /// Gets borrowing history for a specific member
        /// </summary>
        public List<BorrowingData> GetMemberBorrowingHistory(int memberId)
        {
            var borrowings = new List<BorrowingData>();
            
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT 
                            b.BorrowingId,
                            bk.Title AS BookTitle,
                            bk.Author,
                            b.BorrowDate,
                            b.DueDate,
                            b.ReturnDate,
                            b.Status,
                            COALESCE(b.FineAmount, 0) AS FineAmount
                        FROM Borrowings b
                        INNER JOIN Books bk ON b.BookId = bk.BookId
                        WHERE b.MemberId = @MemberId
                        ORDER BY b.BorrowDate DESC";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MemberId", memberId);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var borrowing = new BorrowingData
                                {
                                    BorrowingId = reader.GetInt32("BorrowingId"),
                                    BookTitle = reader.GetString("BookTitle"),
                                    Author = reader.GetString("Author"),
                                    BorrowDate = reader.GetDateTime("BorrowDate"),
                                    DueDate = reader.GetDateTime("DueDate"),
                                    ReturnDate = reader.IsDBNull(reader.GetOrdinal("ReturnDate")) ? (DateTime?)null : reader.GetDateTime("ReturnDate"),
                                    Status = reader.GetString("Status"),
                                    FineAmount = reader.GetDecimal("FineAmount")
                                };
                                
                                borrowings.Add(borrowing);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting borrowing history: {ex.Message}");
            }
            
            return borrowings;
        }

        /// <summary>
        /// Gets statistics for a specific member
        /// </summary>
        public MemberStatistics GetMemberStatistics(int memberId, string memberType)
        {
            var stats = new MemberStatistics
            {
                MaxBooks = GetBookLimit(memberType)
            };
            
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Get current borrowed books count
                    string currentBooksQuery = @"
                        SELECT COUNT(*) 
                        FROM Borrowings 
                        WHERE MemberId = @MemberId AND Status = 'Borrowed'";
                    
                    using (var command = new MySqlCommand(currentBooksQuery, connection))
                    {
                        command.Parameters.AddWithValue("@MemberId", memberId);
                        stats.CurrentBooksCount = Convert.ToInt32(command.ExecuteScalar());
                    }
                    
                    // Get total borrowed count
                    string totalBorrowedQuery = @"
                        SELECT COUNT(*) 
                        FROM Borrowings 
                        WHERE MemberId = @MemberId";
                    
                    using (var command = new MySqlCommand(totalBorrowedQuery, connection))
                    {
                        command.Parameters.AddWithValue("@MemberId", memberId);
                        stats.TotalBorrowed = Convert.ToInt32(command.ExecuteScalar());
                    }
                    
                    // Get unpaid fines
                    string finesQuery = @"
                        SELECT COALESCE(SUM(Amount), 0) 
                        FROM Fines 
                        WHERE MemberId = @MemberId AND Status = 'Unpaid'";
                    
                    using (var command = new MySqlCommand(finesQuery, connection))
                    {
                        command.Parameters.AddWithValue("@MemberId", memberId);
                        stats.UnpaidFines = Convert.ToDecimal(command.ExecuteScalar());
                    }
                    
                    // Get membership expiry date
                    string expiryQuery = @"
                        SELECT MembershipExpiryDate 
                        FROM Members 
                        WHERE MemberId = @MemberId";
                    
                    using (var command = new MySqlCommand(expiryQuery, connection))
                    {
                        command.Parameters.AddWithValue("@MemberId", memberId);
                        var result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            stats.MembershipExpiry = Convert.ToDateTime(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting member statistics: {ex.Message}");
            }
            
            return stats;
        }

        /// <summary>
        /// Gets the current number of books borrowed by a member using MemberNumber
        /// </summary>
        public int GetBooksBorrowedCount(string memberNumber)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT COUNT(*) 
                        FROM Borrowings b
                        INNER JOIN Members m ON b.MemberId = m.MemberId
                        WHERE m.MemberNumber = @MemberNumber AND b.Status = 'Borrowed'";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MemberNumber", memberNumber);
                        object result = command.ExecuteScalar();
                        return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting books borrowed count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the total unpaid fines amount for a member using MemberNumber
        /// </summary>
        public decimal GetUnpaidFinesAmount(string memberNumber)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT COALESCE(SUM(f.Amount), 0) 
                        FROM Fines f
                        INNER JOIN Members m ON f.MemberId = m.MemberId
                        WHERE m.MemberNumber = @MemberNumber AND f.Status = 'Unpaid'";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MemberNumber", memberNumber);
                        object result = command.ExecuteScalar();
                        return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0.00m;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting unpaid fines amount: {ex.Message}");
                return 0.00m;
            }
        }

        /// <summary>
        /// Gets books borrowed count and unpaid fines for all members in a single query (optimized)
        /// Returns a dictionary with MemberNumber as key and tuple (BooksBorrowed, UnpaidFines) as value
        /// </summary>
        public Dictionary<string, (int BooksBorrowed, decimal UnpaidFines)> GetAllMembersStatistics()
        {
            var statistics = new Dictionary<string, (int, decimal)>();
            
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Get all members' books borrowed count
                    string booksQuery = @"
                        SELECT 
                            m.MemberNumber,
                            COUNT(b.BorrowingId) AS BooksBorrowed
                        FROM Members m
                        LEFT JOIN Borrowings b ON m.MemberId = b.MemberId AND b.Status = 'Borrowed'
                        GROUP BY m.MemberNumber";
                    
                    using (var command = new MySqlCommand(booksQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string memberNumber = reader.GetString("MemberNumber");
                            int booksBorrowed = reader.GetInt32("BooksBorrowed");
                            statistics[memberNumber] = (booksBorrowed, 0.00m);
                        }
                    }
                    
                    // Get all members' unpaid fines
                    string finesQuery = @"
                        SELECT 
                            m.MemberNumber,
                            COALESCE(SUM(f.Amount), 0) AS UnpaidFines
                        FROM Members m
                        LEFT JOIN Fines f ON m.MemberId = f.MemberId AND f.Status = 'Unpaid'
                        GROUP BY m.MemberNumber";
                    
                    using (var command = new MySqlCommand(finesQuery, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string memberNumber = reader.GetString("MemberNumber");
                            decimal unpaidFines = reader.GetDecimal("UnpaidFines");
                            
                            if (statistics.ContainsKey(memberNumber))
                            {
                                statistics[memberNumber] = (statistics[memberNumber].Item1, unpaidFines);
                            }
                            else
                            {
                                statistics[memberNumber] = (0, unpaidFines);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting all members statistics: {ex.Message}");
            }
            
            return statistics;
        }

        private int GetBookLimit(string memberType)
        {
            switch (memberType)
            {
                case "Student": return 5;
                case "Faculty": return 10;
                case "Staff": return 7;
                case "Guest": return 2;
                default: return 3;
            }
        }
    }

    /// <summary>
    /// Data class for member information
    /// </summary>
    public class MemberData
    {
        public int MemberId { get; set; }
        public string MemberNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; }
        public string MemberType { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Department { get; set; }
        public int Status { get; set; } // 1 = Active, 2 = Suspended, 3 = Expired
        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case 0: return "Inactive";
                    case 1: return "Active";
                    case 2: return "Suspended";
                    case 3: return "Expired";
                    default: return "Unknown";
                }
            }
        }
        public DateTime RegistrationDate { get; set; }
    }

    /// <summary>
    /// Data class for borrowing information
    /// </summary>
    public class BorrowingData
    {
        public int BorrowingId { get; set; }
        public string BookTitle { get; set; }
        public string Author { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } // Borrowed, Returned, Overdue
        public decimal FineAmount { get; set; }
    }

    /// <summary>
    /// Data class for member statistics
    /// </summary>
    public class MemberStatistics
    {
        public int CurrentBooksCount { get; set; }
        public int MaxBooks { get; set; }
        public int TotalBorrowed { get; set; }
        public decimal UnpaidFines { get; set; }
        public DateTime? MembershipExpiry { get; set; }
    }
}
