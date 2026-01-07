USE LibraryManagementDB;
DROP PROCEDURE IF EXISTS SP_GetBookCategories;
DELIMITER $$
CREATE PROCEDURE SP_GetBookCategories()
BEGIN
    SELECT DISTINCT Category
    FROM Books
    WHERE Category IS NOT NULL AND Category != ''
    ORDER BY Category;
END$$
DELIMITER ;
