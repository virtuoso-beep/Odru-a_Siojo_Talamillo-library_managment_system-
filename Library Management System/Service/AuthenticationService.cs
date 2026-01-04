using System;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;
using Library_Management_System.Helper;
using Library_Management_System.Interfaces;
using Library_Management_System.Models;
namespace Library_Management_System.Service
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
                string baseConnectionString = MYSqlHelper.GetBaseConnectionString();
                if (string.IsNullOrEmpty(baseConnectionString))
                {
                    throw new Exception("Database connection string is null or empty");
                }
                using (var connection = new MySqlConnection(baseConnectionString))
                {
                    connection.Open();
                    InitializeDatabaseIfNeeded(connection);
                }
                string fullConnectionString = MYSqlHelper.GetConnectionString();
                using (var connection = new MySqlConnection(fullConnectionString))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_AuthenticateUser", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_Email", email.Trim().ToLower());
                        StoredProcedureHelper.AddParameter(command, "p_Role", (int)expectedRole);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
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
                                string storedPasswordHash = reader["PasswordHash"].ToString();
                                if (!VerifyPassword(password, storedPasswordHash))
                                {
                                    return null;
                                }
                                int roleValue = Convert.ToInt32(reader["Role"]);
                                UserRole userRole = (UserRole)roleValue;
                                User user = CreateUserFromRole(userRole);
                                user.UserId = Convert.ToInt32(reader["UserId"]);
                                user.Email = reader["Email"].ToString();
                                user.PasswordHash = storedPasswordHash;
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
                using (var command = StoredProcedureHelper.CreateCommand("SP_GetMemberDetails", connection))
                {
                    StoredProcedureHelper.AddParameter(command, "p_UserId", member.UserId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            member.MemberId = Convert.ToInt32(reader["MemberId"]);
                            member.MemberNumber = reader["MemberNumber"].ToString();
                        }
                    }
                }
            }
            catch
            {
            }
        }
        private void InitializeDatabaseIfNeeded(MySqlConnection connection)
        {
            try
            {
                CreateDatabaseIfNeeded(connection);
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
                        CreateDatabaseTables(connection);
                        InsertDefaultAdminUser(connection);
                    }
                    else
                    {
                        UpdateTableSchemaIfNeeded(connection);
                        EnsureAdminUserExists(connection);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            }
        }
        private void UpdateTableSchemaIfNeeded(MySqlConnection connection)
        {
            try
            {
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
                        string addColumnQuery = "ALTER TABLE Users ADD COLUMN CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP";
                        using (var alterCommand = new MySqlCommand(addColumnQuery, connection))
                        {
                            alterCommand.ExecuteNonQuery();
                        }
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
                string createDbQuery = "CREATE DATABASE IF NOT EXISTS LibraryManagementDB";
                using (var command = new MySqlCommand(createDbQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
                string useDbQuery = "USE LibraryManagementDB";
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
            string hashedPassword = HashPassword("Admin123!");
            using (var command = StoredProcedureHelper.CreateCommand("SP_CreateUser", connection))
            {
                StoredProcedureHelper.AddParameter(command, "p_Email", "admin@umindanao.edu.ph");
                StoredProcedureHelper.AddParameter(command, "p_PasswordHash", hashedPassword);
                StoredProcedureHelper.AddParameter(command, "p_FirstName", "Admin");
                StoredProcedureHelper.AddParameter(command, "p_LastName", "User");
                StoredProcedureHelper.AddParameter(command, "p_Role", 1);
                StoredProcedureHelper.AddParameter(command, "p_IsActive", true);
                command.ExecuteNonQuery();
            }
        }
        private void EnsureAdminUserExists(MySqlConnection connection)
        {
            string checkAdminQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email AND Role = 1";
            using (var command = new MySqlCommand(checkAdminQuery, connection))
            {
                command.Parameters.AddWithValue("@Email", "admin@umindanao.edu.ph");
                int adminCount = Convert.ToInt32(command.ExecuteScalar());
                if (adminCount == 0)
                {
                    InsertDefaultAdminUser(connection);
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
            return string.Equals(hashedPassword, passwordHash, StringComparison.OrdinalIgnoreCase);
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
        public static class PasswordValidator
        {
            public static (bool IsValid, string ErrorMessage) ValidatePassword(string password)
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    return (false, "Password cannot be empty.");
                }
                if (password.Length < 8)
                {
                    return (false, "Password must be at least 8 characters long.");
                }
                if (password.Length > 128)
                {
                    return (false, "Password cannot exceed 128 characters.");
                }
                bool hasUpperCase = password.Any(char.IsUpper);
                bool hasLowerCase = password.Any(char.IsLower);
                bool hasDigit = password.Any(char.IsDigit);
                bool hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));
                if (!hasUpperCase)
                {
                    return (false, "Password must contain at least one uppercase letter.");
                }
                if (!hasLowerCase)
                {
                    return (false, "Password must contain at least one lowercase letter.");
                }
                if (!hasDigit)
                {
                    return (false, "Password must contain at least one number.");
                }
                if (!hasSpecialChar)
                {
                    return (false, "Password must contain at least one special character (!@#$%^&* etc.).");
                }
                string[] weakPasswords = { "password", "123456", "admin123", "password123", "admin", "user", "guest" };
                if (weakPasswords.Any(weak => password.ToLower().Contains(weak)))
                {
                    return (false, "Password contains common weak patterns. Please choose a stronger password.");
                }
                return (true, string.Empty);
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
                    using (var command = StoredProcedureHelper.CreateCommand("SP_CheckUserExists", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_Email", email.Trim().ToLower());
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int count = Convert.ToInt32(reader["UserCount"]);
                                return count > 0;
                            }
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
        }
        private string GenerateHash(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
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
