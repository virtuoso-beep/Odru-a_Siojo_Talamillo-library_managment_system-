using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using LMS_Library_Management_System.Helper;
using LMS_Library_Management_System.Interfaces;
using LMS_Library_Management_System.Models;

namespace LMS_Library_Management_System.Service
{
    /// <summary>
    /// Service class for circulation operations (borrow, return, renew)
    /// </summary>
    public class CirculationService : ICirculationService
    {
        private const int DEFAULT_BORROW_DAYS = 14; // Default borrowing period
        private const decimal FINE_PER_DAY = 5.00m; // Fine amount per day overdue

        /// <summary>
        /// Borrows a book to a member
        /// </summary>
        public bool BorrowBook(int memberId, int bookId, DateTime dueDate, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Validate member
                            if (!ValidateMember(memberId, out string memberError))
                            {
                                errorMessage = memberError;
                                return false;
                            }

                            // Check if book is available
                            using (var checkBookCmd = new MySqlCommand(
                                "SELECT AvailableCopies, Title FROM Books WHERE BookId = @BookId",
                                connection, transaction))
                            {
                                checkBookCmd.Parameters.AddWithValue("@BookId", bookId);
                                using (var reader = checkBookCmd.ExecuteReader())
                                {
                                    if (!reader.Read())
                                    {
                                        errorMessage = "Book not found.";
                                        return false;
                                    }

                                    int availableCopies = reader.GetInt32("AvailableCopies");
                                    if (availableCopies <= 0)
                                    {
                                        errorMessage = "No copies available for this book.";
                                        return false;
                                    }
                                }
                            }

                            // Check if member already has this book borrowed
                            using (var checkBorrowingCmd = new MySqlCommand(
                                "SELECT COUNT(*) FROM Borrowings WHERE MemberId = @MemberId AND BookId = @BookId AND ReturnDate IS NULL",
                                connection, transaction))
                            {
                                checkBorrowingCmd.Parameters.AddWithValue("@MemberId", memberId);
                                checkBorrowingCmd.Parameters.AddWithValue("@BookId", bookId);
                                int existingBorrowings = Convert.ToInt32(checkBorrowingCmd.ExecuteScalar());
                                
                                if (existingBorrowings > 0)
                                {
                                    errorMessage = "Member already has this book borrowed.";
                                    return false;
                                }
                            }

                            // Create borrowing record
                            using (var borrowCmd = new MySqlCommand(
                                @"INSERT INTO Borrowings (MemberId, BookId, BorrowDate, DueDate, Status) 
                                  VALUES (@MemberId, @BookId, @BorrowDate, @DueDate, 'Borrowed')",
                                connection, transaction))
                            {
                                borrowCmd.Parameters.AddWithValue("@MemberId", memberId);
                                borrowCmd.Parameters.AddWithValue("@BookId", bookId);
                                borrowCmd.Parameters.AddWithValue("@BorrowDate", DateTime.Now);
                                borrowCmd.Parameters.AddWithValue("@DueDate", dueDate);
                                borrowCmd.ExecuteNonQuery();
                            }

                            // Decrease available copies
                            using (var updateBookCmd = new MySqlCommand(
                                "UPDATE Books SET AvailableCopies = AvailableCopies - 1 WHERE BookId = @BookId",
                                connection, transaction))
                            {
                                updateBookCmd.Parameters.AddWithValue("@BookId", bookId);
                                updateBookCmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            errorMessage = $"Error borrowing book: {ex.Message}";
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error borrowing book: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Returns a borrowed book
        /// </summary>
        public bool ReturnBook(int borrowingId, out decimal fineAmount, out string errorMessage)
        {
            fineAmount = 0;
            errorMessage = string.Empty;

            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Get borrowing details
                            int bookId = 0;
                            DateTime dueDate = DateTime.Now;

                            using (var getBorrowingCmd = new MySqlCommand(
                                "SELECT BookId, DueDate FROM Borrowings WHERE BorrowingId = @BorrowingId AND ReturnDate IS NULL",
                                connection, transaction))
                            {
                                getBorrowingCmd.Parameters.AddWithValue("@BorrowingId", borrowingId);
                                using (var reader = getBorrowingCmd.ExecuteReader())
                                {
                                    if (!reader.Read())
                                    {
                                        errorMessage = "Borrowing record not found or already returned.";
                                        return false;
                                    }
                                    bookId = reader.GetInt32("BookId");
                                    dueDate = reader.GetDateTime("DueDate");
                                }
                            }

                            // Calculate fine if overdue
                            if (DateTime.Now > dueDate)
                            {
                                int daysOverdue = (DateTime.Now - dueDate).Days;
                                fineAmount = daysOverdue * FINE_PER_DAY;
                            }

                            // Update borrowing record
                            using (var returnCmd = new MySqlCommand(
                                @"UPDATE Borrowings 
                                  SET ReturnDate = @ReturnDate, 
                                      Status = CASE WHEN @FineAmount > 0 THEN 'Overdue' ELSE 'Returned' END,
                                      FineAmount = @FineAmount
                                  WHERE BorrowingId = @BorrowingId",
                                connection, transaction))
                            {
                                returnCmd.Parameters.AddWithValue("@BorrowingId", borrowingId);
                                returnCmd.Parameters.AddWithValue("@ReturnDate", DateTime.Now);
                                returnCmd.Parameters.AddWithValue("@FineAmount", fineAmount);
                                returnCmd.ExecuteNonQuery();
                            }

                            // Increase available copies
                            using (var updateBookCmd = new MySqlCommand(
                                "UPDATE Books SET AvailableCopies = AvailableCopies + 1 WHERE BookId = @BookId",
                                connection, transaction))
                            {
                                updateBookCmd.Parameters.AddWithValue("@BookId", bookId);
                                updateBookCmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            errorMessage = $"Error returning book: {ex.Message}";
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error returning book: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Renews a borrowed book
        /// </summary>
        public bool RenewBook(int borrowingId, DateTime newDueDate, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var renewCmd = new MySqlCommand(
                        @"UPDATE Borrowings 
                          SET DueDate = @NewDueDate 
                          WHERE BorrowingId = @BorrowingId AND ReturnDate IS NULL",
                        connection))
                    {
                        renewCmd.Parameters.AddWithValue("@BorrowingId", borrowingId);
                        renewCmd.Parameters.AddWithValue("@NewDueDate", newDueDate);
                        
                        int rowsAffected = renewCmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            errorMessage = "Borrowing record not found or already returned.";
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error renewing book: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Gets all active borrowings for a member
        /// </summary>
        public List<BorrowingTransaction> GetActiveBorrowingsByMember(int memberId)
        {
            var borrowings = new List<BorrowingTransaction>();

            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT 
                            b.BorrowingId,
                            b.MemberId,
                            b.BookId,
                            CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
                            m.MemberNumber,
                            bk.Title AS BookTitle,
                            bk.Author,
                            bk.ISBN,
                            b.BorrowDate,
                            b.DueDate,
                            b.ReturnDate,
                            b.Status,
                            b.FineAmount,
                            CASE WHEN b.ReturnDate IS NULL AND b.DueDate < NOW() THEN 1 ELSE 0 END AS IsOverdue
                        FROM Borrowings b
                        INNER JOIN Members m ON b.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        INNER JOIN Books bk ON b.BookId = bk.BookId
                        WHERE b.MemberId = @MemberId AND b.ReturnDate IS NULL
                        ORDER BY b.DueDate ASC";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MemberId", memberId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var transaction = new BorrowingTransaction
                                {
                                    BorrowingId = reader.GetInt32("BorrowingId"),
                                    MemberId = reader.GetInt32("MemberId"),
                                    BookId = reader.GetInt32("BookId"),
                                    MemberName = reader.GetString("MemberName"),
                                    MemberNumber = reader.GetString("MemberNumber"),
                                    BookTitle = reader.GetString("BookTitle"),
                                    Author = reader.GetString("Author"),
                                    ISBN = reader.IsDBNull(reader.GetOrdinal("ISBN")) ? "" : reader.GetString("ISBN"),
                                    BorrowDate = reader.GetDateTime("BorrowDate"),
                                    DueDate = reader.GetDateTime("DueDate"),
                                    ReturnDate = reader.IsDBNull(reader.GetOrdinal("ReturnDate")) ? (DateTime?)null : reader.GetDateTime("ReturnDate"),
                                    Status = reader.GetString("Status"),
                                    FineAmount = reader.GetDecimal("FineAmount"),
                                    IsOverdue = reader.GetInt32("IsOverdue") == 1
                                };

                                // Update status if overdue
                                if (transaction.IsOverdue)
                                {
                                    transaction.Status = "Overdue";
                                }

                                borrowings.Add(transaction);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting active borrowings: {ex.Message}");
            }

            return borrowings;
        }

        /// <summary>
        /// Gets all active borrowings (for return dialog)
        /// </summary>
        public List<BorrowingTransaction> GetActiveBorrowings()
        {
            var borrowings = new List<BorrowingTransaction>();

            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT 
                            b.BorrowingId,
                            b.MemberId,
                            b.BookId,
                            CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
                            m.MemberNumber,
                            bk.Title AS BookTitle,
                            bk.Author,
                            bk.ISBN,
                            b.BorrowDate,
                            b.DueDate,
                            b.ReturnDate,
                            b.Status,
                            b.FineAmount,
                            CASE WHEN b.ReturnDate IS NULL AND b.DueDate < NOW() THEN 1 ELSE 0 END AS IsOverdue
                        FROM Borrowings b
                        INNER JOIN Members m ON b.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        INNER JOIN Books bk ON b.BookId = bk.BookId
                        WHERE b.ReturnDate IS NULL
                        ORDER BY b.DueDate ASC";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var transaction = new BorrowingTransaction
                                {
                                    BorrowingId = reader.GetInt32("BorrowingId"),
                                    MemberId = reader.GetInt32("MemberId"),
                                    BookId = reader.GetInt32("BookId"),
                                    MemberName = reader.GetString("MemberName"),
                                    MemberNumber = reader.GetString("MemberNumber"),
                                    BookTitle = reader.GetString("BookTitle"),
                                    Author = reader.GetString("Author"),
                                    ISBN = reader.IsDBNull(reader.GetOrdinal("ISBN")) ? "" : reader.GetString("ISBN"),
                                    BorrowDate = reader.GetDateTime("BorrowDate"),
                                    DueDate = reader.GetDateTime("DueDate"),
                                    ReturnDate = reader.IsDBNull(reader.GetOrdinal("ReturnDate")) ? (DateTime?)null : reader.GetDateTime("ReturnDate"),
                                    Status = reader.GetString("Status"),
                                    FineAmount = reader.GetDecimal("FineAmount"),
                                    IsOverdue = reader.GetInt32("IsOverdue") == 1
                                };

                                // Update status if overdue
                                if (transaction.IsOverdue)
                                {
                                    transaction.Status = "Overdue";
                                    // Calculate fine
                                    int daysOverdue = (DateTime.Now - transaction.DueDate).Days;
                                    transaction.FineAmount = daysOverdue * FINE_PER_DAY;
                                }
                                else
                                {
                                    transaction.Status = "Active";
                                }

                                borrowings.Add(transaction);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting active borrowings: {ex.Message}");
            }

            return borrowings;
        }

        /// <summary>
        /// Gets a borrowing transaction by ID
        /// </summary>
        public BorrowingTransaction GetBorrowingById(int borrowingId)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT 
                            b.BorrowingId,
                            b.MemberId,
                            b.BookId,
                            CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
                            m.MemberNumber,
                            bk.Title AS BookTitle,
                            bk.Author,
                            bk.ISBN,
                            b.BorrowDate,
                            b.DueDate,
                            b.ReturnDate,
                            b.Status,
                            b.FineAmount,
                            CASE WHEN b.ReturnDate IS NULL AND b.DueDate < NOW() THEN 1 ELSE 0 END AS IsOverdue
                        FROM Borrowings b
                        INNER JOIN Members m ON b.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        INNER JOIN Books bk ON b.BookId = bk.BookId
                        WHERE b.BorrowingId = @BorrowingId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@BorrowingId", borrowingId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var transaction = new BorrowingTransaction
                                {
                                    BorrowingId = reader.GetInt32("BorrowingId"),
                                    MemberId = reader.GetInt32("MemberId"),
                                    BookId = reader.GetInt32("BookId"),
                                    MemberName = reader.GetString("MemberName"),
                                    MemberNumber = reader.GetString("MemberNumber"),
                                    BookTitle = reader.GetString("BookTitle"),
                                    Author = reader.GetString("Author"),
                                    ISBN = reader.IsDBNull(reader.GetOrdinal("ISBN")) ? "" : reader.GetString("ISBN"),
                                    BorrowDate = reader.GetDateTime("BorrowDate"),
                                    DueDate = reader.GetDateTime("DueDate"),
                                    ReturnDate = reader.IsDBNull(reader.GetOrdinal("ReturnDate")) ? (DateTime?)null : reader.GetDateTime("ReturnDate"),
                                    Status = reader.GetString("Status"),
                                    FineAmount = reader.GetDecimal("FineAmount"),
                                    IsOverdue = reader.GetInt32("IsOverdue") == 1
                                };

                                if (transaction.IsOverdue)
                                {
                                    transaction.Status = "Overdue";
                                }

                                return transaction;
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

        /// <summary>
        /// Gets all borrowing transactions for display (including returned)
        /// </summary>
        public List<BorrowingTransaction> GetAllBorrowingsForDisplay()
        {
            var borrowings = new List<BorrowingTransaction>();

            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    string query = @"
                        SELECT 
                            b.BorrowingId,
                            b.MemberId,
                            b.BookId,
                            CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
                            m.MemberNumber,
                            bk.Title AS BookTitle,
                            bk.Author,
                            bk.ISBN,
                            b.BorrowDate,
                            b.DueDate,
                            b.ReturnDate,
                            b.Status,
                            b.FineAmount,
                            CASE WHEN b.ReturnDate IS NULL AND b.DueDate < NOW() THEN 1 ELSE 0 END AS IsOverdue
                        FROM Borrowings b
                        INNER JOIN Members m ON b.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        INNER JOIN Books bk ON b.BookId = bk.BookId
                        ORDER BY b.BorrowDate DESC";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var transaction = new BorrowingTransaction
                                {
                                    BorrowingId = reader.GetInt32("BorrowingId"),
                                    MemberId = reader.GetInt32("MemberId"),
                                    BookId = reader.GetInt32("BookId"),
                                    MemberName = reader.GetString("MemberName"),
                                    MemberNumber = reader.GetString("MemberNumber"),
                                    BookTitle = reader.GetString("BookTitle"),
                                    Author = reader.GetString("Author"),
                                    ISBN = reader.IsDBNull(reader.GetOrdinal("ISBN")) ? "" : reader.GetString("ISBN"),
                                    BorrowDate = reader.GetDateTime("BorrowDate"),
                                    DueDate = reader.GetDateTime("DueDate"),
                                    ReturnDate = reader.IsDBNull(reader.GetOrdinal("ReturnDate")) ? (DateTime?)null : reader.GetDateTime("ReturnDate"),
                                    Status = reader.GetString("Status"),
                                    FineAmount = reader.GetDecimal("FineAmount"),
                                    IsOverdue = reader.GetInt32("IsOverdue") == 1
                                };

                                // Update status if overdue
                                if (transaction.IsOverdue && !transaction.ReturnDate.HasValue)
                                {
                                    transaction.Status = "Overdue";
                                    // Calculate fine
                                    int daysOverdue = (DateTime.Now - transaction.DueDate).Days;
                                    transaction.FineAmount = daysOverdue * FINE_PER_DAY;
                                }
                                else if (!transaction.ReturnDate.HasValue)
                                {
                                    transaction.Status = "Active";
                                }
                                else
                                {
                                    transaction.Status = "Returned";
                                }

                                borrowings.Add(transaction);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting all borrowings: {ex.Message}");
            }

            return borrowings;
        }

        /// <summary>
        /// Validates if a member can borrow books
        /// </summary>
        public bool ValidateMember(int memberId, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Check if member exists and is active
                    string query = @"
                        SELECT m.Status, COUNT(b.BorrowingId) AS ActiveBorrowings
                        FROM Members m
                        LEFT JOIN Borrowings b ON m.MemberId = b.MemberId AND b.ReturnDate IS NULL
                        WHERE m.MemberId = @MemberId
                        GROUP BY m.Status";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MemberId", memberId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                errorMessage = "Member not found.";
                                return false;
                            }

                            int status = reader.GetInt32("Status");
                            int activeBorrowings = reader.GetInt32("ActiveBorrowings");

                            if (status != 1) // 1 = Active
                            {
                                errorMessage = "Member account is not active.";
                                return false;
                            }

                            // Check borrowing limit (e.g., max 5 books)
                            if (activeBorrowings >= 5)
                            {
                                errorMessage = "Member has reached the maximum borrowing limit (5 books).";
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error validating member: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Calculates fine for overdue book
        /// </summary>
        public decimal CalculateFine(int borrowingId)
        {
            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    using (var command = new MySqlCommand(
                        "SELECT DueDate FROM Borrowings WHERE BorrowingId = @BorrowingId AND ReturnDate IS NULL",
                        connection))
                    {
                        command.Parameters.AddWithValue("@BorrowingId", borrowingId);
                        object result = command.ExecuteScalar();
                        
                        if (result != null && result != DBNull.Value)
                        {
                            DateTime dueDate = Convert.ToDateTime(result);
                            if (DateTime.Now > dueDate)
                            {
                                int daysOverdue = (DateTime.Now - dueDate).Days;
                                return daysOverdue * FINE_PER_DAY;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating fine: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Gets borrowing statistics
        /// </summary>
        public BorrowingStatistics GetBorrowingStatistics()
        {
            var stats = new BorrowingStatistics();

            try
            {
                using (var connection = MYSqlHelper.CreateConnection())
                {
                    // Currently borrowed
                    using (var cmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM Borrowings WHERE ReturnDate IS NULL",
                        connection))
                    {
                        stats.CurrentlyBorrowed = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Overdue
                    using (var cmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM Borrowings WHERE ReturnDate IS NULL AND DueDate < NOW()",
                        connection))
                    {
                        stats.Overdue = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Returned today
                    using (var cmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM Borrowings WHERE DATE(ReturnDate) = CURDATE()",
                        connection))
                    {
                        stats.ReturnedToday = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting statistics: {ex.Message}");
            }

            return stats;
        }
    }
}


