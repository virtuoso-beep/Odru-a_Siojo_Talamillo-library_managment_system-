using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;
using Library_Management_System.Helpers;
using Library_Management_System.Interface;
using Library_Management_System.Models;

namespace Library_Management_System.Services
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
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    
                    string query = @"
                        SELECT UserId, Email, PasswordHash, FirstName, LastName, Role, IsActive, CreatedDate
                        FROM Users
                        WHERE Email = @Email AND IsActive = 1";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email.Trim().ToLower());

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int roleValue = Convert.ToInt32(reader["Role"]);
                                UserRole userRole = (UserRole)roleValue;

                                if (userRole != expectedRole)
                                {
                                    return null;
                                }

                                string storedPasswordHash = reader["PasswordHash"].ToString();

                                if (!VerifyPassword(password, storedPasswordHash))
                                {
                                    return null;
                                }

                                User user = CreateUserFromRole(userRole);
                                
                                user.UserId = Convert.ToInt32(reader["UserId"]);
                                user.Email = reader["Email"].ToString();
                                user.PasswordHash = storedPasswordHash;
                                user.FirstName = reader["FirstName"].ToString();
                                user.LastName = reader["LastName"].ToString();
                                user.IsActive = Convert.ToBoolean(reader["IsActive"]);
                                user.CreatedDate = Convert.ToDateTime(reader["CreatedDate"]);

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
                string query = @"
                    SELECT MemberId, MemberNumber
                    FROM Members
                    WHERE UserId = @UserId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", member.UserId);

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
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    
                    string query = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                    
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Email", email.Trim().ToLower());
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        return count > 0;
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

