USE LibraryManagementDB;
DELIMITER $$
DROP PROCEDURE IF EXISTS SP_GetDashboardStatistics$$
CREATE PROCEDURE SP_GetDashboardStatistics()
BEGIN
    SELECT 
        (SELECT COUNT(*) FROM Members 
         WHERE Status = 1 AND (MembershipExpiryDate IS NULL OR MembershipExpiryDate > NOW())) AS ActiveMembers,
        (SELECT COUNT(*) FROM Members 
         WHERE Status = 1 AND (MembershipExpiryDate IS NULL OR MembershipExpiryDate > DATE_SUB(NOW(), INTERVAL 7 DAY))
         AND RegistrationDate <= DATE_SUB(NOW(), INTERVAL 7 DAY)) AS ActiveMembersLastWeek,
        (SELECT COALESCE(SUM(TotalCopies), 0) FROM Books) AS TotalBooks,
        (SELECT COALESCE(SUM(TotalCopies), 0) FROM Books 
         WHERE CreatedDate <= DATE_SUB(NOW(), INTERVAL 7 DAY)) AS TotalBooksLastWeek,
        (SELECT COUNT(*) FROM Borrowings WHERE ReturnDate IS NULL) AS BooksBorrowed,
        (SELECT COUNT(*) FROM Borrowings 
         WHERE BorrowDate <= DATE_SUB(NOW(), INTERVAL 7 DAY) 
         AND MONTH(BorrowDate) = MONTH(DATE_SUB(NOW(), INTERVAL 7 DAY))) AS BooksBorrowedLastWeek,
        (SELECT COUNT(*) FROM Borrowings 
         WHERE ReturnDate IS NULL AND DueDate < NOW()) AS OverdueBooks,
        (SELECT COUNT(*) FROM Borrowings 
         WHERE ReturnDate IS NULL AND DueDate < DATE_SUB(NOW(), INTERVAL 7 DAY)) AS OverdueBooksLastWeek,
        (SELECT COUNT(*) FROM Borrowings WHERE DATE(BorrowDate) = CURDATE()) AS TodaysBorrowings,
        (SELECT COUNT(*) FROM Borrowings WHERE DATE(ReturnDate) = CURDATE()) AS TodaysReturns,
        (SELECT COALESCE(SUM(Amount), 0) FROM Fines WHERE Status IN ('Pending', 'Unpaid')) AS PendingFines,
        (SELECT COUNT(*) FROM Reservations WHERE Status = 'Active') AS ActiveReservations;
END$$
DROP PROCEDURE IF EXISTS SP_GetRecentActivities$$
CREATE PROCEDURE SP_GetRecentActivities(
    IN p_Limit INT
)
BEGIN
    (SELECT 
        'Borrow' AS ActivityType,
        b.BorrowDate AS ActivityDate,
        CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
        bk.Title AS BookTitle,
        m.MemberNumber
    FROM Borrowings b
    INNER JOIN Members m ON b.MemberId = m.MemberId
    INNER JOIN Users u ON m.UserId = u.UserId
    INNER JOIN Books bk ON b.BookId = bk.BookId
    WHERE b.BorrowDate >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    ORDER BY b.BorrowDate DESC
    LIMIT p_Limit)
    UNION ALL
    (SELECT 
        'Return' AS ActivityType,
        b.ReturnDate AS ActivityDate,
        CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
        bk.Title AS BookTitle,
        m.MemberNumber
    FROM Borrowings b
    INNER JOIN Members m ON b.MemberId = m.MemberId
    INNER JOIN Users u ON m.UserId = u.UserId
    INNER JOIN Books bk ON b.BookId = bk.BookId
    WHERE b.ReturnDate >= DATE_SUB(NOW(), INTERVAL 7 DAY)
    ORDER BY b.ReturnDate DESC
    LIMIT p_Limit)
    ORDER BY ActivityDate DESC
    LIMIT p_Limit;
END$$
DROP PROCEDURE IF EXISTS SP_GetMonthlyStatistics$$
CREATE PROCEDURE SP_GetMonthlyStatistics(
    IN p_Year INT,
    IN p_Month INT
)
BEGIN
    SELECT 
        (SELECT COUNT(*) FROM Members 
         WHERE YEAR(RegistrationDate) = p_Year AND MONTH(RegistrationDate) = p_Month) AS NewMembers,
        (SELECT COUNT(*) FROM Books 
         WHERE YEAR(CreatedDate) = p_Year AND MONTH(CreatedDate) = p_Month) AS BooksAdded,
        (SELECT COUNT(*) FROM Borrowings 
         WHERE YEAR(BorrowDate) = p_Year AND MONTH(BorrowDate) = p_Month) AS TotalBorrowings,
        (SELECT COUNT(*) FROM Borrowings 
         WHERE YEAR(ReturnDate) = p_Year AND MONTH(ReturnDate) = p_Month) AS TotalReturns,
        (SELECT COALESCE(SUM(Amount), 0) FROM Fines 
         WHERE Status = 'Paid' AND YEAR(PaidDate) = p_Year AND MONTH(PaidDate) = p_Month) AS FinesCollected;
END$$
DROP PROCEDURE IF EXISTS SP_GetPopularBooks$$
CREATE PROCEDURE SP_GetPopularBooks(
    IN p_Limit INT,
    IN p_DaysRange INT
)
BEGIN
    SELECT 
        b.BookId,
        b.Title,
        b.Author,
        b.Category,
        COUNT(br.BorrowingId) AS BorrowCount,
        b.AvailableCopies,
        b.TotalCopies
    FROM Books b
    INNER JOIN Borrowings br ON b.BookId = br.BookId
    WHERE br.BorrowDate >= DATE_SUB(NOW(), INTERVAL p_DaysRange DAY)
    GROUP BY b.BookId, b.Title, b.Author, b.Category, b.AvailableCopies, b.TotalCopies
    ORDER BY BorrowCount DESC
    LIMIT p_Limit;
END$$
DROP PROCEDURE IF EXISTS SP_GetActiveMembers$$
CREATE PROCEDURE SP_GetActiveMembers(
    IN p_Limit INT,
    IN p_DaysRange INT
)
BEGIN
    SELECT 
        m.MemberId,
        m.MemberNumber,
        CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
        u.Email,
        COUNT(b.BorrowingId) AS BorrowCount,
        COALESCE(SUM(CASE WHEN b.ReturnDate IS NULL AND b.DueDate < NOW() THEN 1 ELSE 0 END), 0) AS OverdueCount
    FROM Members m
    INNER JOIN Users u ON m.UserId = u.UserId
    INNER JOIN Borrowings b ON m.MemberId = b.MemberId
    WHERE b.BorrowDate >= DATE_SUB(NOW(), INTERVAL p_DaysRange DAY)
    GROUP BY m.MemberId, m.MemberNumber, u.FirstName, u.LastName, u.Email
    ORDER BY BorrowCount DESC
    LIMIT p_Limit;
END$$
DELIMITER ;
SELECT ROUTINE_NAME, CREATED
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'LibraryManagementDB'
  AND ROUTINE_TYPE = 'PROCEDURE'
  AND (ROUTINE_NAME LIKE 'SP_GetDashboard%' OR ROUTINE_NAME LIKE 'SP_GetRecent%' OR 
       ROUTINE_NAME LIKE 'SP_GetMonthly%' OR ROUTINE_NAME LIKE 'SP_GetPopular%' OR
       ROUTINE_NAME LIKE 'SP_GetActive%')
ORDER BY ROUTINE_NAME;
