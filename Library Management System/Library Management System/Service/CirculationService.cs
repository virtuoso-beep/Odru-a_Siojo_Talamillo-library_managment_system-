using System;
using System.Collections.Generic;
using System.Data;
using Library_Management_System.Helper;
using Library_Management_System.Interfaces;
using MySql.Data.MySqlClient;

namespace Library_Management_System.Service
{
    public class CirculationService : ICirculationService
    {
        public class BorrowingInfo
        {
            public string BorrowingId { get; set; }
            public string MemberId { get; set; }
            public string MemberName { get; set; }
            public string BookId { get; set; }
            public string BookTitle { get; set; }
            public string BookISBN { get; set; }
            public DateTime BorrowDate { get; set; }
            public DateTime DueDate { get; set; }
            public DateTime? ReturnDate { get; set; }
            public string Status { get; set; }
            public decimal FineAmount { get; set; }
        }

        public List<BorrowingInfo> GetBorrowings(string searchText = "", string statusFilter = "All Status")
        {
            var borrowings = new List<BorrowingInfo>();

            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    string query = @"
                        SELECT b.BorrowingId, b.MemberId, b.BookId, b.BorrowDate, b.DueDate, b.ReturnDate, b.Status, b.FineAmount,
                               CONCAT(u.FirstName, ' ', u.LastName) as MemberName,
                               bk.Title as BookTitle, bk.ISBN as BookISBN
                        FROM Borrowings b
                        INNER JOIN Members m ON b.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        INNER JOIN Books bk ON b.BookId = bk.BookId
                        WHERE 1=1";

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        query += " AND (bk.Title LIKE @search OR CONCAT(u.FirstName, ' ', u.LastName) LIKE @search OR bk.ISBN LIKE @search)";
                    }

                    if (statusFilter != "All Status")
                    {
                        switch (statusFilter)
                        {
                            case "Active":
                                query += " AND b.Status = 'Borrowed' AND b.ReturnDate IS NULL";
                                break;
                            case "Returned":
                                query += " AND b.Status = 'Returned' AND b.ReturnDate IS NOT NULL";
                                break;
                            case "Overdue":
                                query += " AND b.Status = 'Borrowed' AND b.ReturnDate IS NULL AND b.DueDate < CURDATE()";
                                break;
                        }
                    }

                    query += " ORDER BY b.BorrowDate DESC";

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
                                borrowings.Add(new BorrowingInfo
                                {
                                    BorrowingId = reader["BorrowingId"].ToString(),
                                    MemberId = reader["MemberId"].ToString(),
                                    MemberName = reader["MemberName"].ToString(),
                                    BookId = reader["BookId"].ToString(),
                                    BookTitle = reader["BookTitle"].ToString(),
                                    BookISBN = reader["BookISBN"]?.ToString(),
                                    BorrowDate = Convert.ToDateTime(reader["BorrowDate"]),
                                    DueDate = Convert.ToDateTime(reader["DueDate"]),
                                    ReturnDate = reader["ReturnDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ReturnDate"]) : null,
                                    Status = reader["Status"].ToString(),
                                    FineAmount = Convert.ToDecimal(reader["FineAmount"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting borrowings: {ex.Message}");
            }

            return borrowings;
        }

        public bool CheckoutBook(string memberId, string bookId, int loanPeriodDays = 14)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    // Check if member exists and is active
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

                    // Check if book exists and is available
                    string bookQuery = "SELECT AvailableCopies FROM Books WHERE BookId = @bookId AND AvailableCopies > 0";
                    using (var bookCmd = new MySqlCommand(bookQuery, connection))
                    {
                        bookCmd.Parameters.AddWithValue("@bookId", bookId);
                        var result = bookCmd.ExecuteScalar();
                        if (result == null)
                        {
                            throw new Exception("Book not found or not available.");
                        }
                    }

                    // Check if member already has this book borrowed
                    string existingBorrowQuery = @"
                        SELECT COUNT(*) FROM Borrowings
                        WHERE MemberId = @memberId AND BookId = @bookId AND ReturnDate IS NULL";
                    using (var existingCmd = new MySqlCommand(existingBorrowQuery, connection))
                    {
                        existingCmd.Parameters.AddWithValue("@memberId", memberId);
                        existingCmd.Parameters.AddWithValue("@bookId", bookId);
                        int existingCount = Convert.ToInt32(existingCmd.ExecuteScalar());
                        if (existingCount > 0)
                        {
                            throw new Exception("Member already has this book borrowed.");
                        }
                    }

                    // Check member's borrowing limit (max 5 books)
                    string limitQuery = @"
                        SELECT COUNT(*) FROM Borrowings
                        WHERE MemberId = @memberId AND ReturnDate IS NULL";
                    using (var limitCmd = new MySqlCommand(limitQuery, connection))
                    {
                        limitCmd.Parameters.AddWithValue("@memberId", memberId);
                        int borrowedCount = Convert.ToInt32(limitCmd.ExecuteScalar());
                        if (borrowedCount >= 5)
                        {
                            throw new Exception("Member has reached the maximum borrowing limit (5 books).");
                        }
                    }

                    // Check for unpaid fines
                    string fineQuery = @"
                        SELECT COUNT(*) FROM Fines f
                        INNER JOIN Borrowings b ON f.BorrowingId = b.BorrowingId
                        WHERE b.MemberId = @memberId AND f.Status = 'Unpaid'";
                    using (var fineCmd = new MySqlCommand(fineQuery, connection))
                    {
                        fineCmd.Parameters.AddWithValue("@memberId", memberId);
                        int unpaidFines = Convert.ToInt32(fineCmd.ExecuteScalar());
                        if (unpaidFines > 0)
                        {
                            throw new Exception("Member has unpaid fines. Please clear fines before borrowing.");
                        }
                    }

                    // Perform checkout transaction
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            DateTime borrowDate = DateTime.Now;
                            DateTime dueDate = borrowDate.AddDays(loanPeriodDays);

                            // Insert borrowing record
                            string insertQuery = @"
                                INSERT INTO Borrowings (MemberId, BookId, BorrowDate, DueDate, Status)
                                VALUES (@memberId, @bookId, @borrowDate, @dueDate, 'Borrowed')";
                            using (var insertCmd = new MySqlCommand(insertQuery, connection, transaction))
                            {
                                insertCmd.Parameters.AddWithValue("@memberId", memberId);
                                insertCmd.Parameters.AddWithValue("@bookId", bookId);
                                insertCmd.Parameters.AddWithValue("@borrowDate", borrowDate);
                                insertCmd.Parameters.AddWithValue("@dueDate", dueDate);
                                insertCmd.ExecuteNonQuery();
                            }

                            // Decrease available copies
                            string updateBookQuery = "UPDATE Books SET AvailableCopies = AvailableCopies - 1 WHERE BookId = @bookId";
                            using (var updateCmd = new MySqlCommand(updateBookQuery, connection, transaction))
                            {
                                updateCmd.Parameters.AddWithValue("@bookId", bookId);
                                updateCmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking out book: {ex.Message}");
                throw;
            }
        }

        public bool ReturnBook(string borrowingId)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    // Get borrowing details
                    string getBorrowingQuery = @"
                        SELECT MemberId, BookId, DueDate, ReturnDate
                        FROM Borrowings WHERE BorrowingId = @borrowingId";
                    string memberId = "";
                    string bookId = "";
                    DateTime dueDate = DateTime.MinValue;
                    DateTime? returnDate = null;

                    using (var getCmd = new MySqlCommand(getBorrowingQuery, connection))
                    {
                        getCmd.Parameters.AddWithValue("@borrowingId", borrowingId);
                        using (var reader = getCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                memberId = reader["MemberId"].ToString();
                                bookId = reader["BookId"].ToString();
                                dueDate = Convert.ToDateTime(reader["DueDate"]);
                                returnDate = reader["ReturnDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ReturnDate"]) : null;
                            }
                            else
                            {
                                throw new Exception("Borrowing record not found.");
                            }
                        }
                    }

                    // Check if already returned
                    if (returnDate.HasValue)
                    {
                        throw new Exception("Book has already been returned.");
                    }

                    // Perform return transaction
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            DateTime returnDateTime = DateTime.Now;
                            decimal fineAmount = 0;

                            // Calculate fine if overdue
                            if (returnDateTime > dueDate)
                            {
                                TimeSpan overdue = returnDateTime - dueDate;
                                int overdueDays = (int)System.Math.Ceiling(overdue.TotalDays);
                                fineAmount = overdueDays * 5.00m; // $5 per day
                            }

                            // Update borrowing record
                            string updateBorrowingQuery = @"
                                UPDATE Borrowings
                                SET ReturnDate = @returnDate, Status = 'Returned', FineAmount = @fineAmount
                                WHERE BorrowingId = @borrowingId";
                            using (var updateCmd = new MySqlCommand(updateBorrowingQuery, connection, transaction))
                            {
                                updateCmd.Parameters.AddWithValue("@borrowingId", borrowingId);
                                updateCmd.Parameters.AddWithValue("@returnDate", returnDateTime);
                                updateCmd.Parameters.AddWithValue("@fineAmount", fineAmount);
                                updateCmd.ExecuteNonQuery();
                            }

                            // Increase available copies
                            string updateBookQuery = "UPDATE Books SET AvailableCopies = AvailableCopies + 1 WHERE BookId = @bookId";
                            using (var bookCmd = new MySqlCommand(updateBookQuery, connection, transaction))
                            {
                                bookCmd.Parameters.AddWithValue("@bookId", bookId);
                                bookCmd.ExecuteNonQuery();
                            }

                            // Create fine record if applicable
                            if (fineAmount > 0)
                            {
                                string insertFineQuery = @"
                                    INSERT INTO Fines (BorrowingId, MemberId, Amount, Reason, Status)
                                    VALUES (@borrowingId, @memberId, @amount, 'Overdue return', 'Unpaid')";
                                using (var fineCmd = new MySqlCommand(insertFineQuery, connection, transaction))
                                {
                                    fineCmd.Parameters.AddWithValue("@borrowingId", borrowingId);
                                    fineCmd.Parameters.AddWithValue("@memberId", memberId);
                                    fineCmd.Parameters.AddWithValue("@amount", fineAmount);
                                    fineCmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error returning book: {ex.Message}");
                throw;
            }
        }

        public bool RenewBook(string borrowingId, int extensionDays = 7)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    // Check if borrowing exists and is active
                    string checkQuery = @"
                        SELECT DueDate, ReturnDate FROM Borrowings
                        WHERE BorrowingId = @borrowingId AND ReturnDate IS NULL";
                    DateTime currentDueDate = DateTime.MinValue;

                    using (var checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@borrowingId", borrowingId);
                        using (var reader = checkCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentDueDate = Convert.ToDateTime(reader["DueDate"]);
                            }
                            else
                            {
                                throw new Exception("Borrowing record not found or book already returned.");
                            }
                        }
                    }

                    // Check if renewal is allowed (not overdue)
                    if (DateTime.Now > currentDueDate)
                    {
                        throw new Exception("Cannot renew overdue books. Please return the book first.");
                    }

                    // Update due date
                    DateTime newDueDate = currentDueDate.AddDays(extensionDays);
                    string updateQuery = "UPDATE Borrowings SET DueDate = @newDueDate WHERE BorrowingId = @borrowingId";

                    using (var updateCmd = new MySqlCommand(updateQuery, connection))
                    {
                        updateCmd.Parameters.AddWithValue("@borrowingId", borrowingId);
                        updateCmd.Parameters.AddWithValue("@newDueDate", newDueDate);
                        updateCmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error renewing book: {ex.Message}");
                throw;
            }
        }

        public BorrowingInfo GetBorrowingById(string borrowingId)
        {
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();

                    string query = @"
                        SELECT b.BorrowingId, b.MemberId, b.BookId, b.BorrowDate, b.DueDate, b.ReturnDate, b.Status, b.FineAmount,
                               CONCAT(u.FirstName, ' ', u.LastName) as MemberName,
                               bk.Title as BookTitle, bk.ISBN as BookISBN
                        FROM Borrowings b
                        INNER JOIN Members m ON b.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        INNER JOIN Books bk ON b.BookId = bk.BookId
                        WHERE b.BorrowingId = @borrowingId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@borrowingId", borrowingId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new BorrowingInfo
                                {
                                    BorrowingId = reader["BorrowingId"].ToString(),
                                    MemberId = reader["MemberId"].ToString(),
                                    MemberName = reader["MemberName"].ToString(),
                                    BookId = reader["BookId"].ToString(),
                                    BookTitle = reader["BookTitle"].ToString(),
                                    BookISBN = reader["BookISBN"]?.ToString(),
                                    BorrowDate = Convert.ToDateTime(reader["BorrowDate"]),
                                    DueDate = Convert.ToDateTime(reader["DueDate"]),
                                    ReturnDate = reader["ReturnDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["ReturnDate"]) : null,
                                    Status = reader["Status"].ToString(),
                                    FineAmount = Convert.ToDecimal(reader["FineAmount"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting borrowing: {ex.Message}");
            }

            return null;
        }
    }
}