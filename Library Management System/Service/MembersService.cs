using System;
using System.Collections.Generic;
using System.Data;
using Library_Management_System.Helper;
using MySql.Data.MySqlClient;

namespace Library_Management_System.Services
{
    public class MembersService
    {
        public class MemberInfo
        {
            public string MemberId { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string Email { get; set; }
            public string Status { get; set; }
            public int BooksBorrowed { get; set; }
            public int BooksLimit { get; set; }
            public decimal Fines { get; set; }
            public string Phone { get; set; }
            public string Address { get; set; }
            public DateTime? RegistrationDate { get; set; }
            public DateTime? ExpirationDate { get; set; }
            public int TotalBorrowed { get; set; }
        }

        public class MemberStatistics
        {
            public int TotalMembers { get; set; }
            public int ActiveMembers { get; set; }
            public int SuspendedMembers { get; set; }
            public int ExpiredMembers { get; set; }
        }

        public MemberStatistics GetMemberStatistics()
        {
            var stats = new MemberStatistics();

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Total Members
                    string totalQuery = "SELECT COUNT(*) FROM Members";
                    using (var command = new MySqlCommand(totalQuery, connection))
                    {
                        stats.TotalMembers = Convert.ToInt32(command.ExecuteScalar());
                    }

                    // Active Members
                    string activeQuery = @"
                        SELECT COUNT(*) 
                        FROM Members 
                        WHERE Status = 1 AND (ExpirationDate IS NULL OR ExpirationDate > NOW())";
                    using (var command = new MySqlCommand(activeQuery, connection))
                    {
                        stats.ActiveMembers = Convert.ToInt32(command.ExecuteScalar());
                    }

                    // Suspended Members
                    string suspendedQuery = "SELECT COUNT(*) FROM Members WHERE Status = 3";
                    using (var command = new MySqlCommand(suspendedQuery, connection))
                    {
                        stats.SuspendedMembers = Convert.ToInt32(command.ExecuteScalar());
                    }

                    // Expired Members
                    string expiredQuery = @"
                        SELECT COUNT(*) 
                        FROM Members 
                        WHERE Status = 4 OR (ExpirationDate IS NOT NULL AND ExpirationDate < NOW())";
                    using (var command = new MySqlCommand(expiredQuery, connection))
                    {
                        stats.ExpiredMembers = Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading member statistics: {ex.Message}");
            }

            return stats;
        }

        public List<MemberInfo> GetMembers(string searchText = "", string statusFilter = "All Status", string typeFilter = "All Types")
        {
            var members = new List<MemberInfo>();

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Check if Borrowings and Fines tables exist
                    bool borrowingsTableExists = false;
                    bool finesTableExists = false;

                    string checkBorrowingsQuery = @"
                        SELECT COUNT(*) 
                        FROM information_schema.tables 
                        WHERE table_schema = DATABASE() 
                        AND table_name = 'Borrowings'";
                    using (var checkCmd = new MySqlCommand(checkBorrowingsQuery, connection))
                    {
                        borrowingsTableExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }

                    string checkFinesQuery = @"
                        SELECT COUNT(*) 
                        FROM information_schema.tables 
                        WHERE table_schema = DATABASE() 
                        AND table_name = 'Fines'";
                    using (var checkCmd = new MySqlCommand(checkFinesQuery, connection))
                    {
                        finesTableExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }

                    string booksBorrowedSubquery = borrowingsTableExists 
                        ? @"COALESCE((
                            SELECT COUNT(*) 
                            FROM Borrowings b 
                            WHERE b.MemberId = m.MemberId AND b.ReturnDate IS NULL
                        ), 0)"
                        : "0";

                    string finesSubquery = finesTableExists
                        ? @"COALESCE((
                            SELECT SUM(f.Amount) 
                            FROM Fines f 
                            WHERE f.MemberId = m.MemberId AND (f.Status = 'Pending' OR f.Status = 'Unpaid')
                        ), 0)"
                        : "0";

                    string query = $@"
                        SELECT 
                            m.MemberNumber,
                            u.FirstName,
                            u.LastName,
                            CASE 
                                WHEN m.MemberType = 1 THEN 'Student'
                                WHEN m.MemberType = 2 THEN 'Faculty'
                                WHEN m.MemberType = 3 THEN 'Staff'
                                WHEN m.MemberType IS NULL THEN 'Guest'
                                ELSE 'Guest'
                            END AS MemberType,
                            u.Email,
                            CASE 
                                WHEN m.Status = 1 AND (m.ExpirationDate IS NULL OR m.ExpirationDate > NOW()) THEN 'Active'
                                WHEN m.Status = 2 THEN 'Inactive'
                                WHEN m.Status = 3 THEN 'Suspended'
                                WHEN m.Status = 4 OR (m.ExpirationDate IS NOT NULL AND m.ExpirationDate < NOW()) THEN 'Expired'
                                ELSE 'Unknown'
                            END AS Status,
                            {booksBorrowedSubquery} AS BooksBorrowed,
                            5 AS BooksLimit,
                            {finesSubquery} AS Fines
                        FROM Members m
                        INNER JOIN Users u ON m.UserId = u.UserId
                        WHERE 1=1";

                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        query += " AND (u.FirstName LIKE @search OR u.LastName LIKE @search OR u.Email LIKE @search OR m.MemberNumber LIKE @search)";
                    }

                    if (statusFilter != "All Status")
                    {
                        if (statusFilter == "Active")
                        {
                            query += " AND m.Status = 1 AND (m.ExpirationDate IS NULL OR m.ExpirationDate > NOW())";
                        }
                        else if (statusFilter == "Suspended")
                        {
                            query += " AND m.Status = 3";
                        }
                        else if (statusFilter == "Expired")
                        {
                            query += " AND (m.Status = 4 OR (m.ExpirationDate IS NOT NULL AND m.ExpirationDate < NOW()))";
                        }
                        else if (statusFilter == "Inactive")
                        {
                            query += " AND m.Status = 2";
                        }
                    }

                    if (typeFilter != "All Types")
                    {
                        query += " AND (m.MemberType = @typeValue OR (m.MemberType IS NULL AND @typeValue = 0))";
                    }

                    query += " ORDER BY u.LastName, u.FirstName";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(searchText))
                        {
                            command.Parameters.AddWithValue("@search", $"%{searchText}%");
                        }

                        if (typeFilter != "All Types")
                        {
                            int typeValue = 0;
                            if (typeFilter == "Student") typeValue = 1;
                            else if (typeFilter == "Faculty") typeValue = 2;
                            else if (typeFilter == "Staff") typeValue = 3;
                            command.Parameters.AddWithValue("@typeValue", typeValue);
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                members.Add(new MemberInfo
                                {
                                    MemberId = reader["MemberNumber"].ToString(),
                                    Name = $"{reader["FirstName"]} {reader["LastName"]}",
                                    Type = reader["MemberType"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    Status = reader["Status"].ToString(),
                                    BooksBorrowed = Convert.ToInt32(reader["BooksBorrowed"]),
                                    BooksLimit = Convert.ToInt32(reader["BooksLimit"]),
                                    Fines = Convert.ToDecimal(reader["Fines"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading members: {ex.Message}");
            }

            return members;
        }

        public bool RegisterMember(string firstName, string lastName, string email, string phone, string address, string memberType)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Check if email already exists (case-insensitive)
                    string normalizedEmail = email.ToLower().Trim();
                    string checkEmailQuery = "SELECT COUNT(*) FROM Users WHERE LOWER(Email) = LOWER(@email)";
                    using (var checkCmd = new MySqlCommand(checkEmailQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@email", normalizedEmail);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            throw new Exception("Email already exists in the system.");
                        }
                    }

                    // Generate a default password (can be changed later)
                    string defaultPassword = "Member123!";
                    string passwordHash = Helpers.PasswordHashGenerator.GenerateHash(defaultPassword);

                    // Insert into Users table - ensure email is lowercase
                    string insertUserQuery = @"
                        INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role, IsActive)
                        VALUES (@email, @passwordHash, @firstName, @lastName, 3, TRUE)";
                    
                    int userId;
                    using (var userCmd = new MySqlCommand(insertUserQuery, connection))
                    {
                        userCmd.Parameters.AddWithValue("@email", email.ToLower().Trim()); // Ensure lowercase
                        userCmd.Parameters.AddWithValue("@passwordHash", passwordHash);
                        userCmd.Parameters.AddWithValue("@firstName", firstName);
                        userCmd.Parameters.AddWithValue("@lastName", lastName);
                        userCmd.ExecuteNonQuery();
                        userId = (int)userCmd.LastInsertedId;
                    }

                    // Generate member number
                    string memberNumber = $"MEM-{DateTime.Now:yyyy}-{userId:D4}";

                    // Map member type
                    int memberTypeValue = 0;
                    if (memberType == "Student") memberTypeValue = 1;
                    else if (memberType == "Faculty") memberTypeValue = 2;
                    else if (memberType == "Staff") memberTypeValue = 3;

                    // Insert into Members table
                    // Check if Phone and Address columns exist
                    bool hasPhoneColumn = false;
                    bool hasAddressColumn = false;
                    
                    string checkColumnsQuery = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE table_schema = DATABASE() 
                        AND table_name = 'Members' 
                        AND column_name = 'Phone'";
                    using (var checkCmd = new MySqlCommand(checkColumnsQuery, connection))
                    {
                        hasPhoneColumn = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }
                    
                    string checkAddressQuery = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE table_schema = DATABASE() 
                        AND table_name = 'Members' 
                        AND column_name = 'Address'";
                    using (var checkCmd = new MySqlCommand(checkAddressQuery, connection))
                    {
                        hasAddressColumn = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }
                    
                    string insertMemberQuery;
                    if (hasPhoneColumn && hasAddressColumn)
                    {
                        insertMemberQuery = @"
                            INSERT INTO Members (UserId, MemberNumber, MemberType, Status, RegistrationDate, Phone, Address)
                            VALUES (@userId, @memberNumber, @memberType, 1, NOW(), @phone, @address)";
                    }
                    else
                    {
                        insertMemberQuery = @"
                            INSERT INTO Members (UserId, MemberNumber, MemberType, Status, RegistrationDate)
                            VALUES (@userId, @memberNumber, @memberType, 1, NOW())";
                    }
                    
                    using (var memberCmd = new MySqlCommand(insertMemberQuery, connection))
                    {
                        memberCmd.Parameters.AddWithValue("@userId", userId);
                        memberCmd.Parameters.AddWithValue("@memberNumber", memberNumber);
                        if (memberTypeValue == 0)
                        {
                            memberCmd.Parameters.AddWithValue("@memberType", DBNull.Value);
                        }
                        else
                        {
                            memberCmd.Parameters.AddWithValue("@memberType", memberTypeValue);
                        }
                        
                        if (hasPhoneColumn && hasAddressColumn)
                        {
                            memberCmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone.Trim());
                            memberCmd.Parameters.AddWithValue("@address", string.IsNullOrWhiteSpace(address) ? (object)DBNull.Value : address.Trim());
                        }
                        
                        memberCmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering member: {ex.Message}");
                throw;
            }
        }

        public MemberInfo GetMemberByMemberId(string memberId)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Check if Phone and Address columns exist
                    bool hasPhoneColumn = false;
                    bool hasAddressColumn = false;
                    
                    string checkPhoneQuery = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE table_schema = DATABASE() 
                        AND table_name = 'Members' 
                        AND column_name = 'Phone'";
                    using (var checkCmd = new MySqlCommand(checkPhoneQuery, connection))
                    {
                        hasPhoneColumn = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }
                    
                    string checkAddressQuery = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE table_schema = DATABASE() 
                        AND table_name = 'Members' 
                        AND column_name = 'Address'";
                    using (var checkCmd = new MySqlCommand(checkAddressQuery, connection))
                    {
                        hasAddressColumn = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }
                    
                    string phoneColumn = hasPhoneColumn ? "m.Phone" : "NULL AS Phone";
                    string addressColumn = hasAddressColumn ? "m.Address" : "NULL AS Address";
                    
                    // Check if Borrowings table exists for total borrowed count
                    bool borrowingsTableExists = false;
                    string checkBorrowingsQuery = @"
                        SELECT COUNT(*) 
                        FROM information_schema.tables 
                        WHERE table_schema = DATABASE() 
                        AND table_name = 'Borrowings'";
                    using (var checkCmd = new MySqlCommand(checkBorrowingsQuery, connection))
                    {
                        borrowingsTableExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }
                    
                    // Check if Fines table exists
                    bool finesTableExists = false;
                    string checkFinesQuery = @"
                        SELECT COUNT(*) 
                        FROM information_schema.tables 
                        WHERE table_schema = DATABASE() 
                        AND table_name = 'Fines'";
                    using (var checkCmd = new MySqlCommand(checkFinesQuery, connection))
                    {
                        finesTableExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }
                    
                    string totalBorrowedSubquery = borrowingsTableExists 
                        ? @"COALESCE((
                            SELECT COUNT(*) 
                            FROM Borrowings b 
                            WHERE b.MemberId = m.MemberId
                        ), 0)"
                        : "0";
                    
                    string currentBorrowedSubquery = borrowingsTableExists 
                        ? @"COALESCE((
                            SELECT COUNT(*) 
                            FROM Borrowings b 
                            WHERE b.MemberId = m.MemberId AND b.ReturnDate IS NULL
                        ), 0)"
                        : "0";
                    
                    string finesSubquery = finesTableExists
                        ? @"COALESCE((
                            SELECT SUM(f.Amount) 
                            FROM Fines f 
                            WHERE f.MemberId = m.MemberId AND (f.Status = 'Pending' OR f.Status = 'Unpaid')
                        ), 0)"
                        : "0";
                    
                    string query = $@"
                        SELECT 
                            m.MemberNumber,
                            u.FirstName,
                            u.LastName,
                            CASE 
                                WHEN m.MemberType = 1 THEN 'Student'
                                WHEN m.MemberType = 2 THEN 'Faculty'
                                WHEN m.MemberType = 3 THEN 'Staff'
                                WHEN m.MemberType IS NULL THEN 'Guest'
                                ELSE 'Guest'
                            END AS MemberType,
                            u.Email,
                            CASE 
                                WHEN m.Status = 1 AND (m.ExpirationDate IS NULL OR m.ExpirationDate > NOW()) THEN 'Active'
                                WHEN m.Status = 2 THEN 'Inactive'
                                WHEN m.Status = 3 THEN 'Suspended'
                                WHEN m.Status = 4 OR (m.ExpirationDate IS NOT NULL AND m.ExpirationDate < NOW()) THEN 'Expired'
                                ELSE 'Unknown'
                            END AS Status,
                            {currentBorrowedSubquery} AS BooksBorrowed,
                            5 AS BooksLimit,
                            {finesSubquery} AS Fines,
                            {phoneColumn},
                            {addressColumn},
                            m.RegistrationDate,
                            m.ExpirationDate,
                            {totalBorrowedSubquery} AS TotalBorrowed
                        FROM Members m
                        INNER JOIN Users u ON m.UserId = u.UserId
                        WHERE m.MemberNumber = @memberId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@memberId", memberId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new MemberInfo
                                {
                                    MemberId = reader["MemberNumber"].ToString(),
                                    Name = $"{reader["FirstName"]} {reader["LastName"]}",
                                    Type = reader["MemberType"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    Status = reader["Status"].ToString(),
                                    BooksBorrowed = Convert.ToInt32(reader["BooksBorrowed"]),
                                    BooksLimit = Convert.ToInt32(reader["BooksLimit"]),
                                    Fines = Convert.ToDecimal(reader["Fines"]),
                                    Phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : "",
                                    Address = reader["Address"] != DBNull.Value ? reader["Address"].ToString() : "",
                                    RegistrationDate = reader["RegistrationDate"] != DBNull.Value ? Convert.ToDateTime(reader["RegistrationDate"]) : (DateTime?)null,
                                    ExpirationDate = reader["ExpirationDate"] != DBNull.Value ? Convert.ToDateTime(reader["ExpirationDate"]) : (DateTime?)null,
                                    TotalBorrowed = reader["TotalBorrowed"] != DBNull.Value ? Convert.ToInt32(reader["TotalBorrowed"]) : 0
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting member: {ex.Message}");
                throw;
            }

            return null;
        }

        public bool UpdateMember(string memberId, string firstName, string lastName, string email, string memberType, string status, string phone = "", string address = "")
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Get UserId from MemberNumber first
                    string getUserIdQuery = "SELECT UserId FROM Members WHERE MemberNumber = @memberId";
                    int userId;
                    using (var getUserIdCmd = new MySqlCommand(getUserIdQuery, connection))
                    {
                        getUserIdCmd.Parameters.AddWithValue("@memberId", memberId);
                        object result = getUserIdCmd.ExecuteScalar();
                        if (result == null)
                        {
                            throw new Exception("Member not found.");
                        }
                        userId = Convert.ToInt32(result);
                    }

                    // Check if email already exists for another user (case-insensitive, excluding current user)
                    string normalizedEmail = email.ToLower().Trim();
                    string checkEmailQuery = @"
                        SELECT COUNT(*) 
                        FROM Users 
                        WHERE LOWER(Email) = @email AND UserId != @userId";
                    using (var checkCmd = new MySqlCommand(checkEmailQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@email", normalizedEmail);
                        checkCmd.Parameters.AddWithValue("@userId", userId);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            throw new Exception("Email already exists for another member.");
                        }
                    }

                    // Update Users table - ensure email is lowercase
                    string updateUserQuery = @"
                        UPDATE Users 
                        SET FirstName = @firstName, LastName = @lastName, Email = @email
                        WHERE UserId = @userId";
                    
                    using (var userCmd = new MySqlCommand(updateUserQuery, connection))
                    {
                        userCmd.Parameters.AddWithValue("@firstName", firstName);
                        userCmd.Parameters.AddWithValue("@lastName", lastName);
                        userCmd.Parameters.AddWithValue("@email", email.ToLower().Trim()); // Ensure lowercase
                        userCmd.Parameters.AddWithValue("@userId", userId);
                        int rowsAffected = userCmd.ExecuteNonQuery();
                        
                        if (rowsAffected == 0)
                        {
                            throw new Exception("User record not found. Cannot update member.");
                        }
                    }

                    // Map member type (handle NULL for Guest)
                    int? memberTypeValue = null;
                    if (memberType == "Student") memberTypeValue = 1;
                    else if (memberType == "Faculty") memberTypeValue = 2;
                    else if (memberType == "Staff") memberTypeValue = 3;
                    // Guest = NULL (0 in code, but NULL in database)

                    // Map status
                    int statusValue = 1;
                    if (status == "Inactive") statusValue = 2;
                    else if (status == "Suspended") statusValue = 3;
                    else if (status == "Expired") statusValue = 4;

                    // Check if Phone and Address columns exist
                    bool hasPhoneColumn = false;
                    bool hasAddressColumn = false;
                    
                    string checkColumnsQuery = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE table_schema = DATABASE() 
                        AND table_name = 'Members' 
                        AND column_name = 'Phone'";
                    using (var checkCmd = new MySqlCommand(checkColumnsQuery, connection))
                    {
                        hasPhoneColumn = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }
                    
                    string checkAddressQuery = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE table_schema = DATABASE() 
                        AND table_name = 'Members' 
                        AND column_name = 'Address'";
                    using (var checkCmd = new MySqlCommand(checkAddressQuery, connection))
                    {
                        hasAddressColumn = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }
                    
                    // Update Members table - handle NULL for MemberType
                    string updateMemberQuery;
                    if (hasPhoneColumn && hasAddressColumn)
                    {
                        updateMemberQuery = @"
                            UPDATE Members 
                            SET MemberType = @memberType, Status = @status, Phone = @phone, Address = @address
                            WHERE MemberNumber = @memberId";
                    }
                    else
                    {
                        updateMemberQuery = @"
                            UPDATE Members 
                            SET MemberType = @memberType, Status = @status
                            WHERE MemberNumber = @memberId";
                    }
                    
                    using (var memberCmd = new MySqlCommand(updateMemberQuery, connection))
                    {
                        // Handle NULL for Guest type (MemberType = NULL in database)
                        if (memberType == "Guest")
                        {
                            memberCmd.Parameters.AddWithValue("@memberType", DBNull.Value);
                        }
                        else if (memberTypeValue.HasValue)
                        {
                            memberCmd.Parameters.AddWithValue("@memberType", memberTypeValue.Value);
                        }
                        else
                        {
                            // Default to NULL if not specified
                            memberCmd.Parameters.AddWithValue("@memberType", DBNull.Value);
                        }
                        
                        memberCmd.Parameters.AddWithValue("@status", statusValue);
                        memberCmd.Parameters.AddWithValue("@memberId", memberId);
                        
                        if (hasPhoneColumn && hasAddressColumn)
                        {
                            memberCmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone.Trim());
                            memberCmd.Parameters.AddWithValue("@address", string.IsNullOrWhiteSpace(address) ? (object)DBNull.Value : address.Trim());
                        }
                        
                        int rowsAffected = memberCmd.ExecuteNonQuery();
                        
                        if (rowsAffected == 0)
                        {
                            throw new Exception("Member not found. Please verify the member ID.");
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating member: {ex.Message}");
                throw;
            }
        }

        public bool DeleteMember(string memberId)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Get UserId from MemberNumber
                    string getUserIdQuery = "SELECT UserId FROM Members WHERE MemberNumber = @memberId";
                    int userId;
                    using (var getUserIdCmd = new MySqlCommand(getUserIdQuery, connection))
                    {
                        getUserIdCmd.Parameters.AddWithValue("@memberId", memberId);
                        object result = getUserIdCmd.ExecuteScalar();
                        if (result == null)
                        {
                            throw new Exception("Member not found.");
                        }
                        userId = Convert.ToInt32(result);
                    }

                    // Check if member has active borrowings
                    string checkBorrowingsQuery = @"
                        SELECT COUNT(*) 
                        FROM Borrowings 
                        WHERE MemberId = (SELECT MemberId FROM Members WHERE MemberNumber = @memberId) 
                        AND ReturnDate IS NULL";
                    
                    bool hasActiveBorrowings = false;
                    try
                    {
                        using (var checkCmd = new MySqlCommand(checkBorrowingsQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@memberId", memberId);
                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            hasActiveBorrowings = count > 0;
                        }
                    }
                    catch
                    {
                        // Borrowings table might not exist, ignore
                    }

                    if (hasActiveBorrowings)
                    {
                        throw new Exception("Cannot delete member with active book borrowings. Please return all books first.");
                    }

                    // Delete from Members table
                    // The foreign key constraint has ON DELETE CASCADE, so deleting from Members
                    // will automatically delete the associated User record
                    string deleteMemberQuery = "DELETE FROM Members WHERE MemberNumber = @memberId";
                    using (var memberCmd = new MySqlCommand(deleteMemberQuery, connection))
                    {
                        memberCmd.Parameters.AddWithValue("@memberId", memberId);
                        int rowsAffected = memberCmd.ExecuteNonQuery();
                        
                        if (rowsAffected == 0)
                        {
                            throw new Exception("Member not found.");
                        }
                    }
                    
                    // Note: User record is automatically deleted due to ON DELETE CASCADE
                    // No need to manually delete from Users table

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting member: {ex.Message}");
                throw;
            }
        }
    }
}

