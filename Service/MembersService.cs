using System;
using System.Collections.Generic;
using System.Data;
using Library_Management_System.Helper;
using Library_Management_System.Interfaces;
using MySql.Data.MySqlClient;
using Library_Management_System.Service;
namespace Library_Management_System.Service
{
    public class MembersService : IMembersService
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
            public string IdNumber { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public string Gender { get; set; }
            public string Department { get; set; }
            public string EmergencyContactName { get; set; }
            public string EmergencyContactPhone { get; set; }
            public DateTime? MembershipExpiryDate { get; set; }
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
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_GetMemberStatistics", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                stats.TotalMembers = Convert.ToInt32(reader["TotalMembers"]);
                                stats.ActiveMembers = Convert.ToInt32(reader["ActiveMembers"]);
                                stats.SuspendedMembers = Convert.ToInt32(reader["SuspendedMembers"]);
                                stats.ExpiredMembers = Convert.ToInt32(reader["ExpiredMembers"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading member statistics: {ex.Message}");
            }
            return stats;
        }
        public bool IsEmailRegistered(string email)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var cmd = StoredProcedureHelper.CreateCommand("SP_CheckEmailRegistered", connection))
                    {
                        StoredProcedureHelper.AddParameter(cmd, "p_Email", email.ToLower().Trim());
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return Convert.ToInt32(reader["EmailCount"]) > 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking email: {ex.Message}");
                return false;
            }
            return false;
        }
        public bool IsIdNumberRegistered(string idNumber, string excludeMemberId = null)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var cmd = StoredProcedureHelper.CreateCommand("SP_CheckIdNumberRegistered", connection))
                    {
                        StoredProcedureHelper.AddParameter(cmd, "p_IdNumber", idNumber.Trim());
                        StoredProcedureHelper.AddParameter(cmd, "p_ExcludeMemberId", 
                            excludeMemberId != null ? (object)int.Parse(excludeMemberId) : DBNull.Value);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return Convert.ToInt32(reader["IdCount"]) > 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking ID number: {ex.Message}");
                return false;
            }
            return false;
        }
        public List<MemberInfo> GetMembers(string searchText = "", string statusFilter = "All Status", string typeFilter = "All Types")
        {
            var members = new List<MemberInfo>();
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_GetAllMembers", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText);
                        StoredProcedureHelper.AddParameter(command, "p_StatusFilter", statusFilter);
                        StoredProcedureHelper.AddParameter(command, "p_TypeFilter", typeFilter);
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
                                    Fines = Convert.ToDecimal(reader["Fines"]),
                                    Phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : "",
                                    Address = reader["Address"] != DBNull.Value ? reader["Address"].ToString() : "",
                                    IdNumber = reader["IdNumber"] != DBNull.Value ? reader["IdNumber"].ToString() : "",
                                    DateOfBirth = reader["DateOfBirth"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DateOfBirth"]) : null,
                                    Gender = reader["Gender"] != DBNull.Value ? reader["Gender"].ToString() : "",
                                    Department = reader["Department"] != DBNull.Value ? reader["Department"].ToString() : "",
                                    EmergencyContactName = reader["EmergencyContactName"] != DBNull.Value ? reader["EmergencyContactName"].ToString() : "",
                                    EmergencyContactPhone = reader["EmergencyContactPhone"] != DBNull.Value ? reader["EmergencyContactPhone"].ToString() : "",
                                    MembershipExpiryDate = reader["MembershipExpiryDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["MembershipExpiryDate"]) : null,
                                    RegistrationDate = reader["RegistrationDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["RegistrationDate"]) : null
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
        public string DiagnoseDatabase()
        {
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("=== Database Diagnostics ===");
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    diagnostics.AppendLine("✓ Database connection successful");
                    string[] requiredTables = { "Users", "Members" };
                    foreach (string table in requiredTables)
                    {
                        string checkQuery = $"SHOW TABLES LIKE '{table}'";
                        using (var cmd = new MySqlCommand(checkQuery, connection))
                        {
                            var result = cmd.ExecuteScalar();
                            if (result != null)
                                diagnostics.AppendLine($"✓ Table '{table}' exists");
                            else
                                diagnostics.AppendLine($"✗ Table '{table}' missing");
                        }
                    }
                    string userColumnsQuery = @"
                        SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Users'
                        ORDER BY ORDINAL_POSITION";
                    using (var cmd = new MySqlCommand(userColumnsQuery, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        diagnostics.AppendLine("\nUsers table columns:");
                        while (reader.Read())
                        {
                            string colName = reader["COLUMN_NAME"].ToString();
                            string dataType = reader["DATA_TYPE"].ToString();
                            string nullable = reader["IS_NULLABLE"].ToString();
                            diagnostics.AppendLine($"  - {colName} ({dataType}) Nullable: {nullable}");
                        }
                    }
                    string memberColumnsQuery = @"
                        SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Members'
                        ORDER BY ORDINAL_POSITION";
                    using (var cmd = new MySqlCommand(memberColumnsQuery, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        diagnostics.AppendLine("\nMembers table columns:");
                        while (reader.Read())
                        {
                            string colName = reader["COLUMN_NAME"].ToString();
                            string dataType = reader["DATA_TYPE"].ToString();
                            string nullable = reader["IS_NULLABLE"].ToString();
                            diagnostics.AppendLine($"  - {colName} ({dataType}) Nullable: {nullable}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                diagnostics.AppendLine($"✗ Database error: {ex.Message}");
                diagnostics.AppendLine($"Stack trace: {ex.StackTrace}");
            }
            return diagnostics.ToString();
        }
        public bool RegisterMember(string firstName, string lastName, string email, string phone, string address, string memberType, string status = "Active",
            string idNumber = "", DateTime? dateOfBirth = null, string gender = "", string department = "",
            string emergencyContactName = "", string emergencyContactPhone = "", DateTime? membershipExpiryDate = null)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    System.Diagnostics.Debug.WriteLine($"Registering member: {firstName} {lastName} ({email})");
                    string defaultPassword = GenerateSecureDefaultPassword();
                    string passwordHash = GenerateHash(defaultPassword);
                    using (var command = StoredProcedureHelper.CreateCommand("SP_RegisterMember", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_FirstName", firstName);
                        StoredProcedureHelper.AddParameter(command, "p_LastName", lastName);
                        StoredProcedureHelper.AddParameter(command, "p_Email", email.ToLower().Trim());
                        StoredProcedureHelper.AddParameter(command, "p_PasswordHash", passwordHash);
                        StoredProcedureHelper.AddParameter(command, "p_MemberType", memberType);
                        StoredProcedureHelper.AddParameter(command, "p_Status", status);
                        StoredProcedureHelper.AddParameter(command, "p_Phone", string.IsNullOrWhiteSpace(phone) ? null : phone.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_Address", string.IsNullOrWhiteSpace(address) ? null : address.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_IdNumber", string.IsNullOrWhiteSpace(idNumber) ? null : idNumber.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_DateOfBirth", dateOfBirth);
                        StoredProcedureHelper.AddParameter(command, "p_Gender", string.IsNullOrWhiteSpace(gender) ? null : gender.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_Department", string.IsNullOrWhiteSpace(department) ? null : department.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_EmergencyContactName", string.IsNullOrWhiteSpace(emergencyContactName) ? null : emergencyContactName.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_EmergencyContactPhone", string.IsNullOrWhiteSpace(emergencyContactPhone) ? null : emergencyContactPhone.Trim());
                        StoredProcedureHelper.AddParameter(command, "p_MembershipExpiryDate", membershipExpiryDate);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string memberNumber = reader["MemberNumber"].ToString();
                                int userId = Convert.ToInt32(reader["UserId"]);
                                System.Diagnostics.Debug.WriteLine($"Member created: {memberNumber}, User ID: {userId}");
                            }
                        }
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
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_GetMemberByNumber", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_MemberNumber", memberId);
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
                                    ExpirationDate = reader["MembershipExpiryDate"] != DBNull.Value ? Convert.ToDateTime(reader["MembershipExpiryDate"]) : (DateTime?)null,
                                    TotalBorrowed = reader["TotalBorrowed"] != DBNull.Value ? Convert.ToInt32(reader["TotalBorrowed"]) : 0,
                                    IdNumber = reader["IdNumber"] != DBNull.Value ? reader["IdNumber"].ToString() : "",
                                    DateOfBirth = reader["DateOfBirth"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DateOfBirth"]) : null,
                                    Gender = reader["Gender"] != DBNull.Value ? reader["Gender"].ToString() : "",
                                    Department = reader["Department"] != DBNull.Value ? reader["Department"].ToString() : "",
                                    EmergencyContactName = reader["EmergencyContactName"] != DBNull.Value ? reader["EmergencyContactName"].ToString() : "",
                                    EmergencyContactPhone = reader["EmergencyContactPhone"] != DBNull.Value ? reader["EmergencyContactPhone"].ToString() : "",
                                    MembershipExpiryDate = reader["MembershipExpiryDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["MembershipExpiryDate"]) : null
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
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
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
                    string updateUserQuery = @"
                        UPDATE Users 
                        SET FirstName = @firstName, LastName = @lastName, Email = @email
                        WHERE UserId = @userId";
                    using (var userCmd = new MySqlCommand(updateUserQuery, connection))
                    {
                        userCmd.Parameters.AddWithValue("@firstName", firstName);
                        userCmd.Parameters.AddWithValue("@lastName", lastName);
                        userCmd.Parameters.AddWithValue("@email", email.ToLower().Trim());
                        userCmd.Parameters.AddWithValue("@userId", userId);
                        int rowsAffected = userCmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            throw new Exception("User record not found. Cannot update member.");
                        }
                    }
                    int? memberTypeValue = null;
                    if (memberType == "Student") memberTypeValue = 1;
                    else if (memberType == "Faculty") memberTypeValue = 2;
                    else if (memberType == "Staff") memberTypeValue = 3;
                    int statusValue = 1;
                    if (status == "Inactive") statusValue = 2;
                    else if (status == "Suspended") statusValue = 3;
                    else if (status == "Expired") statusValue = 4;
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
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_DeleteMember", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_MemberNumber", memberId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int rowsAffected = Convert.ToInt32(reader["RowsAffected"]);
                                return rowsAffected > 0;
                            }
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting member: {ex.Message}");
                throw;
            }
        }
        private string GenerateSecureDefaultPassword()
        {
            Random random = new Random();
            int randomNumber = random.Next(1000, 9999);
            return $"Member{randomNumber}!";
        }
        private string GenerateHash(string password)
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
}
