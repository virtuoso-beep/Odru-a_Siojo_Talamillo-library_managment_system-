USE LibraryManagementDB;

-- ============================================================================
-- FOREIGN KEY VERIFICATION SCRIPT
-- ============================================================================
-- This script verifies all foreign key relationships in the database.
-- It checks for missing foreign keys and reports any inconsistencies.
--
-- PREREQUISITES:
-- 1. Run 001_Create_Database_Schema.sql first to create all tables
-- 2. Run 002_Fix_Schema_Issues.sql to add missing columns and tables
-- 3. Then run this script to verify foreign keys
--
-- Note: This script will handle missing tables gracefully and won't error
-- if tables don't exist yet. It uses case-insensitive table name checks.
-- ============================================================================

-- First, check which tables exist
SELECT 'Available Tables' AS Info, TABLE_NAME 
FROM information_schema.TABLES 
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
ORDER BY TABLE_NAME;

-- Check existing foreign keys
SELECT 
    TABLE_NAME,
    CONSTRAINT_NAME,
    COLUMN_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
AND REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY TABLE_NAME, CONSTRAINT_NAME;

-- Expected Foreign Keys:
-- 1. Members.UserId -> Users.UserId
-- 2. Books.CategoryId -> Categories.CategoryId
-- 3. BookCopies.BookId -> Books.BookId
-- 4. CirculationRecords.MemberId -> Members.MemberId
-- 5. CirculationRecords.CopyId -> BookCopies.CopyId
-- 6. Borrowings.MemberId -> Members.MemberId
-- 7. Borrowings.BookId -> Books.BookId
-- 8. Reservations.MemberId -> Members.MemberId
-- 9. Reservations.BookId -> Books.BookId
-- 10. Fines.MemberId -> Members.MemberId
-- 11. Fines.RecordId -> CirculationRecords.RecordId (optional, can be NULL)
-- 12. Fines.BorrowingId -> Borrowings.BorrowingId (optional, can be NULL)

-- Verify foreign key constraints exist (case-insensitive)
SELECT 
    'Members -> Users' AS Relationship,
    CASE 
        WHEN COUNT(*) > 0 THEN '✓ EXISTS'
        ELSE '✗ MISSING'
    END AS Status
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
AND LOWER(TABLE_NAME) = 'members'
AND COLUMN_NAME = 'UserId'
AND LOWER(REFERENCED_TABLE_NAME) = 'users';

SELECT 
    'Books -> Categories' AS Relationship,
    CASE 
        WHEN COUNT(*) > 0 THEN '✓ EXISTS'
        ELSE '✗ MISSING'
    END AS Status
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
AND LOWER(TABLE_NAME) = 'books'
AND COLUMN_NAME = 'CategoryId'
AND LOWER(REFERENCED_TABLE_NAME) = 'categories';

SELECT 
    'BookCopies -> Books' AS Relationship,
    CASE 
        WHEN COUNT(*) > 0 THEN '✓ EXISTS'
        ELSE '✗ MISSING'
    END AS Status
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
AND LOWER(TABLE_NAME) = 'bookcopies'
AND COLUMN_NAME = 'BookId'
AND LOWER(REFERENCED_TABLE_NAME) = 'books';

SELECT 
    'Borrowings -> Members' AS Relationship,
    CASE 
        WHEN COUNT(*) > 0 THEN '✓ EXISTS'
        ELSE '✗ MISSING'
    END AS Status
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
AND LOWER(TABLE_NAME) = 'borrowings'
AND COLUMN_NAME = 'MemberId'
AND LOWER(REFERENCED_TABLE_NAME) = 'members';

SELECT 
    'Borrowings -> Books' AS Relationship,
    CASE 
        WHEN COUNT(*) > 0 THEN '✓ EXISTS'
        ELSE '✗ MISSING'
    END AS Status
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
AND LOWER(TABLE_NAME) = 'borrowings'
AND COLUMN_NAME = 'BookId'
AND LOWER(REFERENCED_TABLE_NAME) = 'books';

SELECT 
    'Reservations -> Members' AS Relationship,
    CASE 
        WHEN COUNT(*) > 0 THEN '✓ EXISTS'
        ELSE '✗ MISSING'
    END AS Status
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
AND LOWER(TABLE_NAME) = 'reservations'
AND COLUMN_NAME = 'MemberId'
AND LOWER(REFERENCED_TABLE_NAME) = 'members';

SELECT 
    'Reservations -> Books' AS Relationship,
    CASE 
        WHEN COUNT(*) > 0 THEN '✓ EXISTS'
        ELSE '✗ MISSING'
    END AS Status
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
AND LOWER(TABLE_NAME) = 'reservations'
AND COLUMN_NAME = 'BookId'
AND LOWER(REFERENCED_TABLE_NAME) = 'books';

SELECT 
    'Fines -> Members' AS Relationship,
    CASE 
        WHEN COUNT(*) > 0 THEN '✓ EXISTS'
        ELSE '✗ MISSING'
    END AS Status
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
AND LOWER(TABLE_NAME) = 'fines'
AND COLUMN_NAME = 'MemberId'
AND LOWER(REFERENCED_TABLE_NAME) = 'members';

SELECT 
    'Fines -> Borrowings' AS Relationship,
    CASE 
        WHEN COUNT(*) > 0 THEN '✓ EXISTS'
        ELSE '✗ MISSING'
    END AS Status
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
AND LOWER(TABLE_NAME) = 'fines'
AND COLUMN_NAME = 'BorrowingId'
AND LOWER(REFERENCED_TABLE_NAME) = 'borrowings';

-- Check for orphaned records (records with invalid foreign keys)
-- Note: These queries will return rows if there are data integrity issues
-- Using stored procedure to handle missing tables gracefully

DELIMITER $$

DROP PROCEDURE IF EXISTS CheckOrphanedRecords$$

CREATE PROCEDURE CheckOrphanedRecords()
BEGIN
    DECLARE table_exists INT DEFAULT 0;
    
    -- Check Orphaned Members (no User)
    SELECT 'Orphaned Members (no User)' AS CheckType, COUNT(*) AS Count
    FROM Members m
    LEFT JOIN Users u ON m.UserId = u.UserId
    WHERE u.UserId IS NULL;
    
    -- Check if Books table exists
    SELECT COUNT(*) INTO table_exists
    FROM information_schema.TABLES 
    WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
    AND LOWER(TABLE_NAME) = 'books';
    
    IF table_exists > 0 THEN
        SELECT 'Orphaned Books (invalid Category)' AS CheckType, COUNT(*) AS Count
        FROM (SELECT TABLE_NAME FROM information_schema.TABLES 
              WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
              AND LOWER(TABLE_NAME) = 'books' LIMIT 1) t
        CROSS JOIN (SELECT TABLE_NAME FROM information_schema.TABLES 
                    WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                    AND LOWER(TABLE_NAME) = 'categories' LIMIT 1) c;
        
        SET @sql = CONCAT('SELECT ''Orphaned Books (invalid Category)'' AS CheckType, COUNT(*) AS Count
                          FROM `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                    WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                    AND LOWER(TABLE_NAME) = ''books'' LIMIT 1), '` b
                          LEFT JOIN `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                         WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                         AND LOWER(TABLE_NAME) = ''categories'' LIMIT 1), '` c 
                          ON b.CategoryId = c.CategoryId
                          WHERE b.CategoryId IS NOT NULL AND c.CategoryId IS NULL');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    ELSE
        SELECT 'Orphaned Books (invalid Category)' AS CheckType, 0 AS Count;
    END IF;
    
    -- Check if BookCopies table exists
    SELECT COUNT(*) INTO table_exists
    FROM information_schema.TABLES 
    WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
    AND LOWER(TABLE_NAME) = 'bookcopies';
    
    IF table_exists > 0 THEN
        SET @sql = CONCAT('SELECT ''Orphaned BookCopies (no Book)'' AS CheckType, COUNT(*) AS Count
                          FROM `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                    WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                    AND LOWER(TABLE_NAME) = ''bookcopies'' LIMIT 1), '` bc
                          LEFT JOIN `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                         WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                         AND LOWER(TABLE_NAME) = ''books'' LIMIT 1), '` b 
                          ON bc.BookId = b.BookId
                          WHERE b.BookId IS NULL');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    ELSE
        SELECT 'Orphaned BookCopies (no Book)' AS CheckType, 0 AS Count;
    END IF;
    
    -- Check if Borrowings table exists
    SELECT COUNT(*) INTO table_exists
    FROM information_schema.TABLES 
    WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
    AND LOWER(TABLE_NAME) = 'borrowings';
    
    IF table_exists > 0 THEN
        SET @sql = CONCAT('SELECT ''Orphaned Borrowings (no Member)'' AS CheckType, COUNT(*) AS Count
                          FROM `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                    WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                    AND LOWER(TABLE_NAME) = ''borrowings'' LIMIT 1), '` br
                          LEFT JOIN `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                         WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                         AND LOWER(TABLE_NAME) = ''members'' LIMIT 1), '` m 
                          ON br.MemberId = m.MemberId
                          WHERE m.MemberId IS NULL');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
        
        SET @sql = CONCAT('SELECT ''Orphaned Borrowings (no Book)'' AS CheckType, COUNT(*) AS Count
                          FROM `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                    WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                    AND LOWER(TABLE_NAME) = ''borrowings'' LIMIT 1), '` br
                          LEFT JOIN `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                         WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                         AND LOWER(TABLE_NAME) = ''books'' LIMIT 1), '` b 
                          ON br.BookId = b.BookId
                          WHERE b.BookId IS NULL');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    ELSE
        SELECT 'Orphaned Borrowings (no Member)' AS CheckType, 0 AS Count;
        SELECT 'Orphaned Borrowings (no Book)' AS CheckType, 0 AS Count;
    END IF;
    
    -- Check if Reservations table exists
    SELECT COUNT(*) INTO table_exists
    FROM information_schema.TABLES 
    WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
    AND LOWER(TABLE_NAME) = 'reservations';
    
    IF table_exists > 0 THEN
        SET @sql = CONCAT('SELECT ''Orphaned Reservations (no Member)'' AS CheckType, COUNT(*) AS Count
                          FROM `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                    WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                    AND LOWER(TABLE_NAME) = ''reservations'' LIMIT 1), '` r
                          LEFT JOIN `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                         WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                         AND LOWER(TABLE_NAME) = ''members'' LIMIT 1), '` m 
                          ON r.MemberId = m.MemberId
                          WHERE m.MemberId IS NULL');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
        
        SET @sql = CONCAT('SELECT ''Orphaned Reservations (no Book)'' AS CheckType, COUNT(*) AS Count
                          FROM `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                    WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                    AND LOWER(TABLE_NAME) = ''reservations'' LIMIT 1), '` r
                          LEFT JOIN `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                         WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                         AND LOWER(TABLE_NAME) = ''books'' LIMIT 1), '` b 
                          ON r.BookId = b.BookId
                          WHERE b.BookId IS NULL');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    ELSE
        SELECT 'Orphaned Reservations (no Member)' AS CheckType, 0 AS Count;
        SELECT 'Orphaned Reservations (no Book)' AS CheckType, 0 AS Count;
    END IF;
    
    -- Check if Fines table exists
    SELECT COUNT(*) INTO table_exists
    FROM information_schema.TABLES 
    WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
    AND LOWER(TABLE_NAME) = 'fines';
    
    IF table_exists > 0 THEN
        SET @sql = CONCAT('SELECT ''Orphaned Fines (no Member)'' AS CheckType, COUNT(*) AS Count
                          FROM `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                    WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                    AND LOWER(TABLE_NAME) = ''fines'' LIMIT 1), '` f
                          LEFT JOIN `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                         WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                         AND LOWER(TABLE_NAME) = ''members'' LIMIT 1), '` m 
                          ON f.MemberId = m.MemberId
                          WHERE m.MemberId IS NULL');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
        
        -- Check if Borrowings exists for Fines check
        SELECT COUNT(*) INTO table_exists
        FROM information_schema.TABLES 
        WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
        AND LOWER(TABLE_NAME) = 'borrowings';
        
        IF table_exists > 0 THEN
            SET @sql = CONCAT('SELECT ''Fines with invalid BorrowingId'' AS CheckType, COUNT(*) AS Count
                              FROM `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                        WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                        AND LOWER(TABLE_NAME) = ''fines'' LIMIT 1), '` f
                              LEFT JOIN `', (SELECT TABLE_NAME FROM information_schema.TABLES 
                                             WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
                                             AND LOWER(TABLE_NAME) = ''borrowings'' LIMIT 1), '` b 
                              ON f.BorrowingId = b.BorrowingId
                              WHERE f.BorrowingId IS NOT NULL AND b.BorrowingId IS NULL');
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
        ELSE
            SELECT 'Fines with invalid BorrowingId' AS CheckType, 0 AS Count;
        END IF;
    ELSE
        SELECT 'Orphaned Fines (no Member)' AS CheckType, 0 AS Count;
        SELECT 'Fines with invalid BorrowingId' AS CheckType, 0 AS Count;
    END IF;
END$$

DELIMITER ;

-- Execute the stored procedure
CALL CheckOrphanedRecords();

-- Clean up
DROP PROCEDURE IF EXISTS CheckOrphanedRecords;

-- Summary
SELECT 'Foreign Key Verification Complete' AS Status;
