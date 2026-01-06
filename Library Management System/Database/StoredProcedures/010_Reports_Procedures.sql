USE LibraryManagementDB;
DELIMITER $$

-- Daily Circulation Report
DROP PROCEDURE IF EXISTS SP_GetDailyCirculationReport$$
CREATE PROCEDURE SP_GetDailyCirculationReport(
    IN p_Date DATE
)
BEGIN
    SELECT 
        DATE(BorrowDate) AS ReportDate,
        COUNT(CASE WHEN ReturnDate IS NULL THEN 1 END) AS TotalBorrowed,
        COUNT(CASE WHEN ReturnDate IS NOT NULL THEN 1 END) AS TotalReturned,
        COUNT(CASE WHEN ReturnDate IS NULL AND DueDate < NOW() THEN 1 END) AS Overdue
    FROM Borrowings
    WHERE DATE(BorrowDate) = p_Date OR DATE(ReturnDate) = p_Date
    GROUP BY DATE(BorrowDate);
END$$

-- Popular Books Report
DROP PROCEDURE IF EXISTS SP_GetPopularBooksReport$$
CREATE PROCEDURE SP_GetPopularBooksReport(
    IN p_Limit INT,
    IN p_StartDate DATE,
    IN p_EndDate DATE
)
BEGIN
    SELECT 
        b.BookId,
        b.Title,
        b.Author,
        b.Category,
        COUNT(br.BorrowingId) AS BorrowCount,
        b.TotalCopies,
        b.AvailableCopies
    FROM Books b
    INNER JOIN Borrowings br ON b.BookId = br.BookId
    WHERE br.BorrowDate BETWEEN p_StartDate AND p_EndDate
    GROUP BY b.BookId, b.Title, b.Author, b.Category, b.TotalCopies, b.AvailableCopies
    ORDER BY BorrowCount DESC
    LIMIT p_Limit;
END$$

-- Overdue Books Report
DROP PROCEDURE IF EXISTS SP_GetOverdueBooksReport$$
CREATE PROCEDURE SP_GetOverdueBooksReport()
BEGIN
    SELECT 
        br.BorrowingId,
        br.MemberId,
        br.BookId,
        b.Title,
        b.Author,
        br.BorrowDate,
        br.DueDate,
        DATEDIFF(NOW(), br.DueDate) AS DaysOverdue,
        u.Email,
        CONCAT(u.FirstName, ' ', u.LastName) AS MemberName
    FROM Borrowings br
    INNER JOIN Books b ON br.BookId = b.BookId
    INNER JOIN Members m ON br.MemberId = m.MemberId
    INNER JOIN Users u ON m.UserId = u.UserId
    WHERE br.ReturnDate IS NULL
      AND br.DueDate < NOW()
    ORDER BY DaysOverdue DESC;
END$$

-- Member Type Distribution Report
DROP PROCEDURE IF EXISTS SP_GetMemberTypeDistribution$$
CREATE PROCEDURE SP_GetMemberTypeDistribution()
BEGIN
    SELECT 
        MemberType,
        COUNT(*) AS MemberCount,
        COUNT(CASE WHEN Status = 1 THEN 1 END) AS ActiveCount,
        COUNT(CASE WHEN Status != 1 THEN 1 END) AS InactiveCount
    FROM Members
    GROUP BY MemberType
    ORDER BY MemberCount DESC;
END$$

-- Member Activity Summary Report
DROP PROCEDURE IF EXISTS SP_GetMemberActivitySummary$$
CREATE PROCEDURE SP_GetMemberActivitySummary(
    IN p_StartDate DATE,
    IN p_EndDate DATE
)
BEGIN
    SELECT 
        m.MemberId,
        m.MemberNumber,
        CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
        u.Email,
        m.MemberType,
        COUNT(br.BorrowingId) AS TotalBorrowings,
        COUNT(CASE WHEN br.ReturnDate IS NULL THEN 1 END) AS ActiveBorrowings,
        COUNT(CASE WHEN br.ReturnDate IS NOT NULL THEN 1 END) AS CompletedBorrowings,
        COALESCE(SUM(f.Amount), 0) AS TotalFines
    FROM Members m
    INNER JOIN Users u ON m.UserId = u.UserId
    LEFT JOIN Borrowings br ON m.MemberId = br.MemberId 
        AND br.BorrowDate BETWEEN p_StartDate AND p_EndDate
    LEFT JOIN Fines f ON m.MemberId = f.MemberId
    GROUP BY m.MemberId, m.MemberNumber, u.FirstName, u.LastName, u.Email, m.MemberType
    ORDER BY TotalBorrowings DESC;
END$$

-- Collection Statistics Report
DROP PROCEDURE IF EXISTS SP_GetCollectionStatistics$$
CREATE PROCEDURE SP_GetCollectionStatistics()
BEGIN
    SELECT 
        COUNT(*) AS TotalBooks,
        SUM(TotalCopies) AS TotalCopies,
        SUM(AvailableCopies) AS AvailableCopies,
        SUM(TotalCopies - AvailableCopies) AS BorrowedCopies,
        COUNT(DISTINCT Category) AS TotalCategories,
        COUNT(DISTINCT Author) AS TotalAuthors
    FROM Books;
END$$

-- Collection by Category Report
DROP PROCEDURE IF EXISTS SP_GetCollectionByCategory$$
CREATE PROCEDURE SP_GetCollectionByCategory()
BEGIN
    SELECT 
        COALESCE(b.Category, 'Uncategorized') AS Category,
        COUNT(*) AS BookCount,
        SUM(b.TotalCopies) AS TotalCopies,
        SUM(b.AvailableCopies) AS AvailableCopies
    FROM Books b
    GROUP BY b.Category
    ORDER BY BookCount DESC;
END$$

-- Fine Report
DROP PROCEDURE IF EXISTS SP_GetFineReport$$
CREATE PROCEDURE SP_GetFineReport(
    IN p_StatusFilter VARCHAR(20)
)
BEGIN
    SELECT 
        f.FineId,
        f.MemberId,
        m.MemberNumber,
        CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
        u.Email,
        f.Amount,
        f.Status,
        f.CreatedDate,
        f.PaidDate,
        f.Reason,
        COALESCE(SUM(CASE WHEN f.Status = 'Unpaid' THEN f.Amount ELSE 0 END), 0) AS TotalUnpaid,
        COALESCE(SUM(CASE WHEN f.Status = 'Paid' THEN f.Amount ELSE 0 END), 0) AS TotalPaid,
        COALESCE(SUM(CASE WHEN f.Status = 'Waived' THEN f.Amount ELSE 0 END), 0) AS TotalWaived
    FROM Fines f
    INNER JOIN Members m ON f.MemberId = m.MemberId
    INNER JOIN Users u ON m.UserId = u.UserId
    WHERE (p_StatusFilter IS NULL OR p_StatusFilter = '' OR f.Status = p_StatusFilter)
    GROUP BY f.FineId, f.MemberId, m.MemberNumber, u.FirstName, u.LastName, 
             u.Email, f.Amount, f.Status, f.CreatedDate, f.PaidDate, f.Reason
    ORDER BY f.CreatedDate DESC;
END$$

DELIMITER ;

