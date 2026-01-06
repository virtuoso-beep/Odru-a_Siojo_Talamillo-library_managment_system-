using System;
using System.Collections.Generic;
using System.Data;
using Library_Management_System.Helper;
using MySql.Data.MySqlClient;
namespace Library_Management_System.Service
{
    public class ReportsService
    {
        public class CirculationDataPoint
        {
            public string Date { get; set; }
            public int Borrowings { get; set; }
            public int Returns { get; set; }
        }
        public class PopularBook
        {
            public string BookId { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public int BorrowCount { get; set; }
            public int AvailableCopies { get; set; }
            public int TotalCopies { get; set; }
        }
        public class OverdueBook
        {
            public string BookTitle { get; set; }
            public string MemberName { get; set; }
            public DateTime BorrowDate { get; set; }
            public DateTime DueDate { get; set; }
            public int DaysOverdue { get; set; }
        }
        public class MemberTypeDistribution
        {
            public string MemberType { get; set; }
            public int Count { get; set; }
        }
        public class MemberActivitySummary
        {
            public int TotalMembers { get; set; }
            public int ActiveMembers { get; set; }
            public int SuspendedMembers { get; set; }
            public int ExpiredMembers { get; set; }
            public int NewMembersThisMonth { get; set; }
            public int MembersWithActiveBorrowings { get; set; }
        }
        public class CollectionStatistic
        {
            public int TotalBooks { get; set; }
            public int TotalCopies { get; set; }
            public int AvailableCopies { get; set; }
            public int BorrowedCopies { get; set; }
            public int BooksByCategory { get; set; }
            public string CategoryName { get; set; }
        }
        public class FineReportData
        {
            public decimal TotalUnpaidFines { get; set; }
            public decimal TotalPaidFines { get; set; }
            public int UnpaidFineCount { get; set; }
            public int PaidFineCount { get; set; }
            public decimal MonthlyRevenue { get; set; }
            public List<FineByMember> FinesByMember { get; set; }
        }
        public class FineByMember
        {
            public string MemberName { get; set; }
            public string MemberNumber { get; set; }
            public decimal TotalFines { get; set; }
            public int FineCount { get; set; }
        }
        // Circulation Reports
        public List<CirculationDataPoint> GetDailyCirculationData(DateTime startDate, DateTime endDate)
        {
            var dataPoints = new List<CirculationDataPoint>();
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            DATE(BorrowDate) as Date,
                            COUNT(*) as Borrowings
                        FROM Borrowings
                        WHERE DATE(BorrowDate) BETWEEN @startDate AND @endDate
                        GROUP BY DATE(BorrowDate)
                        ORDER BY Date";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@startDate", startDate.Date);
                        command.Parameters.AddWithValue("@endDate", endDate.Date);
                        var borrowingsDict = new Dictionary<DateTime, int>();
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime date = Convert.ToDateTime(reader["Date"]).Date;
                                int count = Convert.ToInt32(reader["Borrowings"]);
                                borrowingsDict[date] = count;
                            }
                        }
                        string returnQuery = @"
                            SELECT 
                                DATE(ReturnDate) as Date,
                                COUNT(*) as Returns
                            FROM Borrowings
                            WHERE ReturnDate IS NOT NULL 
                            AND DATE(ReturnDate) BETWEEN @startDate AND @endDate
                            GROUP BY DATE(ReturnDate)
                            ORDER BY Date";
                        using (var returnCommand = new MySqlCommand(returnQuery, connection))
                        {
                            returnCommand.Parameters.AddWithValue("@startDate", startDate.Date);
                            returnCommand.Parameters.AddWithValue("@endDate", endDate.Date);
                            var returnsDict = new Dictionary<DateTime, int>();
                            using (var reader = returnCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    DateTime date = Convert.ToDateTime(reader["Date"]).Date;
                                    int count = Convert.ToInt32(reader["Returns"]);
                                    returnsDict[date] = count;
                                }
                            }
                            for (DateTime date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                            {
                                dataPoints.Add(new CirculationDataPoint
                                {
                                    Date = date.ToString("MMM dd"),
                                    Borrowings = borrowingsDict.ContainsKey(date) ? borrowingsDict[date] : 0,
                                    Returns = returnsDict.ContainsKey(date) ? returnsDict[date] : 0
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, "GetDailyCirculationData");
                System.Diagnostics.Debug.WriteLine($"Error getting daily circulation data: {ex.Message}");
            }
            return dataPoints;
        }
        public List<PopularBook> GetPopularBooks(int limit = 10, int daysRange = 30)
        {
            var books = new List<PopularBook>();
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    using (var command = StoredProcedureHelper.CreateCommand("SP_GetPopularBooks", connection))
                    {
                        StoredProcedureHelper.AddParameter(command, "p_Limit", limit);
                        StoredProcedureHelper.AddParameter(command, "p_DaysRange", daysRange);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                books.Add(new PopularBook
                                {
                                    BookId = reader["BookId"].ToString(),
                                    Title = reader["Title"].ToString(),
                                    Author = reader["Author"]?.ToString(),
                                    BorrowCount = Convert.ToInt32(reader["BorrowCount"]),
                                    AvailableCopies = Convert.ToInt32(reader["AvailableCopies"]),
                                    TotalCopies = Convert.ToInt32(reader["TotalCopies"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, "GetPopularBooks");
                System.Diagnostics.Debug.WriteLine($"Error getting popular books: {ex.Message}");
            }
            return books;
        }
        public List<OverdueBook> GetOverdueBooks()
        {
            var overdueBooks = new List<OverdueBook>();
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            bk.Title AS BookTitle,
                            CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
                            b.BorrowDate,
                            b.DueDate,
                            DATEDIFF(NOW(), b.DueDate) AS DaysOverdue
                        FROM Borrowings b
                        INNER JOIN Books bk ON b.BookId = bk.BookId
                        INNER JOIN Members m ON b.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        WHERE b.ReturnDate IS NULL 
                        AND b.DueDate < NOW()
                        ORDER BY DaysOverdue DESC";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                overdueBooks.Add(new OverdueBook
                                {
                                    BookTitle = reader["BookTitle"].ToString(),
                                    MemberName = reader["MemberName"].ToString(),
                                    BorrowDate = Convert.ToDateTime(reader["BorrowDate"]),
                                    DueDate = Convert.ToDateTime(reader["DueDate"]),
                                    DaysOverdue = Convert.ToInt32(reader["DaysOverdue"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, "GetOverdueBooks");
                System.Diagnostics.Debug.WriteLine($"Error getting overdue books: {ex.Message}");
            }
            return overdueBooks;
        }
        // Member Reports
        public List<MemberTypeDistribution> GetMemberTypeDistribution()
        {
            var distribution = new List<MemberTypeDistribution>();
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            CASE 
                                WHEN MemberType = 1 THEN 'Student'
                                WHEN MemberType = 2 THEN 'Faculty'
                                WHEN MemberType = 3 THEN 'Staff'
                                ELSE 'Guest'
                            END AS MemberType,
                            COUNT(*) AS Count
                        FROM Members
                        GROUP BY MemberType
                        ORDER BY Count DESC";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                distribution.Add(new MemberTypeDistribution
                                {
                                    MemberType = reader["MemberType"].ToString(),
                                    Count = Convert.ToInt32(reader["Count"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, "GetMemberTypeDistribution");
                System.Diagnostics.Debug.WriteLine($"Error getting member type distribution: {ex.Message}");
            }
            return distribution;
        }
        public MemberActivitySummary GetMemberActivitySummary()
        {
            var summary = new MemberActivitySummary();
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
                                summary.TotalMembers = Convert.ToInt32(reader["TotalMembers"]);
                                summary.ActiveMembers = Convert.ToInt32(reader["ActiveMembers"]);
                                summary.SuspendedMembers = Convert.ToInt32(reader["SuspendedMembers"]);
                                summary.ExpiredMembers = Convert.ToInt32(reader["ExpiredMembers"]);
                            }
                        }
                    }
                    string newMembersQuery = @"
                        SELECT COUNT(*) 
                        FROM Members 
                        WHERE YEAR(RegistrationDate) = YEAR(NOW()) 
                        AND MONTH(RegistrationDate) = MONTH(NOW())";
                    using (var command = new MySqlCommand(newMembersQuery, connection))
                    {
                        var result = command.ExecuteScalar();
                        summary.NewMembersThisMonth = result != null ? Convert.ToInt32(result) : 0;
                    }
                    string activeBorrowingsQuery = @"
                        SELECT COUNT(DISTINCT MemberId) 
                        FROM Borrowings 
                        WHERE ReturnDate IS NULL";
                    using (var command = new MySqlCommand(activeBorrowingsQuery, connection))
                    {
                        var result = command.ExecuteScalar();
                        summary.MembersWithActiveBorrowings = result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, "GetMemberActivitySummary");
                System.Diagnostics.Debug.WriteLine($"Error getting member activity summary: {ex.Message}");
            }
            return summary;
        }
        // Collection Reports
        public CollectionStatistic GetCollectionStatistics()
        {
            var stats = new CollectionStatistic();
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            COUNT(DISTINCT BookId) AS TotalBooks,
                            SUM(TotalCopies) AS TotalCopies,
                            SUM(AvailableCopies) AS AvailableCopies
                        FROM Books";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                stats.TotalBooks = Convert.ToInt32(reader["TotalBooks"]);
                                stats.TotalCopies = Convert.ToInt32(reader["TotalCopies"]);
                                stats.AvailableCopies = Convert.ToInt32(reader["AvailableCopies"]);
                                stats.BorrowedCopies = stats.TotalCopies - stats.AvailableCopies;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, "GetCollectionStatistics");
                System.Diagnostics.Debug.WriteLine($"Error getting collection statistics: {ex.Message}");
            }
            return stats;
        }
        public List<CollectionStatistic> GetCollectionByCategory()
        {
            var stats = new List<CollectionStatistic>();
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            COALESCE(c.CategoryName, 'Uncategorized') AS CategoryName,
                            COUNT(DISTINCT b.BookId) AS BooksByCategory,
                            SUM(b.TotalCopies) AS TotalCopies,
                            SUM(b.AvailableCopies) AS AvailableCopies
                        FROM Books b
                        LEFT JOIN Categories c ON b.CategoryId = c.CategoryId
                        GROUP BY c.CategoryId, c.CategoryName
                        ORDER BY BooksByCategory DESC";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                stats.Add(new CollectionStatistic
                                {
                                    CategoryName = reader["CategoryName"].ToString(),
                                    BooksByCategory = Convert.ToInt32(reader["BooksByCategory"]),
                                    TotalCopies = Convert.ToInt32(reader["TotalCopies"]),
                                    AvailableCopies = Convert.ToInt32(reader["AvailableCopies"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, "GetCollectionByCategory");
                System.Diagnostics.Debug.WriteLine($"Error getting collection by category: {ex.Message}");
            }
            return stats;
        }
        // Fines Reports
        public FineReportData GetFineReportData(DateTime? startDate = null, DateTime? endDate = null)
        {
            var reportData = new FineReportData
            {
                FinesByMember = new List<FineByMember>()
            };
            try
            {
                using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
                {
                    connection.Open();
                    string unpaidQuery = @"
                        SELECT 
                            COALESCE(SUM(Amount), 0) AS TotalUnpaid,
                            COUNT(*) AS UnpaidCount
                        FROM Fines
                        WHERE Status IN ('Pending', 'Unpaid')";
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        unpaidQuery += " AND CreatedDate BETWEEN @startDate AND @endDate";
                    }
                    using (var command = new MySqlCommand(unpaidQuery, connection))
                    {
                        if (startDate.HasValue && endDate.HasValue)
                        {
                            command.Parameters.AddWithValue("@startDate", startDate.Value);
                            command.Parameters.AddWithValue("@endDate", endDate.Value);
                        }
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                reportData.TotalUnpaidFines = Convert.ToDecimal(reader["TotalUnpaid"]);
                                reportData.UnpaidFineCount = Convert.ToInt32(reader["UnpaidCount"]);
                            }
                        }
                    }
                    string paidQuery = @"
                        SELECT 
                            COALESCE(SUM(Amount), 0) AS TotalPaid,
                            COUNT(*) AS PaidCount
                        FROM Fines
                        WHERE Status = 'Paid'";
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        paidQuery += " AND PaidDate BETWEEN @startDate AND @endDate";
                    }
                    using (var command = new MySqlCommand(paidQuery, connection))
                    {
                        if (startDate.HasValue && endDate.HasValue)
                        {
                            command.Parameters.AddWithValue("@startDate", startDate.Value);
                            command.Parameters.AddWithValue("@endDate", endDate.Value);
                        }
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                reportData.TotalPaidFines = Convert.ToDecimal(reader["TotalPaid"]);
                                reportData.PaidFineCount = Convert.ToInt32(reader["PaidCount"]);
                            }
                        }
                    }
                    string monthlyQuery = @"
                        SELECT COALESCE(SUM(Amount), 0) AS MonthlyRevenue
                        FROM Fines
                        WHERE Status = 'Paid'
                        AND YEAR(PaidDate) = YEAR(NOW())
                        AND MONTH(PaidDate) = MONTH(NOW())";
                    using (var command = new MySqlCommand(monthlyQuery, connection))
                    {
                        var result = command.ExecuteScalar();
                        reportData.MonthlyRevenue = result != null ? Convert.ToDecimal(result) : 0;
                    }
                    string byMemberQuery = @"
                        SELECT 
                            CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
                            m.MemberNumber,
                            COALESCE(SUM(f.Amount), 0) AS TotalFines,
                            COUNT(*) AS FineCount
                        FROM Fines f
                        INNER JOIN Members m ON f.MemberId = m.MemberId
                        INNER JOIN Users u ON m.UserId = u.UserId
                        WHERE f.Status IN ('Pending', 'Unpaid')
                        GROUP BY m.MemberId, u.FirstName, u.LastName, m.MemberNumber
                        HAVING TotalFines > 0
                        ORDER BY TotalFines DESC
                        LIMIT 20";
                    using (var command = new MySqlCommand(byMemberQuery, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                reportData.FinesByMember.Add(new FineByMember
                                {
                                    MemberName = reader["MemberName"].ToString(),
                                    MemberNumber = reader["MemberNumber"].ToString(),
                                    TotalFines = Convert.ToDecimal(reader["TotalFines"]),
                                    FineCount = Convert.ToInt32(reader["FineCount"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.ErrorHandler.LogError(ex, "GetFineReportData");
                System.Diagnostics.Debug.WriteLine($"Error getting fine report data: {ex.Message}");
            }
            return reportData;
        }
    }
}
