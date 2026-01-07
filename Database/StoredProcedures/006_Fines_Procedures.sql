USE LibraryManagementDB;
DELIMITER $$
DROP PROCEDURE IF EXISTS SP_GetAllFines$$
CREATE PROCEDURE SP_GetAllFines(
    IN p_SearchText VARCHAR(255),
    IN p_StatusFilter VARCHAR(50)
)
BEGIN
    SET @search = CONCAT('%', IFNULL(p_SearchText, ''), '%');
    SELECT 
        f.FineId,
        f.MemberId,
        m.MemberNumber,
        CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
        f.BorrowingId,
        f.Amount,
        f.Reason,
        f.Status,
        f.CreatedDate,
        f.PaidDate
    FROM Fines f
    INNER JOIN Members m ON f.MemberId = m.MemberId
    INNER JOIN Users u ON m.UserId = u.UserId
    WHERE (p_SearchText IS NULL OR p_SearchText = '' OR
           m.MemberNumber LIKE @search OR
           u.FirstName LIKE @search OR
           u.LastName LIKE @search)
      AND (p_StatusFilter IS NULL OR p_StatusFilter = 'All' OR f.Status = p_StatusFilter)
    ORDER BY f.CreatedDate DESC;
END$$
DROP PROCEDURE IF EXISTS SP_GetMemberFines$$
CREATE PROCEDURE SP_GetMemberFines(
    IN p_MemberId INT,
    IN p_StatusFilter VARCHAR(50)
)
BEGIN
    SELECT 
        f.FineId,
        f.Amount,
        f.Reason,
        f.Status,
        f.CreatedDate,
        f.PaidDate,
        f.BorrowingId,
        b.BookId,
        bk.Title AS BookTitle
    FROM Fines f
    LEFT JOIN Borrowings b ON f.BorrowingId = b.BorrowingId
    LEFT JOIN Books bk ON b.BookId = bk.BookId
    WHERE f.MemberId = p_MemberId
      AND (p_StatusFilter IS NULL OR p_StatusFilter = 'All' OR f.Status = p_StatusFilter)
    ORDER BY f.CreatedDate DESC;
END$$
DROP PROCEDURE IF EXISTS SP_ProcessFinePayment$$
CREATE PROCEDURE SP_ProcessFinePayment(
    IN p_FineId INT
)
BEGIN
    UPDATE Fines
    SET Status = 'Paid',
        PaidDate = NOW()
    WHERE FineId = p_FineId AND Status IN ('Pending', 'Unpaid');
    SELECT ROW_COUNT() AS RowsAffected;
END$$
DROP PROCEDURE IF EXISTS SP_WaiveFine$$
CREATE PROCEDURE SP_WaiveFine(
    IN p_FineId INT
)
BEGIN
    UPDATE Fines
    SET Status = 'Waived',
        PaidDate = NOW()
    WHERE FineId = p_FineId;
    SELECT ROW_COUNT() AS RowsAffected;
END$$
DROP PROCEDURE IF EXISTS SP_GetTotalFines$$
CREATE PROCEDURE SP_GetTotalFines(
    IN p_MemberId INT
)
BEGIN
    SELECT 
        COALESCE(SUM(Amount), 0) AS TotalFines,
        COUNT(*) AS FineCount
    FROM Fines
    WHERE MemberId = p_MemberId
      AND Status IN ('Pending', 'Unpaid');
END$$
DROP PROCEDURE IF EXISTS SP_CalculateOverdueFines$$
CREATE PROCEDURE SP_CalculateOverdueFines()
BEGIN
    DECLARE v_FineRate DECIMAL(10,2) DEFAULT 5.00;
    DECLARE v_ProcessedCount INT DEFAULT 0;
    INSERT INTO Fines (MemberId, BorrowingId, Amount, Reason, Status, CreatedDate)
    SELECT 
        b.MemberId,
        b.BorrowingId,
        DATEDIFF(NOW(), b.DueDate) * v_FineRate AS Amount,
        CONCAT('Overdue fine: ', DATEDIFF(NOW(), b.DueDate), ' days') AS Reason,
        'Pending' AS Status,
        NOW() AS CreatedDate
    FROM Borrowings b
    WHERE b.ReturnDate IS NULL
      AND b.DueDate < NOW()
      AND NOT EXISTS (
          SELECT 1 FROM Fines f 
          WHERE f.BorrowingId = b.BorrowingId
      );
    SET v_ProcessedCount = ROW_COUNT();
    SELECT v_ProcessedCount AS FinesCreated;
END$$
DROP PROCEDURE IF EXISTS SP_AddFine$$
CREATE PROCEDURE SP_AddFine(
    IN p_MemberId INT,
    IN p_BorrowingId INT,
    IN p_Amount DECIMAL(10,2),
    IN p_Reason TEXT
)
BEGIN
    INSERT INTO Fines (MemberId, BorrowingId, Amount, Reason, Status, CreatedDate)
    VALUES (p_MemberId, p_BorrowingId, p_Amount, p_Reason, 'Pending', NOW());
    SELECT LAST_INSERT_ID() AS FineId;
END$$
DROP PROCEDURE IF EXISTS SP_GetFineById$$
CREATE PROCEDURE SP_GetFineById(
    IN p_FineId INT
)
BEGIN
    SELECT 
        f.FineId,
        f.MemberId,
        m.MemberNumber,
        CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
        f.BorrowingId,
        f.Amount,
        f.Reason,
        f.Status,
        f.CreatedDate,
        f.PaidDate,
        b.BookId,
        bk.Title AS BookTitle,
        b.BorrowDate,
        b.DueDate,
        b.ReturnDate
    FROM Fines f
    INNER JOIN Members m ON f.MemberId = m.MemberId
    INNER JOIN Users u ON m.UserId = u.UserId
    LEFT JOIN Borrowings b ON f.BorrowingId = b.BorrowingId
    LEFT JOIN Books bk ON b.BookId = bk.BookId
    WHERE f.FineId = p_FineId;
END$$
DELIMITER ;
SELECT ROUTINE_NAME, CREATED
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'LibraryManagementDB'
  AND ROUTINE_TYPE = 'PROCEDURE'
  AND ROUTINE_NAME LIKE 'SP_%Fine%'
ORDER BY ROUTINE_NAME;
