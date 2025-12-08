using System;
using System.Data;
using Library_Management_System.Helpers;
using MySql.Data.MySqlClient;

namespace Library_Management_System.Services
{
    public class DashboardService
    {
        public class DashboardStatistics
        {
            public int TotalBooks { get; set; }
            public int ActiveMembers { get; set; }
            public int BooksBorrowed { get; set; }
            public int OverdueBooks { get; set; }
            public int TodaysBorrowings { get; set; }
            public int TodaysReturns { get; set; }
            public decimal PendingFines { get; set; }
            public int Reservations { get; set; }
            public int TotalBooksLastWeek { get; set; }
            public int ActiveMembersLastWeek { get; set; }
            public int BooksBorrowedLastWeek { get; set; }
            public int OverdueBooksLastWeek { get; set; }
        }

        public DashboardStatistics GetDashboardStatistics()
        {
            var stats = new DashboardStatistics();

            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    stats.ActiveMembers = GetActiveMembersCount(connection);
                    stats.ActiveMembersLastWeek = GetActiveMembersCountLastWeek(connection);

                    stats.TotalBooks = GetTotalBooksCount(connection);
                    stats.TotalBooksLastWeek = GetTotalBooksCountLastWeek(connection);

                    stats.BooksBorrowed = GetBooksBorrowedCount(connection);
                    stats.BooksBorrowedLastWeek = GetBooksBorrowedCountLastWeek(connection);

                    stats.OverdueBooks = GetOverdueBooksCount(connection);
                    stats.OverdueBooksLastWeek = GetOverdueBooksCountLastWeek(connection);

                    stats.TodaysBorrowings = GetTodaysBorrowingsCount(connection);

                    stats.TodaysReturns = GetTodaysReturnsCount(connection);

                    stats.PendingFines = GetPendingFinesAmount(connection);

                    stats.Reservations = GetReservationsCount(connection);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard statistics: {ex.Message}");
            }

            return stats;
        }

        private int GetActiveMembersCount(MySqlConnection connection)
        {
            try
            {
                string query = @"
                    SELECT COUNT(*) 
                    FROM Members 
                    WHERE Status = 1 AND (ExpirationDate IS NULL OR ExpirationDate > NOW())";

                using (var command = new MySqlCommand(query, connection))
                {
                    var result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        private int GetActiveMembersCountLastWeek(MySqlConnection connection)
        {
            try
            {
                string query = @"
                    SELECT COUNT(*) 
                    FROM Members 
                    WHERE Status = 1 
                    AND (ExpirationDate IS NULL OR ExpirationDate > DATE_SUB(NOW(), INTERVAL 7 DAY))
                    AND RegistrationDate <= DATE_SUB(NOW(), INTERVAL 7 DAY)";

                using (var command = new MySqlCommand(query, connection))
                {
                    var result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        private int GetTotalBooksCount(MySqlConnection connection)
        {
            try
            {
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'Books'";

                using (var checkCommand = new MySqlCommand(checkTableQuery, connection))
                {
                    var tableExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
                    
                    if (!tableExists)
                        return 0;

                    string query = "SELECT COUNT(*) FROM Books WHERE IsActive = 1";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private int GetTotalBooksCountLastWeek(MySqlConnection connection)
        {
            try
            {
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'Books'";

                using (var checkCommand = new MySqlCommand(checkTableQuery, connection))
                {
                    var tableExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
                    
                    if (!tableExists)
                        return 0;

                    string query = @"
                        SELECT COUNT(*) 
                        FROM Books 
                        WHERE IsActive = 1 
                        AND CreatedDate <= DATE_SUB(NOW(), INTERVAL 7 DAY)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private int GetBooksBorrowedCount(MySqlConnection connection)
        {
            try
            {
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'Borrowings'";

                using (var checkCommand = new MySqlCommand(checkTableQuery, connection))
                {
                    var tableExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
                    
                    if (!tableExists)
                        return 0;

                    string query = @"
                        SELECT COUNT(*) 
                        FROM Borrowings 
                        WHERE ReturnDate IS NULL";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private int GetBooksBorrowedCountLastWeek(MySqlConnection connection)
        {
            try
            {
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'Borrowings'";

                using (var checkCommand = new MySqlCommand(checkTableQuery, connection))
                {
                    var tableExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
                    
                    if (!tableExists)
                        return 0;

                    string query = @"
                        SELECT COUNT(*) 
                        FROM Borrowings 
                        WHERE ReturnDate IS NULL 
                        AND BorrowDate <= DATE_SUB(NOW(), INTERVAL 7 DAY)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private int GetOverdueBooksCount(MySqlConnection connection)
        {
            try
            {
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'Borrowings'";

                using (var checkCommand = new MySqlCommand(checkTableQuery, connection))
                {
                    var tableExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
                    
                    if (!tableExists)
                        return 0;

                    string query = @"
                        SELECT COUNT(*) 
                        FROM Borrowings 
                        WHERE ReturnDate IS NULL 
                        AND DueDate < NOW()";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private int GetOverdueBooksCountLastWeek(MySqlConnection connection)
        {
            try
            {
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'Borrowings'";

                using (var checkCommand = new MySqlCommand(checkTableQuery, connection))
                {
                    var tableExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
                    
                    if (!tableExists)
                        return 0;

                    string query = @"
                        SELECT COUNT(*) 
                        FROM Borrowings 
                        WHERE ReturnDate IS NULL 
                        AND DueDate < DATE_SUB(NOW(), INTERVAL 7 DAY)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private int GetTodaysBorrowingsCount(MySqlConnection connection)
        {
            try
            {
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'Borrowings'";

                using (var checkCommand = new MySqlCommand(checkTableQuery, connection))
                {
                    var tableExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
                    
                    if (!tableExists)
                        return 0;

                    string query = @"
                        SELECT COUNT(*) 
                        FROM Borrowings 
                        WHERE DATE(BorrowDate) = CURDATE()";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private int GetTodaysReturnsCount(MySqlConnection connection)
        {
            try
            {
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'Borrowings'";

                using (var checkCommand = new MySqlCommand(checkTableQuery, connection))
                {
                    var tableExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
                    
                    if (!tableExists)
                        return 0;

                    string query = @"
                        SELECT COUNT(*) 
                        FROM Borrowings 
                        WHERE DATE(ReturnDate) = CURDATE()";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private decimal GetPendingFinesAmount(MySqlConnection connection)
        {
            try
            {
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'Fines'";

                using (var checkCommand = new MySqlCommand(checkTableQuery, connection))
                {
                    var tableExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
                    
                    if (!tableExists)
                        return 0;

                    string query = @"
                        SELECT COALESCE(SUM(Amount), 0) 
                        FROM Fines 
                        WHERE Status = 'Pending' OR Status = 'Unpaid'";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return result != null ? Convert.ToDecimal(result) : 0;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private int GetReservationsCount(MySqlConnection connection)
        {
            try
            {
                string checkTableQuery = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = DATABASE() 
                    AND table_name = 'Reservations'";

                using (var checkCommand = new MySqlCommand(checkTableQuery, connection))
                {
                    var tableExists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
                    
                    if (!tableExists)
                        return 0;

                    string query = @"
                        SELECT COUNT(*) 
                        FROM Reservations 
                        WHERE Status = 'Pending' OR Status = 'Active'";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        public string CalculatePercentageChange(int current, int previous)
        {
            if (previous == 0)
            {
                return current > 0 ? "+100%" : "0%";
            }

            double change = ((double)(current - previous) / previous) * 100;
            string sign = change >= 0 ? "+" : "";
            return $"{sign}{change:F0}% from last week";
        }
    }
}

