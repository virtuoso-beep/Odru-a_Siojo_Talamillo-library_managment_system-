using System;
using System.Collections.Generic;
using System.Data;
using Library_Management_System.Helper;
using Library_Management_System.Interfaces;
using MySql.Data.MySqlClient;
namespace Library_Management_System.Service
{
    public class FinesService : IFineService
    {
        public class FineInfo
        {
            public string FineId { get; set; }
            public string MemberId { get; set; }
            public string MemberName { get; set; }
            public string MemberNumber { get; set; }
            public decimal Amount { get; set; }
            public string Reason { get; set; }
            public string Status { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime? PaidDate { get; set; }
            public string BorrowingId { get; set; }
            public string BookTitle { get; set; }
        }
        public List<FineInfo> GetFines(string searchText = "", string statusFilter = "All")
        {
            var fines = new List<FineInfo>();
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_GetAllFines", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText);
                        StoredProcedureHelper.AddParameter(command, "p_StatusFilter", statusFilter == "All" ? null : statusFilter);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                fines.Add(new FineInfo
                                {
                                    FineId = reader["FineId"].ToString(),
                                    MemberId = reader["MemberId"].ToString(),
                                    MemberNumber = reader["MemberNumber"].ToString(),
                                    MemberName = reader["MemberName"].ToString(),
                                    Amount = Convert.ToDecimal(reader["Amount"]),
                                    Reason = reader["Reason"].ToString(),
                                    Status = reader["Status"].ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                    PaidDate = reader["PaidDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["PaidDate"]) : null,
                                    BorrowingId = reader["BorrowingId"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting fines: {ex.Message}");
            }
            return fines;
        }
        public bool ProcessFinePayment(string fineId)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_ProcessFinePayment", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_FineId", int.Parse(fineId));
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
                System.Diagnostics.Debug.WriteLine($"Error processing fine payment: {ex.Message}");
                throw;
            }
        }
        public bool WaiveFine(string fineId, string reason)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_WaiveFine", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_FineId", int.Parse(fineId));
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
                System.Diagnostics.Debug.WriteLine($"Error waiving fine: {ex.Message}");
                throw;
            }
        }
        public decimal GetTotalFines(string memberId = null, string status = null)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    if (string.IsNullOrWhiteSpace(memberId))
                    {
                        using (var command = StoredProcedureHelper.CreateCommand("SP_GetAllFines", connection))
                        {
                            StoredProcedureHelper.AddParameter(command, "p_SearchText", null);
                            StoredProcedureHelper.AddParameter(command, "p_StatusFilter", status ?? "Pending");
                            decimal total = 0;
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    total += Convert.ToDecimal(reader["Amount"]);
                                }
                            }
                            return total;
                        }
                    }
                    else
                    {
                        using (var command = StoredProcedureHelper.CreateCommand("SP_GetTotalFines", connection))
                        {
                            StoredProcedureHelper.AddParameter(command, "p_MemberId", int.Parse(memberId));
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    return Convert.ToDecimal(reader["TotalFines"]);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting total fines: {ex.Message}");
            }
            return 0;
        }
        public FineInfo GetFineById(string fineId)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_GetFineById", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_FineId", int.Parse(fineId));
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new FineInfo
                                {
                                    FineId = reader["FineId"].ToString(),
                                    MemberId = reader["MemberId"].ToString(),
                                    MemberNumber = reader["MemberNumber"].ToString(),
                                    MemberName = reader["MemberName"].ToString(),
                                    BorrowingId = reader["BorrowingId"]?.ToString(),
                                    Amount = Convert.ToDecimal(reader["Amount"]),
                                    Reason = reader["Reason"].ToString(),
                                    Status = reader["Status"].ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                    PaidDate = reader["PaidDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["PaidDate"]) : null,
                                    BookTitle = reader["BookTitle"]?.ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting fine: {ex.Message}");
            }
            return null;
        }
        public void CalculateOverdueFines()
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_CalculateOverdueFines", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int finesCreated = Convert.ToInt32(reader["FinesCreated"]);
                                System.Diagnostics.Debug.WriteLine($"Created {finesCreated} overdue fines");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating overdue fines: {ex.Message}");
            }
        }
        public void ProcessOverdueFines()
        {
            CalculateOverdueFines();
        }
        public bool AddFine(string memberId, decimal amount, string reason, string bookTitle = null, string notes = null)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_AddFine", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_MemberId", int.Parse(memberId));
                        StoredProcedureHelper.AddParameter(command, "p_BorrowingId", null);
                        StoredProcedureHelper.AddParameter(command, "p_Amount", amount);
                        StoredProcedureHelper.AddParameter(command, "p_Reason", reason + (bookTitle != null ? $" - Book: {bookTitle}" : "") + (notes != null ? $" - Notes: {notes}" : ""));
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int fineId = Convert.ToInt32(reader["FineId"]);
                                System.Diagnostics.Debug.WriteLine($"Fine created with ID: {fineId}");
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding fine: {ex.Message}");
                throw;
            }
        }
    }
}