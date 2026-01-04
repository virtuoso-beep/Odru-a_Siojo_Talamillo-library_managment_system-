using System;
using System.Data;
using Library_Management_System.Helper;
using Library_Management_System.Interfaces;
using MySql.Data.MySqlClient;
namespace Library_Management_System.Service
{
    public class DashboardService : IDashboardService
    {
        public class DashboardStatistics
        {
            public int ActiveMembers { get; set; }
            public int ActiveMembersLastWeek { get; set; }
            public int TotalBooks { get; set; }
            public int TotalBooksLastWeek { get; set; }
            public int BooksBorrowed { get; set; }
            public int BooksBorrowedLastWeek { get; set; }
            public int OverdueBooks { get; set; }
            public int OverdueBooksLastWeek { get; set; }
            public int TodaysBorrowings { get; set; }
            public int TodaysReturns { get; set; }
            public decimal PendingFines { get; set; }
            public int Reservations { get; set; }
        }
        public DashboardStatistics GetDashboardStatistics()
        {
            var stats = new DashboardStatistics();
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_GetDashboardStatistics", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                stats.ActiveMembers = Convert.ToInt32(reader["ActiveMembers"]);
                                stats.ActiveMembersLastWeek = Convert.ToInt32(reader["ActiveMembersLastWeek"]);
                                stats.TotalBooks = Convert.ToInt32(reader["TotalBooks"]);
                                stats.TotalBooksLastWeek = Convert.ToInt32(reader["TotalBooksLastWeek"]);
                                stats.BooksBorrowed = Convert.ToInt32(reader["BooksBorrowed"]);
                                stats.BooksBorrowedLastWeek = Convert.ToInt32(reader["BooksBorrowedLastWeek"]);
                                stats.OverdueBooks = Convert.ToInt32(reader["OverdueBooks"]);
                                stats.OverdueBooksLastWeek = Convert.ToInt32(reader["OverdueBooksLastWeek"]);
                                stats.TodaysBorrowings = Convert.ToInt32(reader["TodaysBorrowings"]);
                                stats.TodaysReturns = Convert.ToInt32(reader["TodaysReturns"]);
                                stats.PendingFines = Convert.ToDecimal(reader["PendingFines"]);
                                stats.Reservations = Convert.ToInt32(reader["ActiveReservations"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting dashboard statistics: {ex.Message}");
            }
            return stats;
        }
        public int GetActiveMembersCount()
        {
            return GetDashboardStatistics().ActiveMembers;
        }
        public int GetActiveMembersCountLastWeek()
        {
            return GetDashboardStatistics().ActiveMembersLastWeek;
        }
        public int GetTotalBooksCount()
        {
            return GetDashboardStatistics().TotalBooks;
        }
        public int GetTotalBooksCountLastWeek()
        {
            return GetDashboardStatistics().TotalBooksLastWeek;
        }
        public int GetBooksBorrowedCount()
        {
            return GetDashboardStatistics().BooksBorrowed;
        }
        public int GetBooksBorrowedCountLastWeek()
        {
            return GetDashboardStatistics().BooksBorrowedLastWeek;
        }
        public int GetOverdueBooksCount()
        {
            return GetDashboardStatistics().OverdueBooks;
        }
        public int GetOverdueBooksCountLastWeek()
        {
            return GetDashboardStatistics().OverdueBooksLastWeek;
        }
        public int GetTodaysBorrowingsCount()
        {
            return GetDashboardStatistics().TodaysBorrowings;
        }
        public int GetTodaysReturnsCount()
        {
            return GetDashboardStatistics().TodaysReturns;
        }
        public decimal GetPendingFinesAmount()
        {
            return GetDashboardStatistics().PendingFines;
        }
        public int GetReservationsCount()
        {
            return GetDashboardStatistics().Reservations;
        }
        public decimal CalculatePercentageChange(decimal current, decimal previous)
        {
            if (previous == 0) return current > 0 ? 100 : 0;
            return ((current - previous) / previous) * 100;
        }
        public string CalculatePercentageChangeFormatted(decimal current, decimal previous)
        {
            decimal change = CalculatePercentageChange(current, previous);
            return $"{(change >= 0 ? "+" : "")}{change:F1}%";
        }
    }
}
