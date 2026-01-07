using System;
using System.Collections.Generic;
using System.Data;
using Library_Management_System.Helper;
using MySql.Data.MySqlClient;
namespace Library_Management_System.Service
{
    public class ReservationService
    {
        public class ReservationInfo
        {
            public string ReservationId { get; set; }
            public string MemberId { get; set; }
            public string MemberName { get; set; }
            public string BookId { get; set; }
            public string BookTitle { get; set; }
            public string BookISBN { get; set; }
            public DateTime ReservedDate { get; set; }
            public DateTime ExpiryDate { get; set; }
            public string Status { get; set; }
            public bool IsNotified { get; set; }
            public DateTime? FulfilledDate { get; set; }
        }
        public List<ReservationInfo> GetReservations(string searchText = "", string statusFilter = "All")
        {
            var reservations = new List<ReservationInfo>();
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = @"
                        SELECT r.ReservationId, r.MemberId, r.BookId, r.ReservedDate, r.ExpiryDate, 
                               r.Status, r.IsNotified, r.FulfilledDate,
                               CONCAT(u.FirstName, ' ', u.LastName) as MemberName,
                               bk.Title as BookTitle, bk.ISBN as BookISBN
                        FROM Reservations r
                        INNER JOIN Members m ON r.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        INNER JOIN Books bk ON r.BookId = bk.BookId
                        WHERE 1=1";
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        query += " AND (bk.Title LIKE @search OR CONCAT(u.FirstName, ' ', u.LastName) LIKE @search OR r.ReservationId LIKE @search)";
                    }
                    if (statusFilter != "All")
                    {
                        if (statusFilter == "Pending")
                        {
                            query += " AND r.Status = 'Pending'";
                        }
                        else if (statusFilter == "Ready")
                        {
                            query += " AND r.Status = 'Ready'";
                        }
                        else if (statusFilter == "Fulfilled")
                        {
                            query += " AND r.Status = 'Fulfilled'";
                        }
                    }
                    query += " ORDER BY r.ReservedDate DESC";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(searchText))
                        {
                            command.Parameters.AddWithValue("@search", $"%{searchText}%");
                        }
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                reservations.Add(new ReservationInfo
                                {
                                    ReservationId = reader["ReservationId"].ToString(),
                                    MemberId = reader["MemberId"].ToString(),
                                    MemberName = reader["MemberName"].ToString(),
                                    BookId = reader["BookId"].ToString(),
                                    BookTitle = reader["BookTitle"].ToString(),
                                    BookISBN = reader["BookISBN"]?.ToString(),
                                    ReservedDate = Convert.ToDateTime(reader["ReservedDate"]),
                                    ExpiryDate = Convert.ToDateTime(reader["ExpiryDate"]),
                                    Status = reader["Status"].ToString(),
                                    IsNotified = Convert.ToBoolean(reader["IsNotified"]),
                                    FulfilledDate = reader["FulfilledDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["FulfilledDate"]) : null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting reservations: {ex.Message}");
            }
            return reservations;
        }
        public bool CreateReservation(string memberId, string bookId, int reservationDays = 7)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string memberQuery = "SELECT UserId FROM Members WHERE MemberId = @memberId AND Status = 1";
                    using (var memberCmd = new MySqlCommand(memberQuery, connection))
                    {
                        memberCmd.Parameters.AddWithValue("@memberId", memberId);
                        var result = memberCmd.ExecuteScalar();
                        if (result == null)
                        {
                            throw new Exception("Member not found or inactive.");
                        }
                    }
                    string bookQuery = "SELECT BookId FROM Books WHERE BookId = @bookId";
                    using (var bookCmd = new MySqlCommand(bookQuery, connection))
                    {
                        bookCmd.Parameters.AddWithValue("@bookId", bookId);
                        var result = bookCmd.ExecuteScalar();
                        if (result == null)
                        {
                            throw new Exception("Book not found.");
                        }
                    }
                    string existingQuery = @"
                        SELECT COUNT(*) FROM Reservations
                        WHERE MemberId = @memberId AND BookId = @bookId 
                        AND (Status = 'Pending' OR Status = 'Ready')";
                    using (var existingCmd = new MySqlCommand(existingQuery, connection))
                    {
                        existingCmd.Parameters.AddWithValue("@memberId", memberId);
                        existingCmd.Parameters.AddWithValue("@bookId", bookId);
                        int existingCount = Convert.ToInt32(existingCmd.ExecuteScalar());
                        if (existingCount > 0)
                        {
                            throw new Exception("Member already has an active reservation for this book.");
                        }
                    }
                    string availabilityQuery = "SELECT AvailableCopies FROM Books WHERE BookId = @bookId";
                    int availableCopies = 0;
                    using (var availCmd = new MySqlCommand(availabilityQuery, connection))
                    {
                        availCmd.Parameters.AddWithValue("@bookId", bookId);
                        var result = availCmd.ExecuteScalar();
                        if (result != null)
                        {
                            availableCopies = Convert.ToInt32(result);
                        }
                    }
                    string initialStatus = availableCopies > 0 ? "Ready" : "Pending";
                    DateTime reservedDate = DateTime.Now;
                    DateTime expiryDate = reservedDate.AddDays(reservationDays);
                    string insertQuery = @"
                        INSERT INTO Reservations (MemberId, BookId, ReservedDate, ExpiryDate, Status, IsNotified)
                        VALUES (@memberId, @bookId, @reservedDate, @expiryDate, @status, 0)";
                    using (var insertCmd = new MySqlCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@memberId", memberId);
                        insertCmd.Parameters.AddWithValue("@bookId", bookId);
                        insertCmd.Parameters.AddWithValue("@reservedDate", reservedDate);
                        insertCmd.Parameters.AddWithValue("@expiryDate", expiryDate);
                        insertCmd.Parameters.AddWithValue("@status", initialStatus);
                        insertCmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating reservation: {ex.Message}");
                throw;
            }
        }
        public bool CancelReservation(string reservationId)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string checkQuery = "SELECT Status FROM Reservations WHERE ReservationId = @reservationId";
                    string currentStatus = "";
                    using (var checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@reservationId", reservationId);
                        var result = checkCmd.ExecuteScalar();
                        if (result == null)
                        {
                            throw new Exception("Reservation not found.");
                        }
                        currentStatus = result.ToString();
                    }
                    if (currentStatus == "Fulfilled" || currentStatus == "Cancelled")
                    {
                        throw new Exception($"Cannot cancel a reservation with status '{currentStatus}'.");
                    }
                    string updateQuery = "UPDATE Reservations SET Status = 'Cancelled' WHERE ReservationId = @reservationId";
                    using (var updateCmd = new MySqlCommand(updateQuery, connection))
                    {
                        updateCmd.Parameters.AddWithValue("@reservationId", reservationId);
                        updateCmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cancelling reservation: {ex.Message}");
                throw;
            }
        }
        public ReservationInfo GetReservationById(string reservationId)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = @"
                        SELECT r.ReservationId, r.MemberId, r.BookId, r.ReservedDate, r.ExpiryDate, 
                               r.Status, r.IsNotified, r.FulfilledDate,
                               CONCAT(u.FirstName, ' ', u.LastName) as MemberName,
                               bk.Title as BookTitle, bk.ISBN as BookISBN
                        FROM Reservations r
                        INNER JOIN Members m ON r.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        INNER JOIN Books bk ON r.BookId = bk.BookId
                        WHERE r.ReservationId = @reservationId";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@reservationId", reservationId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ReservationInfo
                                {
                                    ReservationId = reader["ReservationId"].ToString(),
                                    MemberId = reader["MemberId"].ToString(),
                                    MemberName = reader["MemberName"].ToString(),
                                    BookId = reader["BookId"].ToString(),
                                    BookTitle = reader["BookTitle"].ToString(),
                                    BookISBN = reader["BookISBN"]?.ToString(),
                                    ReservedDate = Convert.ToDateTime(reader["ReservedDate"]),
                                    ExpiryDate = Convert.ToDateTime(reader["ExpiryDate"]),
                                    Status = reader["Status"].ToString(),
                                    IsNotified = Convert.ToBoolean(reader["IsNotified"]),
                                    FulfilledDate = reader["FulfilledDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["FulfilledDate"]) : null
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting reservation: {ex.Message}");
            }
            return null;
        }
    }
}
