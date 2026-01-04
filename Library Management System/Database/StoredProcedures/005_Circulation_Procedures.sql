USE LibraryManagementDB;
DELIMITER $$
DROP PROCEDURE IF EXISTS SP_GetAllBorrowings$$
CREATE PROCEDURE SP_GetAllBorrowings(
    IN p_SearchText VARCHAR(255),
    IN p_StatusFilter VARCHAR(50)
)
BEGIN
    SET @search = CONCAT('%', IFNULL(p_SearchText, ''), '%');
    SELECT 
        b.BorrowingId,
        b.MemberId,
        m.MemberNumber,
        CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
        b.BookId,
        bk.Title AS BookTitle,
        bk.Author AS BookAuthor,
        b.BorrowDate,
        b.DueDate,
        b.ReturnDate,
        CASE 
            WHEN b.ReturnDate IS NOT NULL THEN 'Returned'
            WHEN b.DueDate < NOW() THEN 'Overdue'
            ELSE 'Active'
        END AS Status,
        DATEDIFF(NOW(), b.DueDate) AS DaysOverdue
    FROM Borrowings b
    INNER JOIN Members m ON b.MemberId = m.MemberId
    INNER JOIN Users u ON m.UserId = u.UserId
    INNER JOIN Books bk ON b.BookId = bk.BookId
    WHERE (p_SearchText IS NULL OR p_SearchText = '' OR
           m.MemberNumber LIKE @search OR
           u.FirstName LIKE @search OR
           u.LastName LIKE @search OR
           bk.Title LIKE @search)
      AND (p_StatusFilter IS NULL OR p_StatusFilter = 'All' OR
           (p_StatusFilter = 'Active' AND b.ReturnDate IS NULL AND b.DueDate >= NOW()) OR
           (p_StatusFilter = 'Overdue' AND b.ReturnDate IS NULL AND b.DueDate < NOW()) OR
           (p_StatusFilter = 'Returned' AND b.ReturnDate IS NOT NULL))
    ORDER BY b.BorrowDate DESC;
END$$
DROP PROCEDURE IF EXISTS SP_CheckoutBook$$
CREATE PROCEDURE SP_CheckoutBook(
    IN p_MemberId INT,
    IN p_BookId INT,
    IN p_DueDate DATE
)
BEGIN
    DECLARE v_Available INT;
    DECLARE v_MemberStatus INT;
    DECLARE v_ActiveBorrowings INT;
    DECLARE v_BorrowLimit INT DEFAULT 5;
    SELECT Status INTO v_MemberStatus
    FROM Members WHERE MemberId = p_MemberId;
    IF v_MemberStatus != 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Member account is not active';
    END IF;
    SELECT COUNT(*) INTO v_ActiveBorrowings
    FROM Borrowings
    WHERE MemberId = p_MemberId AND ReturnDate IS NULL;
    IF v_ActiveBorrowings >= v_BorrowLimit THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Member has reached borrowing limit';
    END IF;
    SELECT AvailableCopies INTO v_Available
    FROM Books WHERE BookId = p_BookId;
    IF v_Available < 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No copies available for checkout';
    END IF;
    INSERT INTO Borrowings (MemberId, BookId, BorrowDate, DueDate)
    VALUES (p_MemberId, p_BookId, NOW(), p_DueDate);
    UPDATE Books
    SET AvailableCopies = AvailableCopies - 1,
        UpdatedDate = NOW()
    WHERE BookId = p_BookId;
    SELECT LAST_INSERT_ID() AS BorrowingId;
END$$
DROP PROCEDURE IF EXISTS SP_ReturnBook$$
CREATE PROCEDURE SP_ReturnBook(
    IN p_BorrowingId INT
)
BEGIN
    DECLARE v_BookId INT;
    DECLARE v_DueDate DATE;
    DECLARE v_DaysOverdue INT;
    DECLARE v_FineAmount DECIMAL(10,2);
    DECLARE v_MemberId INT;
    SELECT BookId, DueDate, MemberId
    INTO v_BookId, v_DueDate, v_MemberId
    FROM Borrowings
    WHERE BorrowingId = p_BorrowingId AND ReturnDate IS NULL;
    IF v_BookId IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Borrowing record not found or already returned';
    END IF;
    UPDATE Borrowings
    SET ReturnDate = NOW()
    WHERE BorrowingId = p_BorrowingId;
    UPDATE Books
    SET AvailableCopies = AvailableCopies + 1,
        UpdatedDate = NOW()
    WHERE BookId = v_BookId;
    SET v_DaysOverdue = DATEDIFF(NOW(), v_DueDate);
    IF v_DaysOverdue > 0 THEN
        SET v_FineAmount = v_DaysOverdue * 5.00;
        INSERT INTO Fines (MemberId, BorrowingId, Amount, Reason, Status, CreatedDate)
        VALUES (v_MemberId, p_BorrowingId, v_FineAmount, 
                CONCAT('Overdue fine: ', v_DaysOverdue, ' days'), 
                'Pending', NOW());
    END IF;
    SELECT 
        p_BorrowingId AS BorrowingId,
        v_DaysOverdue AS DaysOverdue,
        v_FineAmount AS FineAmount;
END$$
DROP PROCEDURE IF EXISTS SP_RenewBook$$
CREATE PROCEDURE SP_RenewBook(
    IN p_BorrowingId INT,
    IN p_NewDueDate DATE
)
BEGIN
    DECLARE v_CurrentDueDate DATE;
    SELECT DueDate INTO v_CurrentDueDate
    FROM Borrowings
    WHERE BorrowingId = p_BorrowingId AND ReturnDate IS NULL;
    IF v_CurrentDueDate IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Borrowing record not found or already returned';
    END IF;
    UPDATE Borrowings
    SET DueDate = p_NewDueDate
    WHERE BorrowingId = p_BorrowingId;
    SELECT ROW_COUNT() AS RowsAffected, p_NewDueDate AS NewDueDate;
END$$
DROP PROCEDURE IF EXISTS SP_GetBorrowingById$$
CREATE PROCEDURE SP_GetBorrowingById(
    IN p_BorrowingId INT
)
BEGIN
    SELECT 
        b.BorrowingId,
        b.MemberId,
        m.MemberNumber,
        CONCAT(u.FirstName, ' ', u.LastName) AS MemberName,
        u.Email,
        b.BookId,
        bk.Title AS BookTitle,
        bk.Author AS BookAuthor,
        bk.ISBN,
        b.BorrowDate,
        b.DueDate,
        b.ReturnDate,
        CASE 
            WHEN b.ReturnDate IS NOT NULL THEN 'Returned'
            WHEN b.DueDate < NOW() THEN 'Overdue'
            ELSE 'Active'
        END AS Status,
        CASE
            WHEN b.ReturnDate IS NULL AND b.DueDate < NOW() THEN DATEDIFF(NOW(), b.DueDate)
            ELSE 0
        END AS DaysOverdue
    FROM Borrowings b
    INNER JOIN Members m ON b.MemberId = m.MemberId
    INNER JOIN Users u ON m.UserId = u.UserId
    INNER JOIN Books bk ON b.BookId = bk.BookId
    WHERE b.BorrowingId = p_BorrowingId;
END$$
DROP PROCEDURE IF EXISTS SP_GetMemberBorrowingHistory$$
CREATE PROCEDURE SP_GetMemberBorrowingHistory(
    IN p_MemberId INT
)
BEGIN
    SELECT 
        b.BorrowingId,
        bk.Title AS BookTitle,
        bk.Author AS BookAuthor,
        b.BorrowDate,
        b.DueDate,
        b.ReturnDate,
        CASE 
            WHEN b.ReturnDate IS NOT NULL THEN 'Returned'
            WHEN b.DueDate < NOW() THEN 'Overdue'
            ELSE 'Active'
        END AS Status
    FROM Borrowings b
    INNER JOIN Books bk ON b.BookId = bk.BookId
    WHERE b.MemberId = p_MemberId
    ORDER BY b.BorrowDate DESC;
END$$
DELIMITER ;
SELECT ROUTINE_NAME, CREATED
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'LibraryManagementDB'
  AND ROUTINE_TYPE = 'PROCEDURE'
  AND (ROUTINE_NAME LIKE 'SP_%Borrow%' OR ROUTINE_NAME LIKE 'SP_Checkout%' OR ROUTINE_NAME LIKE 'SP_Return%' OR ROUTINE_NAME LIKE 'SP_Renew%')
ORDER BY ROUTINE_NAME;
