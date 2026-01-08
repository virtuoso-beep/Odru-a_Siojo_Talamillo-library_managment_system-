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
                // Check if Users table has CreatedDate column
                string checkColumnQuery = @"
                    SELECT COUNT(*)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                    AND table_name = 'Users'
                    AND column_name = 'CreatedDate'";

                using (var command = new MySqlCommand(checkColumnQuery, connection))
                {
                    int columnCount = Convert.ToInt32(command.ExecuteScalar());

                    if (columnCount == 0)
                    {
                        // Add CreatedDate column to existing Users table
                        string addColumnQuery = "ALTER TABLE Users ADD COLUMN CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP";
                        using (var alterCommand = new MySqlCommand(addColumnQuery, connection))
                        {
                            alterCommand.ExecuteNonQuery();
                        }

                        // Update existing records to have a CreatedDate
                        string updateExistingQuery = "UPDATE Users SET CreatedDate = NOW() WHERE CreatedDate IS NULL";
                        using (var updateCommand = new MySqlCommand(updateExistingQuery, connection))
                        {
                            updateCommand.ExecuteNonQuery();
                        }

                        System.Diagnostics.Debug.WriteLine("Added CreatedDate column to existing Users table");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Schema update error: {ex.Message}");
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
                    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
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
            string hashedPassword = HashPassword("Staff123!");
            
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

