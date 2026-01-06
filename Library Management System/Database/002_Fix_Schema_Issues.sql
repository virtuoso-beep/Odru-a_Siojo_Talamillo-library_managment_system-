USE LibraryManagementDB;

-- ============================================================================
-- SCHEMA FIX SCRIPT
-- ============================================================================
-- This script fixes database schema issues and adds missing columns/tables.
--
-- PREREQUISITES:
-- 1. Run 001_Create_Database_Schema.sql FIRST to create all base tables
-- 2. Then run this script to add missing columns and the Borrowings table
-- ============================================================================

-- Fix 1: Create Borrowings table as it's referenced by stored procedures
-- This table is used by all stored procedures for circulation operations
-- Note: This will only create if Books and Members tables exist

SET @books_exists = (SELECT COUNT(*) FROM information_schema.TABLES 
                     WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                     AND LOWER(TABLE_NAME) = 'books');
SET @members_exists = (SELECT COUNT(*) FROM information_schema.TABLES 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'members');

SET @sql = IF(@books_exists > 0 AND @members_exists > 0,
    CONCAT('CREATE TABLE IF NOT EXISTS Borrowings (
        BorrowingId INT AUTO_INCREMENT PRIMARY KEY,
        MemberId INT NOT NULL,
        BookId INT NOT NULL,
        BorrowDate DATETIME DEFAULT CURRENT_TIMESTAMP,
        DueDate DATETIME NOT NULL,
        ReturnDate DATETIME NULL,
        Status VARCHAR(20) DEFAULT ''Borrowed'',
        FineAmount DECIMAL(10,2) DEFAULT 0.00,
        Notes TEXT,
        FOREIGN KEY (MemberId) REFERENCES `', 
        (SELECT TABLE_NAME FROM information_schema.TABLES 
         WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
         AND LOWER(TABLE_NAME) = ''members'' LIMIT 1), '`(MemberId) ON DELETE CASCADE,
        FOREIGN KEY (BookId) REFERENCES `',
        (SELECT TABLE_NAME FROM information_schema.TABLES 
         WHERE TABLE_SCHEMA = ''LibraryManagementDB'' 
         AND LOWER(TABLE_NAME) = ''books'' LIMIT 1), '`(BookId) ON DELETE CASCADE,
        INDEX idx_member_id (MemberId),
        INDEX idx_book_id (BookId),
        INDEX idx_status (Status),
        INDEX idx_due_date (DueDate),
        INDEX idx_borrow_date (BorrowDate),
        INDEX idx_return_date (ReturnDate)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci'),
    'SELECT ''Books or Members table does not exist. Please run 001_Create_Database_Schema.sql first.'' AS Error');

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Fix 2: Add missing columns to Reservations table
SET @reservations_exists = (SELECT COUNT(*) FROM information_schema.TABLES 
                            WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                            AND LOWER(TABLE_NAME) = 'reservations');

IF @reservations_exists > 0 THEN
    -- Check if ReservedDate column exists
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'reservations' 
                       AND LOWER(COLUMN_NAME) = 'reserveddate');
    
    IF @col_exists = 0 THEN
        SET @sql = CONCAT('ALTER TABLE `', 
            (SELECT TABLE_NAME FROM information_schema.TABLES 
             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
             AND LOWER(TABLE_NAME) = 'reservations' LIMIT 1), 
            '` ADD COLUMN ReservedDate DATETIME DEFAULT CURRENT_TIMESTAMP');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
    
    -- Check if ExpiryDate column exists
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'reservations' 
                       AND LOWER(COLUMN_NAME) = 'expirydate');
    
    IF @col_exists = 0 THEN
        SET @sql = CONCAT('ALTER TABLE `', 
            (SELECT TABLE_NAME FROM information_schema.TABLES 
             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
             AND LOWER(TABLE_NAME) = 'reservations' LIMIT 1), 
            '` ADD COLUMN ExpiryDate DATETIME NOT NULL DEFAULT (DATE_ADD(CURRENT_TIMESTAMP, INTERVAL 7 DAY))');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
    
    -- Update existing records that might have NULL ExpiryDate
    SET @sql = CONCAT('UPDATE `', 
        (SELECT TABLE_NAME FROM information_schema.TABLES 
         WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
         AND LOWER(TABLE_NAME) = 'reservations' LIMIT 1), 
        '` SET ExpiryDate = DATE_ADD(COALESCE(ReservationDate, ReservedDate, CURRENT_TIMESTAMP), INTERVAL 7 DAY)
         WHERE ExpiryDate IS NULL OR ExpiryDate = ''0000-00-00 00:00:00''');
    PREPARE stmt FROM @sql;
    EXECUTE stmt;
    DEALLOCATE PREPARE stmt;
END IF;

-- Fix 3: Add missing columns to Members table
SET @members_exists = (SELECT COUNT(*) FROM information_schema.TABLES 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'members');

IF @members_exists > 0 THEN
    -- Check if MembershipExpiryDate column exists
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'members' 
                       AND LOWER(COLUMN_NAME) = 'membershipexpirydate');
    
    IF @col_exists = 0 THEN
        SET @sql = CONCAT('ALTER TABLE `', 
            (SELECT TABLE_NAME FROM information_schema.TABLES 
             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
             AND LOWER(TABLE_NAME) = 'members' LIMIT 1), 
            '` ADD COLUMN MembershipExpiryDate DATE');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
    
    -- Check if EmergencyContactName column exists
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'members' 
                       AND LOWER(COLUMN_NAME) = 'emergencycontactname');
    
    IF @col_exists = 0 THEN
        SET @sql = CONCAT('ALTER TABLE `', 
            (SELECT TABLE_NAME FROM information_schema.TABLES 
             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
             AND LOWER(TABLE_NAME) = 'members' LIMIT 1), 
            '` ADD COLUMN EmergencyContactName VARCHAR(100)');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
    
    -- Check if EmergencyContactPhone column exists
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'members' 
                       AND LOWER(COLUMN_NAME) = 'emergencycontactphone');
    
    IF @col_exists = 0 THEN
        SET @sql = CONCAT('ALTER TABLE `', 
            (SELECT TABLE_NAME FROM information_schema.TABLES 
             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
             AND LOWER(TABLE_NAME) = 'members' LIMIT 1), 
            '` ADD COLUMN EmergencyContactPhone VARCHAR(20)');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END IF;

-- Fix 4: Update Fines table to support BorrowingId (used by stored procedures)
SET @fines_exists = (SELECT COUNT(*) FROM information_schema.TABLES 
                     WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                     AND LOWER(TABLE_NAME) = 'fines');
SET @borrowings_exists = (SELECT COUNT(*) FROM information_schema.TABLES 
                           WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                           AND LOWER(TABLE_NAME) = 'borrowings');

IF @fines_exists > 0 THEN
    -- Check if BorrowingId column exists
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'fines' 
                       AND LOWER(COLUMN_NAME) = 'borrowingid');
    
    IF @col_exists = 0 THEN
        SET @sql = CONCAT('ALTER TABLE `', 
            (SELECT TABLE_NAME FROM information_schema.TABLES 
             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
             AND LOWER(TABLE_NAME) = 'fines' LIMIT 1), 
            '` ADD COLUMN BorrowingId INT');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
        
        -- Add index for BorrowingId
        SET @idx_exists = (SELECT COUNT(*) FROM information_schema.STATISTICS 
                           WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                           AND LOWER(TABLE_NAME) = 'fines' 
                           AND LOWER(INDEX_NAME) = 'idx_borrowing_id');
        
        IF @idx_exists = 0 THEN
            SET @sql = CONCAT('CREATE INDEX idx_borrowing_id ON `', 
                (SELECT TABLE_NAME FROM information_schema.TABLES 
                 WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                 AND LOWER(TABLE_NAME) = 'fines' LIMIT 1), 
                '`(BorrowingId)');
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
        END IF;
        
        -- Add foreign key if Borrowings table exists
        IF @borrowings_exists > 0 THEN
            SET @fk_exists = (SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE 
                              WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                              AND LOWER(TABLE_NAME) = 'fines' 
                              AND LOWER(COLUMN_NAME) = 'borrowingid' 
                              AND REFERENCED_TABLE_NAME IS NOT NULL);
            
            IF @fk_exists = 0 THEN
                SET @sql = CONCAT('ALTER TABLE `', 
                    (SELECT TABLE_NAME FROM information_schema.TABLES 
                     WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                     AND LOWER(TABLE_NAME) = 'fines' LIMIT 1), 
                    '` ADD CONSTRAINT fk_fines_borrowings
                     FOREIGN KEY (BorrowingId) REFERENCES `',
                    (SELECT TABLE_NAME FROM information_schema.TABLES 
                     WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                     AND LOWER(TABLE_NAME) = 'borrowings' LIMIT 1), 
                    '`(BorrowingId) ON DELETE SET NULL');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            END IF;
        END IF;
    END IF;
END IF;

-- Fix 5: Add missing columns to Books table
SET @books_exists = (SELECT COUNT(*) FROM information_schema.TABLES 
                     WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                     AND LOWER(TABLE_NAME) = 'books');

IF @books_exists > 0 THEN
    -- Check if UpdatedDate column exists
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'books' 
                       AND LOWER(COLUMN_NAME) = 'updateddate');
    
    IF @col_exists = 0 THEN
        SET @sql = CONCAT('ALTER TABLE `', 
            (SELECT TABLE_NAME FROM information_schema.TABLES 
             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
             AND LOWER(TABLE_NAME) = 'books' LIMIT 1), 
            '` ADD COLUMN UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
    
    -- Check if Category column exists
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'books' 
                       AND LOWER(COLUMN_NAME) = 'category');
    
    IF @col_exists = 0 THEN
        SET @sql = CONCAT('ALTER TABLE `', 
            (SELECT TABLE_NAME FROM information_schema.TABLES 
             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
             AND LOWER(TABLE_NAME) = 'books' LIMIT 1), 
            '` ADD COLUMN Category VARCHAR(100)');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
        
        -- Populate Category from Categories table if it exists
        SET @categories_exists = (SELECT COUNT(*) FROM information_schema.TABLES 
                                   WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                                   AND LOWER(TABLE_NAME) = 'categories');
        
        IF @categories_exists > 0 THEN
            SET @sql = CONCAT('UPDATE `', 
                (SELECT TABLE_NAME FROM information_schema.TABLES 
                 WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                 AND LOWER(TABLE_NAME) = 'books' LIMIT 1), 
                '` b 
                INNER JOIN `',
                (SELECT TABLE_NAME FROM information_schema.TABLES 
                 WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                 AND LOWER(TABLE_NAME) = 'categories' LIMIT 1), 
                '` c ON b.CategoryId = c.CategoryId 
                SET b.Category = c.CategoryName 
                WHERE b.Category IS NULL OR b.Category = ''''');
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
        END IF;
    END IF;
END IF;

-- Fix 6: Add missing columns to Fines table
IF @fines_exists > 0 THEN
    -- Check if Reason column exists
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'fines' 
                       AND LOWER(COLUMN_NAME) = 'reason');
    
    IF @col_exists = 0 THEN
        SET @sql = CONCAT('ALTER TABLE `', 
            (SELECT TABLE_NAME FROM information_schema.TABLES 
             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
             AND LOWER(TABLE_NAME) = 'fines' LIMIT 1), 
            '` ADD COLUMN Reason TEXT');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
    
    -- Check if CreatedDate column exists
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'fines' 
                       AND LOWER(COLUMN_NAME) = 'createddate');
    
    IF @col_exists = 0 THEN
        SET @sql = CONCAT('ALTER TABLE `', 
            (SELECT TABLE_NAME FROM information_schema.TABLES 
             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
             AND LOWER(TABLE_NAME) = 'fines' LIMIT 1), 
            '` ADD COLUMN CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
    
    -- Update Status default if column exists
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'fines' 
                       AND LOWER(COLUMN_NAME) = 'status');
    
    IF @col_exists > 0 THEN
        SET @sql = CONCAT('ALTER TABLE `', 
            (SELECT TABLE_NAME FROM information_schema.TABLES 
             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
             AND LOWER(TABLE_NAME) = 'fines' LIMIT 1), 
            '` MODIFY COLUMN Status VARCHAR(20) DEFAULT ''Unpaid''');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END IF;

-- Fix 7: Add missing indexes for performance
-- Note: Additional comprehensive indexes are in 003_Add_Performance_Indexes.sql

-- Index for Fines table
IF @fines_exists > 0 THEN
    SET @idx_exists = (SELECT COUNT(*) FROM information_schema.STATISTICS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'fines' 
                       AND LOWER(INDEX_NAME) = 'idx_fines_member_status');
    
    IF @idx_exists = 0 THEN
        SET @sql = CONCAT('CREATE INDEX idx_fines_member_status ON `', 
            (SELECT TABLE_NAME FROM information_schema.TABLES 
             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
             AND LOWER(TABLE_NAME) = 'fines' LIMIT 1), 
            '`(MemberId, Status)');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
    
    SET @idx_exists = (SELECT COUNT(*) FROM information_schema.STATISTICS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'fines' 
                       AND LOWER(INDEX_NAME) = 'idx_fines_created_date');
    
    IF @idx_exists = 0 THEN
        SET @sql = CONCAT('CREATE INDEX idx_fines_created_date ON `', 
            (SELECT TABLE_NAME FROM information_schema.TABLES 
             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
             AND LOWER(TABLE_NAME) = 'fines' LIMIT 1), 
            '`(CreatedDate)');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END IF;

-- Index for Books table
IF @books_exists > 0 THEN
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'books' 
                       AND LOWER(COLUMN_NAME) = 'category');
    
    IF @col_exists > 0 THEN
        SET @idx_exists = (SELECT COUNT(*) FROM information_schema.STATISTICS 
                           WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                           AND LOWER(TABLE_NAME) = 'books' 
                           AND LOWER(INDEX_NAME) = 'idx_books_category');
        
        IF @idx_exists = 0 THEN
            SET @sql = CONCAT('CREATE INDEX idx_books_category ON `', 
                (SELECT TABLE_NAME FROM information_schema.TABLES 
                 WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                 AND LOWER(TABLE_NAME) = 'books' LIMIT 1), 
                '`(Category)');
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
        END IF;
    END IF;
END IF;

-- Index for Reservations table
IF @reservations_exists > 0 THEN
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'reservations' 
                       AND LOWER(COLUMN_NAME) = 'expirydate');
    
    IF @col_exists > 0 THEN
        SET @idx_exists = (SELECT COUNT(*) FROM information_schema.STATISTICS 
                           WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                           AND LOWER(TABLE_NAME) = 'reservations' 
                           AND LOWER(INDEX_NAME) = 'idx_reservations_expiry');
        
        IF @idx_exists = 0 THEN
            SET @sql = CONCAT('CREATE INDEX idx_reservations_expiry ON `', 
                (SELECT TABLE_NAME FROM information_schema.TABLES 
                 WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                 AND LOWER(TABLE_NAME) = 'reservations' LIMIT 1), 
                '`(ExpiryDate)');
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
        END IF;
    END IF;
END IF;

-- Index for Members table
IF @members_exists > 0 THEN
    SET @col_exists = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'members' 
                       AND LOWER(COLUMN_NAME) = 'membershipexpirydate');
    
    IF @col_exists > 0 THEN
        SET @idx_exists = (SELECT COUNT(*) FROM information_schema.STATISTICS 
                           WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                           AND LOWER(TABLE_NAME) = 'members' 
                           AND LOWER(INDEX_NAME) = 'idx_members_expiry');
        
        IF @idx_exists = 0 THEN
            SET @sql = CONCAT('CREATE INDEX idx_members_expiry ON `', 
                (SELECT TABLE_NAME FROM information_schema.TABLES 
                 WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                 AND LOWER(TABLE_NAME) = 'members' LIMIT 1), 
                '`(MembershipExpiryDate)');
            PREPARE stmt FROM @sql;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
        END IF;
    END IF;
END IF;

-- Verification queries
SELECT 'Schema fixes completed. Verifying tables...' AS Status;
SELECT TABLE_NAME, TABLE_ROWS 
FROM information_schema.TABLES 
WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
AND LOWER(TABLE_NAME) IN ('borrowings', 'reservations', 'members', 'fines', 'books')
ORDER BY TABLE_NAME;

SELECT 'Verifying columns...' AS Status;
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
AND (
    (LOWER(TABLE_NAME) = 'reservations' AND LOWER(COLUMN_NAME) IN ('reserveddate', 'expirydate'))
    OR (LOWER(TABLE_NAME) = 'members' AND LOWER(COLUMN_NAME) IN ('membershipexpirydate', 'emergencycontactname', 'emergencycontactphone'))
    OR (LOWER(TABLE_NAME) = 'fines' AND LOWER(COLUMN_NAME) IN ('borrowingid', 'reason', 'createddate'))
    OR (LOWER(TABLE_NAME) = 'books' AND LOWER(COLUMN_NAME) IN ('updateddate', 'category'))
)
ORDER BY TABLE_NAME, COLUMN_NAME;
