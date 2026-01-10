using System;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;
using LMS_Library_Management_System.Helper;
using LMS_Library_Management_System.Interfaces;
using LMS_Library_Management_System.Models;

namespace LMS_Library_Management_System.Service
{
    public class AuthenticationService : IAuthenticationService
    {
        public User Authenticate(string email, string password, UserRole expectedRole)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            try
            {
                // First, connect to MySQL server (without specifying database)
                string baseConnectionString = MYSqlHelper.GetBaseConnectionString();
                if (string.IsNullOrEmpty(baseConnectionString))
                {
                    throw new Exception("Database connection string is null or empty");
                }

                using (var connection = new MySqlConnection(baseConnectionString))
                {
                    connection.Open();

                    // Initialize database if needed
                    InitializeDatabaseIfNeeded(connection);
                }

                // Now connect to the specific database for authentication
                string fullConnectionString = MYSqlHelper.GetConnectionString();
                using (var connection = new MySqlConnection(fullConnectionString))
                {
                    connection.Open();
                    
                    string hashedPassword = HashPassword(password);
                    
                    using (var command = new MySqlCommand("sp_AuthenticateUser", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_Email", email.Trim().ToLower());
                        command.Parameters.AddWithValue("p_PasswordHash", hashedPassword);
                        command.Parameters.AddWithValue("p_ExpectedRole", (int)expectedRole);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Check if required columns exist and are not null
                                if (reader["Role"] == DBNull.Value ||
                                    reader["PasswordHash"] == DBNull.Value ||
                                    reader["UserId"] == DBNull.Value ||
                                    reader["Email"] == DBNull.Value ||
                                    reader["FirstName"] == DBNull.Value ||
                                    reader["LastName"] == DBNull.Value ||
                                    reader["IsActive"] == DBNull.Value)
                                {
                                    throw new Exception("Database schema error: Required user columns are missing or null");
                                }

                                int roleValue = Convert.ToInt32(reader["Role"]);
                                UserRole userRole = (UserRole)roleValue;

                                User user = CreateUserFromRole(userRole);

                                user.UserId = Convert.ToInt32(reader["UserId"]);
                                user.Email = reader["Email"].ToString();
                                user.PasswordHash = reader["PasswordHash"].ToString();
                                user.FirstName = reader["FirstName"].ToString();
                                user.LastName = reader["LastName"].ToString();
                                user.IsActive = Convert.ToBoolean(reader["IsActive"]);
                                user.CreatedDate = reader["CreatedDate"] != DBNull.Value 
                                    ? Convert.ToDateTime(reader["CreatedDate"]) 
                                    : DateTime.Now;

                                if (user is Member member)
                                {
                                    GetMemberDetails(member, connection);
                                }

                                return user;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Authentication error: {ex.Message}", ex);
            }

            return null;
        }

        private void GetMemberDetails(Member member, MySqlConnection connection)
        {
            try
            {
                using (var command = new MySqlCommand("sp_GetMemberDetails", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_UserId", member.UserId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            member.MemberId = reader["MemberId"] != DBNull.Value 
                                ? Convert.ToInt32(reader["MemberId"]) 
                                : 0;
                            member.MemberNumber = reader["MemberNumber"] != DBNull.Value 
                                ? reader["MemberNumber"].ToString() 
                                : string.Empty;
                        }
                    }
                }
            }
            catch
            {
                // Member details not found - not critical for authentication
            }
        }

        private void InitializeDatabaseIfNeeded(MySqlConnection connection)
        {
            try
            {
                // First, ensure the database exists
                CreateDatabaseIfNeeded(connection);

                // Check if Users table exists
                string checkTableQuery = @"
                    SELECT COUNT(*)
                    FROM information_schema.tables
                    WHERE table_schema = DATABASE()
                    AND table_name = 'Users'";

                using (var command = new MySqlCommand(checkTableQuery, connection))
                {
                    int tableCount = Convert.ToInt32(command.ExecuteScalar());

                    if (tableCount == 0)
                    {
                        // Create database tables
                        CreateDatabaseTables(connection);
                        // Insert default admin user
                        InsertDefaultAdminUser(connection);
                        // Insert default staff user
                        InsertDefaultStaffUser(connection);
                    }
                    else
                    {
                        // Check if table schema needs updating
                        UpdateTableSchemaIfNeeded(connection);
                        // Check if admin user exists
                        EnsureAdminUserExists(connection);
                        // Check if staff user exists
                        EnsureStaffUserExists(connection);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - allow authentication to continue
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            }
        }

        private void UpdateTableSchemaIfNeeded(MySqlConnection connection)
        {
            try
            {
                // Check and add CreatedDate column if needed
                CheckAndAddColumn(connection, "Users", "CreatedDate", "DATETIME DEFAULT CURRENT_TIMESTAMP", 
                    "UPDATE Users SET CreatedDate = NOW() WHERE CreatedDate IS NULL");

                // Check and add Department column if needed
                CheckAndAddColumn(connection, "Users", "Department", "VARCHAR(100) NULL", null);

                // Check and add LastLogin column if needed
                CheckAndAddColumn(connection, "Users", "LastLogin", "DATETIME NULL", null);

                // Check and add Phone column if needed
                CheckAndAddColumn(connection, "Users", "Phone", "VARCHAR(20) NULL", null);

                // Always update stored procedures to ensure they include Department and LastLogin
                UpdateStoredProcedures(connection);
                
                // Ensure sp_UpdateUser exists with Department and Phone parameters
                UpdateUpdateUserProcedure(connection);
                
                // Ensure sp_CreateUser exists with Department and Phone parameters
                UpdateCreateUserProcedure(connection);
                
                // Create/update sp_UpdateLastLogin procedure
                UpdateLastLoginProcedure(connection);
                
                // Create/update sp_UpdateUserPassword procedure
                UpdateUserPasswordProcedure(connection);
                
                // Create/update sp_UpdateUserRole procedure
                UpdateUserRoleProcedure(connection);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Schema update error: {ex.Message}");
            }
        }

        private bool CheckAndAddColumn(MySqlConnection connection, string tableName, string columnName, string columnDefinition, string updateQuery)
        {
            try
            {
                string checkColumnQuery = @"
                    SELECT COUNT(*)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                    AND table_name = @tableName
                    AND column_name = @columnName";

                using (var command = new MySqlCommand(checkColumnQuery, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);
                    command.Parameters.AddWithValue("@columnName", columnName);
                    int columnCount = Convert.ToInt32(command.ExecuteScalar());

                    if (columnCount == 0)
                    {
                        // Add column to existing table
                        // Try to add after LastLogin if it exists, otherwise just add at the end
                        string addColumnQuery;
                        try
                        {
                            // Check if LastLogin column exists
                            string checkLastLoginQuery = @"
                                SELECT COUNT(*)
                                FROM information_schema.columns
                                WHERE table_schema = DATABASE()
                                AND table_name = @tableName
                                AND column_name = 'LastLogin'";
                            
                            using (var checkCmd = new MySqlCommand(checkLastLoginQuery, connection))
                            {
                                checkCmd.Parameters.AddWithValue("@tableName", tableName);
                                int lastLoginExists = Convert.ToInt32(checkCmd.ExecuteScalar());
                                
                                if (lastLoginExists > 0 && columnName == "Phone")
                                {
                                    addColumnQuery = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition} AFTER LastLogin";
                                }
                                else
                                {
                                    addColumnQuery = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}";
                                }
                            }
                        }
                        catch
                        {
                            // Fallback to simple ADD COLUMN
                            addColumnQuery = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}";
                        }
                        
                        using (var alterCommand = new MySqlCommand(addColumnQuery, connection))
                        {
                            alterCommand.ExecuteNonQuery();
                            System.Diagnostics.Debug.WriteLine($"Successfully added {columnName} column to {tableName} table");
                        }

                        // Update existing records if update query is provided
                        if (!string.IsNullOrEmpty(updateQuery))
                        {
                            using (var updateCommand = new MySqlCommand(updateQuery, connection))
                            {
                                updateCommand.ExecuteNonQuery();
                            }
                        }

                        System.Diagnostics.Debug.WriteLine($"Added {columnName} column to existing {tableName} table");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking/adding column {columnName}: {ex.Message}");
            }
            return false;
        }

        private void UpdateStoredProcedures(MySqlConnection connection)
        {
            try
            {
                // Update sp_GetUsersByRole to include Department, LastLogin, and Phone
                string updateSpGetUsersByRole = @"
                    DROP PROCEDURE IF EXISTS sp_GetUsersByRole;
                    CREATE PROCEDURE sp_GetUsersByRole(IN p_Role INT)
                    BEGIN
                        SELECT 
                            u.UserId,
                            u.Email,
                            u.FirstName,
                            u.LastName,
                            CONCAT(u.FirstName, ' ', u.LastName) AS FullName,
                            u.Role,
                            u.IsActive,
                            u.CreatedDate,
                            u.Department,
                            u.Phone,
                            CASE 
                                WHEN u.LastLogin IS NULL THEN NULL
                                ELSE DATE_FORMAT(u.LastLogin, '%Y-%m-%d %H:%i:%s')
                            END AS LastLogin
                        FROM Users u
                        WHERE u.Role = p_Role
                        ORDER BY u.CreatedDate DESC;
                    END";

                using (var command = new MySqlCommand(updateSpGetUsersByRole, connection))
                {
                    command.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("Updated sp_GetUsersByRole stored procedure");
                }

                // Update sp_GetUserById to include Department, LastLogin, and Phone
                string updateSpGetUserById = @"
                    DROP PROCEDURE IF EXISTS sp_GetUserById;
                    CREATE PROCEDURE sp_GetUserById(IN p_UserId INT)
                    BEGIN
                        SELECT 
                            u.UserId,
                            u.Email,
                            u.FirstName,
                            u.LastName,
                            CONCAT(u.FirstName, ' ', u.LastName) AS FullName,
                            u.Role,
                            u.IsActive,
                            u.CreatedDate,
                            u.Department,
                            u.Phone,
                            CASE 
                                WHEN u.LastLogin IS NULL THEN NULL
                                ELSE DATE_FORMAT(u.LastLogin, '%Y-%m-%d %H:%i:%s')
                            END AS LastLogin
                        FROM Users u
                        WHERE u.UserId = p_UserId;
                    END";

                using (var command = new MySqlCommand(updateSpGetUserById, connection))
                {
                    command.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("Updated sp_GetUserById stored procedure");
                }

                // Update sp_GetUserByEmail to include Department, LastLogin, and Phone
                string updateSpGetUserByEmail = @"
                    DROP PROCEDURE IF EXISTS sp_GetUserByEmail;
                    CREATE PROCEDURE sp_GetUserByEmail(IN p_Email VARCHAR(255))
                    BEGIN
                        SELECT 
                            u.UserId,
                            u.Email,
                            u.FirstName,
                            u.LastName,
                            CONCAT(u.FirstName, ' ', u.LastName) AS FullName,
                            u.Role,
                            u.IsActive,
                            u.CreatedDate,
                            u.Department,
                            u.Phone,
                            CASE 
                                WHEN u.LastLogin IS NULL THEN NULL
                                ELSE DATE_FORMAT(u.LastLogin, '%Y-%m-%d %H:%i:%s')
                            END AS LastLogin
                        FROM Users u
                        WHERE LOWER(u.Email) = LOWER(p_Email);
                    END";

                using (var command = new MySqlCommand(updateSpGetUserByEmail, connection))
                {
                    command.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("Updated sp_GetUserByEmail stored procedure");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating stored procedures: {ex.Message}");
            }
        }

        private void UpdateUpdateUserProcedure(MySqlConnection connection)
        {
            try
            {
                // Create or update sp_UpdateUser stored procedure
                string updateSpUpdateUser = @"
                    DROP PROCEDURE IF EXISTS sp_UpdateUser;
                    CREATE PROCEDURE sp_UpdateUser(
                        IN p_UserId INT,
                        IN p_Email VARCHAR(255),
                        IN p_FirstName VARCHAR(100),
                        IN p_LastName VARCHAR(100),
                        IN p_Department VARCHAR(100),
                        IN p_Phone VARCHAR(20)
                    )
                    BEGIN
                        UPDATE Users
                        SET Email = LOWER(p_Email),
                            FirstName = p_FirstName,
                            LastName = p_LastName,
                            Department = NULLIF(p_Department, ''),
                            Phone = NULLIF(p_Phone, '')
                        WHERE UserId = p_UserId;
                        
                        SELECT ROW_COUNT() AS RowsAffected;
                    END";

                using (var command = new MySqlCommand(updateSpUpdateUser, connection))
                {
                    command.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("Created/Updated sp_UpdateUser stored procedure");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating/updating sp_UpdateUser: {ex.Message}");
            }
        }

        private void UpdateCreateUserProcedure(MySqlConnection connection)
        {
            try
            {
                // Create or update sp_CreateUser stored procedure
                string updateSpCreateUser = @"
                    DROP PROCEDURE IF EXISTS sp_CreateUser;
                    CREATE PROCEDURE sp_CreateUser(
                        IN p_Email VARCHAR(255),
                        IN p_PasswordHash VARCHAR(255),
                        IN p_FirstName VARCHAR(100),
                        IN p_LastName VARCHAR(100),
                        IN p_Role INT,
                        IN p_Department VARCHAR(100),
                        IN p_Phone VARCHAR(20)
                    )
                    BEGIN
                        INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role, Department, Phone)
                        VALUES (LOWER(p_Email), p_PasswordHash, p_FirstName, p_LastName, p_Role, NULLIF(p_Department, ''), NULLIF(p_Phone, ''));
                        
                        SELECT LAST_INSERT_ID() AS UserId;
                    END";

                using (var command = new MySqlCommand(updateSpCreateUser, connection))
                {
                    command.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("Created/Updated sp_CreateUser stored procedure");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating/updating sp_CreateUser: {ex.Message}");
            }
        }

        private void UpdateLastLoginProcedure(MySqlConnection connection)
        {
            try
            {
                // Create or update sp_UpdateLastLogin stored procedure
                string updateSpUpdateLastLogin = @"
                    DROP PROCEDURE IF EXISTS sp_UpdateLastLogin;
                    CREATE PROCEDURE sp_UpdateLastLogin(IN p_Email VARCHAR(255))
                    BEGIN
                        UPDATE Users
                        SET LastLogin = NOW()
                        WHERE LOWER(Email) = LOWER(p_Email);
                        
                        SELECT ROW_COUNT() AS RowsAffected;
                    END";

                using (var command = new MySqlCommand(updateSpUpdateLastLogin, connection))
                {
                    command.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("Updated sp_UpdateLastLogin stored procedure");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating sp_UpdateLastLogin procedure: {ex.Message}");
            }
        }

        private void UpdateUserPasswordProcedure(MySqlConnection connection)
        {
            try
            {
                // Create or update sp_UpdateUserPassword stored procedure
                string updateSpUpdateUserPassword = @"
                    DROP PROCEDURE IF EXISTS sp_UpdateUserPassword;
                    CREATE PROCEDURE sp_UpdateUserPassword(
                        IN p_Email VARCHAR(255),
                        IN p_PasswordHash VARCHAR(255)
                    )
                    BEGIN
                        UPDATE Users
                        SET PasswordHash = p_PasswordHash
                        WHERE LOWER(Email) = LOWER(p_Email);
                        
                        SELECT ROW_COUNT() AS RowsAffected;
                    END";

                using (var command = new MySqlCommand(updateSpUpdateUserPassword, connection))
                {
                    command.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("Created/Updated sp_UpdateUserPassword stored procedure");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating/updating sp_UpdateUserPassword: {ex.Message}");
            }
        }

        private void UpdateUserRoleProcedure(MySqlConnection connection)
        {
            try
            {
                // Create or update sp_UpdateUserRole stored procedure
                string updateSpUpdateUserRole = @"
                    DROP PROCEDURE IF EXISTS sp_UpdateUserRole;
                    CREATE PROCEDURE sp_UpdateUserRole(
                        IN p_UserId INT,
                        IN p_Role INT
                    )
                    BEGIN
                        UPDATE Users
                        SET Role = p_Role
                        WHERE UserId = p_UserId;
                        
                        SELECT ROW_COUNT() AS RowsAffected;
                    END";

                using (var command = new MySqlCommand(updateSpUpdateUserRole, connection))
                {
                    command.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("Created/Updated sp_UpdateUserRole stored procedure");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating/updating sp_UpdateUserRole: {ex.Message}");
            }
        }

        private void CreateDatabaseIfNeeded(MySqlConnection connection)
        {
            try
            {
                // Create database if it doesn't exist
                string createDbQuery = "CREATE DATABASE IF NOT EXISTS LMS_DB";
                using (var command = new MySqlCommand(createDbQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Switch to the database
                string useDbQuery = "USE LMS_DB";
                using (var command = new MySqlCommand(useDbQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database creation error: {ex.Message}");
            }
        }

        private void CreateDatabaseTables(MySqlConnection connection)
        {
            string[] createQueries = new string[]
            {
                @"CREATE TABLE Users (
                    UserId INT PRIMARY KEY AUTO_INCREMENT,
                    Email VARCHAR(255) UNIQUE NOT NULL,
                    PasswordHash VARCHAR(255) NOT NULL,
                    FirstName VARCHAR(100) NOT NULL,
                    LastName VARCHAR(100) NOT NULL,
                    Role INT NOT NULL,
                    IsActive BOOLEAN DEFAULT TRUE,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    Department VARCHAR(100) NULL,
                    LastLogin DATETIME NULL,
                    Phone VARCHAR(20) NULL
                )",

                @"CREATE TABLE Members (
                    MemberId INT PRIMARY KEY AUTO_INCREMENT,
                    UserId INT NOT NULL,
                    MemberNumber VARCHAR(20) UNIQUE NOT NULL,
                    MemberType VARCHAR(50) NOT NULL,
                    Status INT DEFAULT 1,
                    RegistrationDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    Phone VARCHAR(20),
                    Address TEXT,
                    IdNumber VARCHAR(20),
                    DateOfBirth DATE,
                    Gender VARCHAR(10),
                    Department VARCHAR(100),
                    EmergencyContactName VARCHAR(100),
                    EmergencyContactPhone VARCHAR(20),
                    MembershipExpiryDate DATE,
                    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
                )",

                @"CREATE TABLE Books (
                    BookId INT PRIMARY KEY AUTO_INCREMENT,
                    ISBN VARCHAR(20) UNIQUE,
                    Title VARCHAR(255) NOT NULL,
                    Author VARCHAR(255) NOT NULL,
                    Publisher VARCHAR(255),
                    PublicationYear INT,
                    Category VARCHAR(100),
                    TotalCopies INT DEFAULT 1,
                    AvailableCopies INT DEFAULT 1,
                    Description TEXT,
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
                )",

                @"CREATE TABLE Borrowings (
                    BorrowingId INT PRIMARY KEY AUTO_INCREMENT,
                    MemberId INT NOT NULL,
                    BookId INT NOT NULL,
                    BorrowDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    DueDate DATETIME NOT NULL,
                    ReturnDate DATETIME NULL,
                    Status VARCHAR(20) DEFAULT 'Borrowed',
                    FineAmount DECIMAL(10,2) DEFAULT 0,
                    FOREIGN KEY (MemberId) REFERENCES Members(MemberId),
                    FOREIGN KEY (BookId) REFERENCES Books(BookId)
                )",

                @"CREATE TABLE Fines (
                    FineId INT PRIMARY KEY AUTO_INCREMENT,
                    BorrowingId INT NOT NULL,
                    MemberId INT NOT NULL,
                    Amount DECIMAL(10,2) NOT NULL,
                    Reason VARCHAR(255),
                    Status VARCHAR(20) DEFAULT 'Unpaid',
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                    PaidDate DATETIME NULL,
                    FOREIGN KEY (BorrowingId) REFERENCES Borrowings(BorrowingId),
                    FOREIGN KEY (MemberId) REFERENCES Members(MemberId)
                )"
            };

            foreach (string query in createQueries)
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void InsertDefaultAdminUser(MySqlConnection connection)
        {
            // Create default admin user
            string hashedPassword = HashPassword("Admin123!");
            
            try
            {
                using (var command = new MySqlCommand("sp_CreateUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Email", "admin@library.com");
                    command.Parameters.AddWithValue("p_PasswordHash", hashedPassword);
                    command.Parameters.AddWithValue("p_FirstName", "Admin");
                    command.Parameters.AddWithValue("p_LastName", "User");
                    command.Parameters.AddWithValue("p_Role", 1); // Administrator
                    command.Parameters.AddWithValue("p_Department", (object)DBNull.Value); // No department for default admin

                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                // If stored procedure doesn't support Department parameter, try without it
                using (var command = new MySqlCommand("sp_CreateUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Email", "admin@library.com");
                    command.Parameters.AddWithValue("p_PasswordHash", hashedPassword);
                    command.Parameters.AddWithValue("p_FirstName", "Admin");
                    command.Parameters.AddWithValue("p_LastName", "User");
                    command.Parameters.AddWithValue("p_Role", 1); // Administrator

                    command.ExecuteNonQuery();
                }
            }
        }

        private void EnsureAdminUserExists(MySqlConnection connection)
        {
            // Check if admin user exists
            using (var command = new MySqlCommand("sp_GetUserCountByEmail", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("p_Email", "admin@library.com");
                command.Parameters.AddWithValue("p_Role", 1); // Administrator
                
                using (var reader = command.ExecuteReader())
                {
                    int adminCount = 0;
                    if (reader.Read())
                    {
                        adminCount = Convert.ToInt32(reader["UserCount"]);
                    }

                    if (adminCount == 0)
                    {
                        // Insert default admin user if not exists
                        InsertDefaultAdminUser(connection);
                    }
                }
            }
        }

        private void InsertDefaultStaffUser(MySqlConnection connection)
        {
            // Create default staff user
            string hashedPassword = HashPassword("staff123!"); // Fixed: lowercase 's' as per user requirement
            
            try
            {
                using (var command = new MySqlCommand("sp_CreateUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Email", "staff@library.com");
                    command.Parameters.AddWithValue("p_PasswordHash", hashedPassword);
                    command.Parameters.AddWithValue("p_FirstName", "Library");
                    command.Parameters.AddWithValue("p_LastName", "Staff");
                    command.Parameters.AddWithValue("p_Role", 2); // Staff
                    command.Parameters.AddWithValue("p_Department", (object)DBNull.Value); // No department for default staff

                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                // If stored procedure doesn't support Department parameter, try without it
                using (var command = new MySqlCommand("sp_CreateUser", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_Email", "staff@library.com");
                    command.Parameters.AddWithValue("p_PasswordHash", hashedPassword);
                    command.Parameters.AddWithValue("p_FirstName", "Library");
                    command.Parameters.AddWithValue("p_LastName", "Staff");
                    command.Parameters.AddWithValue("p_Role", 2); // Staff

                    command.ExecuteNonQuery();
                }
            }
        }

        private void EnsureStaffUserExists(MySqlConnection connection)
        {
            // Check if staff user exists
            using (var command = new MySqlCommand("sp_GetUserCountByEmail", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("p_Email", "staff@library.com");
                command.Parameters.AddWithValue("p_Role", 2); // Staff
                
                using (var reader = command.ExecuteReader())
                {
                    int staffCount = 0;
                    if (reader.Read())
                    {
                        staffCount = Convert.ToInt32(reader["UserCount"]);
                    }

                    if (staffCount == 0)
                    {
                        // Insert default staff user if not exists
                        InsertDefaultStaffUser(connection);
                    }
                }
            }
        }

        private User CreateUserFromRole(UserRole role)
        {
            switch (role)
            {
                case UserRole.Administrator:
                    return new Librarian();
                case UserRole.Staff:
                    return new LibraryStaff();
                case UserRole.Member:
                    return new Member();
                default:
                    throw new ArgumentException("Invalid user role");
            }
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            {
                return false;
            }

            string hashedPassword = HashPassword(password);
            return hashedPassword == passwordHash;
        }

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty");
            }

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

        public bool UserExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    
                    using (var command = new MySqlCommand("sp_CheckUserExists", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_Email", email.Trim().ToLower());
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int count = Convert.ToInt32(reader["UserCount"]);
                                return count > 0;
                            }
                        }
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}

