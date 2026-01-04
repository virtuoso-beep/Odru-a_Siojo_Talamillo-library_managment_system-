USE LibraryManagementDB;
DELIMITER $$
DROP PROCEDURE IF EXISTS SP_GetAllBooks$$
CREATE PROCEDURE SP_GetAllBooks(
    IN p_SearchText VARCHAR(255),
    IN p_CategoryFilter VARCHAR(100)
)
BEGIN
    SET @search = CONCAT('%', IFNULL(p_SearchText, ''), '%');
    SELECT 
        BookId,
        Title,
        Author,
        ISBN,
        Publisher,
        PublicationYear,
        Category,
        TotalCopies,
        AvailableCopies,
        Location,
        Description,
        CreatedDate,
        UpdatedDate
    FROM Books
    WHERE (p_SearchText IS NULL OR p_SearchText = '' OR
           Title LIKE @search OR
           Author LIKE @search OR
           ISBN LIKE @search OR
           Publisher LIKE @search)
      AND (p_CategoryFilter IS NULL OR p_CategoryFilter = '' OR p_CategoryFilter = 'All Categories' OR Category = p_CategoryFilter)
    ORDER BY Title;
END$$
DROP PROCEDURE IF EXISTS SP_GetBookById$$
CREATE PROCEDURE SP_GetBookById(
    IN p_BookId INT
)
BEGIN
    SELECT 
        BookId,
        Title,
        Author,
        ISBN,
        Publisher,
        PublicationYear,
        Category,
        TotalCopies,
        AvailableCopies,
        Location,
        Description,
        CreatedDate,
        UpdatedDate
    FROM Books
    WHERE BookId = p_BookId;
END$$
DROP PROCEDURE IF EXISTS SP_AddBook$$
CREATE PROCEDURE SP_AddBook(
    IN p_Title VARCHAR(255),
    IN p_Author VARCHAR(255),
    IN p_ISBN VARCHAR(50),
    IN p_Publisher VARCHAR(255),
    IN p_PublicationYear INT,
    IN p_Category VARCHAR(100),
    IN p_TotalCopies INT,
    IN p_Location VARCHAR(100),
    IN p_Description TEXT
)
BEGIN
    INSERT INTO Books (
        Title, Author, ISBN, Publisher, PublicationYear,
        Category, TotalCopies, AvailableCopies, Location, Description,
        CreatedDate, UpdatedDate
    )
    VALUES (
        p_Title, p_Author, p_ISBN, p_Publisher, p_PublicationYear,
        p_Category, p_TotalCopies, p_TotalCopies, p_Location, p_Description,
        NOW(), NOW()
    );
    SELECT LAST_INSERT_ID() AS BookId;
END$$
DROP PROCEDURE IF EXISTS SP_UpdateBook$$
CREATE PROCEDURE SP_UpdateBook(
    IN p_BookId INT,
    IN p_Title VARCHAR(255),
    IN p_Author VARCHAR(255),
    IN p_ISBN VARCHAR(50),
    IN p_Publisher VARCHAR(255),
    IN p_PublicationYear INT,
    IN p_Category VARCHAR(100),
    IN p_TotalCopies INT,
    IN p_Location VARCHAR(100),
    IN p_Description TEXT
)
BEGIN
    DECLARE v_CurrentAvailable INT;
    DECLARE v_CurrentTotal INT;
    DECLARE v_Difference INT;
    SELECT TotalCopies, AvailableCopies INTO v_CurrentTotal, v_CurrentAvailable
    FROM Books WHERE BookId = p_BookId;
    SET v_Difference = p_TotalCopies - v_CurrentTotal;
    UPDATE Books
    SET Title = p_Title,
        Author = p_Author,
        ISBN = p_ISBN,
        Publisher = p_Publisher,
        PublicationYear = p_PublicationYear,
        Category = p_Category,
        TotalCopies = p_TotalCopies,
        AvailableCopies = v_CurrentAvailable + v_Difference,
        Location = p_Location,
        Description = p_Description,
        UpdatedDate = NOW()
    WHERE BookId = p_BookId;
    SELECT ROW_COUNT() AS RowsAffected;
END$$
DROP PROCEDURE IF EXISTS SP_DeleteBook$$
CREATE PROCEDURE SP_DeleteBook(
    IN p_BookId INT
)
BEGIN
    DECLARE v_ActiveBorrowings INT;
    SELECT COUNT(*) INTO v_ActiveBorrowings
    FROM Borrowings
    WHERE BookId = p_BookId AND ReturnDate IS NULL;
    IF v_ActiveBorrowings > 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Cannot delete book with active borrowings';
    END IF;
    DELETE FROM Books WHERE BookId = p_BookId;
    SELECT ROW_COUNT() AS RowsAffected;
END$$
DROP PROCEDURE IF EXISTS SP_GetBookCategories$$
CREATE PROCEDURE SP_GetBookCategories()
BEGIN
    SELECT DISTINCT Category
    FROM Books
    WHERE Category IS NOT NULL AND Category != ''
    ORDER BY Category;
END$$
DROP PROCEDURE IF EXISTS SP_SearchBooks$$
CREATE PROCEDURE SP_SearchBooks(
    IN p_Title VARCHAR(255),
    IN p_Author VARCHAR(255),
    IN p_ISBN VARCHAR(50),
    IN p_Category VARCHAR(100)
)
BEGIN
    SELECT 
        BookId,
        Title,
        Author,
        ISBN,
        Publisher,
        PublicationYear,
        Category,
        TotalCopies,
        AvailableCopies,
        Location
    FROM Books
    WHERE (p_Title IS NULL OR Title LIKE CONCAT('%', p_Title, '%'))
      AND (p_Author IS NULL OR Author LIKE CONCAT('%', p_Author, '%'))
      AND (p_ISBN IS NULL OR ISBN LIKE CONCAT('%', p_ISBN, '%'))
      AND (p_Category IS NULL OR Category = p_Category)
    ORDER BY Title;
END$$
DELIMITER ;
SELECT ROUTINE_NAME, CREATED
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'LibraryManagementDB'
  AND ROUTINE_TYPE = 'PROCEDURE'
  AND ROUTINE_NAME LIKE 'SP_%Book%'
ORDER BY ROUTINE_NAME;
