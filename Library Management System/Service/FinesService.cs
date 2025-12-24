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
            public string BorrowingId { get; set; }
            public string MemberId { get; set; }
            public string MemberName { get; set; }
            public string BookTitle { get; set; }
            public decimal Amount { get; set; }
            public string Reason { get; set; }
            public string Status { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime? PaidDate { get; set; }
        }

        public List<FineInfo> GetFines(string searchText = "", string statusFilter = "All Status")
        {
            var fines = new List<FineInfo>();

            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    string query = @"
                        SELECT f.FineId, f.BorrowingId, f.MemberId, f.Amount, f.Reason, f.Status, f.CreatedDate, f.PaidDate,
                               CONCAT(u.FirstName, ' ', u.LastName) as MemberName,
                               COALESCE(bk.Title, '') as BookTitle
                        FROM Fines f
                        INNER JOIN Members m ON f.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        LEFT JOIN Borrowings b ON f.BorrowingId = b.BorrowingId
                        LEFT JOIN Books bk ON b.BookId = bk.BookId
                        WHERE 1=1";

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        query += " AND (CONCAT(u.FirstName, ' ', u.LastName) LIKE @search OR COALESCE(bk.Title, '') LIKE @search OR f.Reason LIKE @search)";
                    }

                    if (statusFilter != "All Status")
                    {
                        query += " AND f.Status = @status";
                    }

                    query += " ORDER BY f.CreatedDate DESC";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(searchText))
                        {
                            command.Parameters.AddWithValue("@search", $"%{searchText}%");
                        }
                        if (statusFilter != "All Status")
                        {
                            command.Parameters.AddWithValue("@status", statusFilter);
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                fines.Add(new FineInfo
                                {
                                    FineId = reader["FineId"].ToString(),
                                    BorrowingId = reader["BorrowingId"] != DBNull.Value ? reader["BorrowingId"].ToString() : null,
                                    MemberId = reader["MemberId"].ToString(),
                                    MemberName = reader["MemberName"].ToString(),
                                    BookTitle = reader["BookTitle"] != DBNull.Value ? reader["BookTitle"].ToString() : "",
                                    Amount = Convert.ToDecimal(reader["Amount"]),
                                    Reason = reader["Reason"]?.ToString(),
                                    Status = reader["Status"].ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                    PaidDate = reader["PaidDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["PaidDate"]) : null
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

                    string query = @"
                        UPDATE Fines
                        SET Status = 'Paid', PaidDate = NOW()
                        WHERE FineId = @fineId AND Status = 'Unpaid'";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@fineId", fineId);
                        int rowsAffected = command.ExecuteNonQuery();

                        return rowsAffected > 0;
                    }
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

                    string query = @"
                        UPDATE Fines
                        SET Status = 'Waived', PaidDate = NOW(), Reason = CONCAT(IFNULL(Reason, ''), ' [WAIVED: ', @reason, ']')
                        WHERE FineId = @fineId AND Status = 'Unpaid'";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@fineId", fineId);
                        command.Parameters.AddWithValue("@reason", reason);
                        int rowsAffected = command.ExecuteNonQuery();

                        return rowsAffected > 0;
                    }
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

                    string query = "SELECT SUM(Amount) FROM Fines WHERE 1=1";

                    if (!string.IsNullOrEmpty(memberId))
                    {
                        query += " AND MemberId = @memberId";
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        query += " AND Status = @status";
                    }

                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(memberId))
                        {
                            command.Parameters.AddWithValue("@memberId", memberId);
                        }
                        if (!string.IsNullOrEmpty(status))
                        {
                            command.Parameters.AddWithValue("@status", status);
                        }

                        var result = command.ExecuteScalar();
                        return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting total fines: {ex.Message}");
                return 0;
            }
        }

        public FineInfo GetFineById(string fineId)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    string query = @"
                        SELECT f.FineId, f.BorrowingId, f.MemberId, f.Amount, f.Reason, f.Status, f.CreatedDate, f.PaidDate,
                               CONCAT(u.FirstName, ' ', u.LastName) as MemberName,
                               COALESCE(bk.Title, '') as BookTitle
                        FROM Fines f
                        INNER JOIN Members m ON f.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        LEFT JOIN Borrowings b ON f.BorrowingId = b.BorrowingId
                        LEFT JOIN Books bk ON b.BookId = bk.BookId
                        WHERE f.FineId = @fineId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@fineId", fineId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new FineInfo
                                {
                                    FineId = reader["FineId"].ToString(),
                                    BorrowingId = reader["BorrowingId"] != DBNull.Value ? reader["BorrowingId"].ToString() : null,
                                    MemberId = reader["MemberId"].ToString(),
                                    MemberName = reader["MemberName"].ToString(),
                                    BookTitle = reader["BookTitle"] != DBNull.Value ? reader["BookTitle"].ToString() : "",
                                    Amount = Convert.ToDecimal(reader["Amount"]),
                                    Reason = reader["Reason"]?.ToString(),
                                    Status = reader["Status"].ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                                    PaidDate = reader["PaidDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["PaidDate"]) : null
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

        // Calculate fines for overdue books (can be called periodically)
        public void CalculateOverdueFines()
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    // Find overdue borrowings without fines
                    string query = @"
                        SELECT b.BorrowingId, b.MemberId, b.BookId, b.DueDate
                        FROM Borrowings b
                        WHERE b.ReturnDate IS NULL
                        AND b.DueDate < CURDATE()
                        AND NOT EXISTS (
                            SELECT 1 FROM Fines f
                            WHERE f.BorrowingId = b.BorrowingId
                            AND f.Status = 'Unpaid'
                        )";

                    var overdueBorrowings = new List<(string borrowingId, string memberId, DateTime dueDate)>();

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                overdueBorrowings.Add((
                                    reader["BorrowingId"].ToString(),
                                    reader["MemberId"].ToString(),
                                    Convert.ToDateTime(reader["DueDate"])
                                ));
                            }
                        }
                    }

                    // Create fines for overdue borrowings
                    foreach (var (borrowingId, memberId, dueDate) in overdueBorrowings)
                    {
                        TimeSpan overdue = DateTime.Now - dueDate;
                        int overdueDays = (int)System.Math.Ceiling(overdue.TotalDays);
                        decimal fineAmount = overdueDays * 5.00m; // $5 per day

                        string insertFineQuery = @"
                            INSERT INTO Fines (BorrowingId, MemberId, Amount, Reason, Status)
                            VALUES (@borrowingId, @memberId, @amount, 'Overdue return', 'Unpaid')";

                        using (var fineCmd = new MySqlCommand(insertFineQuery, connection))
                        {
                            fineCmd.Parameters.AddWithValue("@borrowingId", borrowingId);
                            fineCmd.Parameters.AddWithValue("@memberId", memberId);
                            fineCmd.Parameters.AddWithValue("@amount", fineAmount);
                            fineCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating overdue fines: {ex.Message}");
            }
        }

        // Alias method for backward compatibility
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

                    // Build reason string - include book title and notes if provided
                    string fullReason = reason;
                    if (!string.IsNullOrWhiteSpace(bookTitle))
                    {
                        fullReason = $"{reason} - {bookTitle}";
                    }
                    if (!string.IsNullOrWhiteSpace(notes))
                    {
                        fullReason += string.IsNullOrWhiteSpace(bookTitle) ? $" - {notes}" : $" ({notes})";
                    }

                    string query = @"
                        INSERT INTO Fines (BorrowingId, MemberId, Amount, Reason, Status, CreatedDate)
                        VALUES (NULL, @memberId, @amount, @reason, 'Unpaid', NOW())";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@memberId", memberId);
                        command.Parameters.AddWithValue("@amount", amount);
                        command.Parameters.AddWithValue("@reason", fullReason);
                        int rowsAffected = command.ExecuteNonQuery();

                        return rowsAffected > 0;
                    }
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