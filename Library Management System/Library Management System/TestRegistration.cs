using System;
using System.Configuration;
using MySql.Data.MySqlClient;
using Library_Management_System.Service;
using Library_Management_System.Models;

class TestRegistration
{
    static void Main()
    {
        Console.WriteLine("Testing member registration...");

        string connectionString = ConfigurationManager.ConnectionStrings["MySQLConnection"]?.ConnectionString;

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("ERROR: Connection string not found");
            return;
        }

        try
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("✓ Database connection successful");

                // Check if email exists
                string testEmail = "test.user@umindanao.edu.ph";
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE LOWER(Email) = LOWER(@email)";
                using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@email", testEmail);
                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                    Console.WriteLine($"Email '{testEmail}' exists: {count > 0}");
                }

                // Generate password hash
                string password = "TestPass123!";
                string passwordHash = GenerateHash(password);
                Console.WriteLine($"Generated password hash for: {password}");

                // Insert user
                string insertUserQuery = @"
                    INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role, IsActive)
                    VALUES (@email, @passwordHash, @firstName, @lastName, 3, TRUE)";

                int userId;
                using (MySqlCommand userCmd = new MySqlCommand(insertUserQuery, connection))
                {
                    userCmd.Parameters.AddWithValue("@email", testEmail);
                    userCmd.Parameters.AddWithValue("@passwordHash", passwordHash);
                    userCmd.Parameters.AddWithValue("@firstName", "Test");
                    userCmd.Parameters.AddWithValue("@lastName", "User");
                    userCmd.ExecuteNonQuery();
                    userId = (int)userCmd.LastInsertedId;
                    Console.WriteLine($"✓ User inserted with ID: {userId}");
                }

                // Insert member
                string memberNumber = $"MEM-{DateTime.Now:yyyy}-{userId:D4}";
                string insertMemberQuery = @"
                    INSERT INTO Members (UserId, MemberNumber, MemberType, Status, RegistrationDate,
                                       Phone, Address, IdNumber, DateOfBirth, Gender, Department)
                    VALUES (@userId, @memberNumber, @memberType, @status, NOW(),
                           @phone, @address, @idNumber, @dateOfBirth, @gender, @department)";

                using (MySqlCommand memberCmd = new MySqlCommand(insertMemberQuery, connection))
                {
                    memberCmd.Parameters.AddWithValue("@userId", userId);
                    memberCmd.Parameters.AddWithValue("@memberNumber", memberNumber);
                    memberCmd.Parameters.AddWithValue("@memberType", 1); // Student
                    memberCmd.Parameters.AddWithValue("@status", 1); // Active
                    memberCmd.Parameters.AddWithValue("@phone", "09123456789");
                    memberCmd.Parameters.AddWithValue("@address", "Test Address");
                    memberCmd.Parameters.AddWithValue("@idNumber", "123456789");
                    memberCmd.Parameters.AddWithValue("@dateOfBirth", new DateTime(2000, 1, 1));
                    memberCmd.Parameters.AddWithValue("@gender", "Male");
                    memberCmd.Parameters.AddWithValue("@department", "Computer Science");

                    memberCmd.ExecuteNonQuery();
                    Console.WriteLine($"✓ Member inserted with number: {memberNumber}");
                }

                Console.WriteLine("✓ Registration test completed successfully!");

                // Test authentication with the created user
                Console.WriteLine("\n--- Testing Authentication ---");
                var authService = new AuthenticationService();
                try
                {
                    User authenticatedUser = authService.Authenticate("admin@umindanao.edu.ph", "Admin123!", UserRole.Administrator);
                    if (authenticatedUser != null)
                    {
                        Console.WriteLine($"✓ Authentication successful! Welcome {authenticatedUser.FullName}");
                        Console.WriteLine($"  Role: {authenticatedUser.GetRoleDescription()}");
                        Console.WriteLine($"  Email: {authenticatedUser.Email}");
                    }
                    else
                    {
                        Console.WriteLine("✗ Authentication failed - invalid credentials");
                    }
                }
                catch (Exception authEx)
                {
                    Console.WriteLine($"✗ Authentication test failed: {authEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Registration test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static string GenerateHash(string password)
    {
        using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
