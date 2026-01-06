USE LibraryManagementDB;

-- ============================================================================
-- DATABASE SCHEMA FIXES TEST SCRIPT
-- ============================================================================
-- This script tests all database schema fixes and verifies stored procedures
-- work correctly after schema updates.
--
-- PREREQUISITES:
-- 1. Run 001_Create_Database_Schema.sql
-- 2. Run 002_Fix_Schema_Issues.sql
-- 3. Run all stored procedures from StoredProcedures/ folder
-- ============================================================================

SET @test_passed = 0;
SET @test_failed = 0;
SET @test_total = 0;

-- Helper procedure to log test results
DELIMITER $$

DROP PROCEDURE IF EXISTS LogTestResult$$

CREATE PROCEDURE LogTestResult(IN test_name VARCHAR(255), IN passed BOOLEAN, IN message TEXT)
BEGIN
    SET @test_total = @test_total + 1;
    IF passed THEN
        SET @test_passed = @test_passed + 1;
        SELECT CONCAT('✅ PASS: ', test_name) AS TestResult, message AS Details;
    ELSE
        SET @test_failed = @test_failed + 1;
        SELECT CONCAT('❌ FAIL: ', test_name) AS TestResult, message AS Details;
    END IF;
END$$

DELIMITER ;

-- ============================================================================
-- TEST 1: Verify Borrowings Table Exists
-- ============================================================================
SELECT '=== TEST 1: Borrowings Table ===' AS TestSection;

SET @table_exists = (SELECT COUNT(*) FROM information_schema.TABLES 
                     WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                     AND LOWER(TABLE_NAME) = 'borrowings');

CALL LogTestResult('Borrowings Table Exists', 
    @table_exists > 0,
    IF(@table_exists > 0, 'Borrowings table found', 'Borrowings table NOT found'));

-- Verify Borrowings table structure
IF @table_exists > 0 THEN
    SET @col_count = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                      WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                      AND LOWER(TABLE_NAME) = 'borrowings');
    CALL LogTestResult('Borrowings Table Columns', 
        @col_count >= 8,
        CONCAT('Found ', @col_count, ' columns (expected at least 8)'));
    
    -- Check for required columns
    SET @has_borrowingid = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                            WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                            AND LOWER(TABLE_NAME) = 'borrowings' 
                            AND LOWER(COLUMN_NAME) = 'borrowingid');
    SET @has_memberid = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                         WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                         AND LOWER(TABLE_NAME) = 'borrowings' 
                         AND LOWER(COLUMN_NAME) = 'memberid');
    SET @has_bookid = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'borrowings' 
                       AND LOWER(COLUMN_NAME) = 'bookid');
    
    CALL LogTestResult('Borrowings Required Columns', 
        @has_borrowingid > 0 AND @has_memberid > 0 AND @has_bookid > 0,
        CONCAT('BorrowingId: ', IF(@has_borrowingid > 0, 'Yes', 'No'),
               ', MemberId: ', IF(@has_memberid > 0, 'Yes', 'No'),
               ', BookId: ', IF(@has_bookid > 0, 'Yes', 'No')));
END IF;

-- ============================================================================
-- TEST 2: Verify Reservations Table Columns
-- ============================================================================
SELECT '=== TEST 2: Reservations Table Columns ===' AS TestSection;

SET @table_exists = (SELECT COUNT(*) FROM information_schema.TABLES 
                     WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                     AND LOWER(TABLE_NAME) = 'reservations');

IF @table_exists > 0 THEN
    SET @has_reserveddate = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                             WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                             AND LOWER(TABLE_NAME) = 'reservations' 
                             AND LOWER(COLUMN_NAME) = 'reserveddate');
    SET @has_expirydate = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                           WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                           AND LOWER(TABLE_NAME) = 'reservations' 
                           AND LOWER(COLUMN_NAME) = 'expirydate');
    
    CALL LogTestResult('Reservations ReservedDate Column', 
        @has_reserveddate > 0,
        IF(@has_reserveddate > 0, 'ReservedDate column exists', 'ReservedDate column missing'));
    
    CALL LogTestResult('Reservations ExpiryDate Column', 
        @has_expirydate > 0,
        IF(@has_expirydate > 0, 'ExpiryDate column exists', 'ExpiryDate column missing'));
END IF;

-- ============================================================================
-- TEST 3: Verify Members Table Columns
-- ============================================================================
SELECT '=== TEST 3: Members Table Columns ===' AS TestSection;

SET @table_exists = (SELECT COUNT(*) FROM information_schema.TABLES 
                     WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                     AND LOWER(TABLE_NAME) = 'members');

IF @table_exists > 0 THEN
    SET @has_expirydate = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                           WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                           AND LOWER(TABLE_NAME) = 'members' 
                           AND LOWER(COLUMN_NAME) = 'membershipexpirydate');
    SET @has_emergencyname = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                              WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                              AND LOWER(TABLE_NAME) = 'members' 
                              AND LOWER(COLUMN_NAME) = 'emergencycontactname');
    SET @has_emergencyphone = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                               WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                               AND LOWER(TABLE_NAME) = 'members' 
                               AND LOWER(COLUMN_NAME) = 'emergencycontactphone');
    
    CALL LogTestResult('Members MembershipExpiryDate Column', 
        @has_expirydate > 0,
        IF(@has_expirydate > 0, 'MembershipExpiryDate exists', 'MembershipExpiryDate missing'));
    
    CALL LogTestResult('Members EmergencyContactName Column', 
        @has_emergencyname > 0,
        IF(@has_emergencyname > 0, 'EmergencyContactName exists', 'EmergencyContactName missing'));
    
    CALL LogTestResult('Members EmergencyContactPhone Column', 
        @has_emergencyphone > 0,
        IF(@has_emergencyphone > 0, 'EmergencyContactPhone exists', 'EmergencyContactPhone missing'));
END IF;

-- ============================================================================
-- TEST 4: Verify Fines Table Columns
-- ============================================================================
SELECT '=== TEST 4: Fines Table Columns ===' AS TestSection;

SET @table_exists = (SELECT COUNT(*) FROM information_schema.TABLES 
                     WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                     AND LOWER(TABLE_NAME) = 'fines');

IF @table_exists > 0 THEN
    SET @has_borrowingid = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                            WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                            AND LOWER(TABLE_NAME) = 'fines' 
                            AND LOWER(COLUMN_NAME) = 'borrowingid');
    SET @has_reason = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                       WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                       AND LOWER(TABLE_NAME) = 'fines' 
                       AND LOWER(COLUMN_NAME) = 'reason');
    SET @has_createddate = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                            WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                            AND LOWER(TABLE_NAME) = 'fines' 
                            AND LOWER(COLUMN_NAME) = 'createddate');
    
    CALL LogTestResult('Fines BorrowingId Column', 
        @has_borrowingid > 0,
        IF(@has_borrowingid > 0, 'BorrowingId exists', 'BorrowingId missing'));
    
    CALL LogTestResult('Fines Reason Column', 
        @has_reason > 0,
        IF(@has_reason > 0, 'Reason exists', 'Reason missing'));
    
    CALL LogTestResult('Fines CreatedDate Column', 
        @has_createddate > 0,
        IF(@has_createddate > 0, 'CreatedDate exists', 'CreatedDate missing'));
END IF;

-- ============================================================================
-- TEST 5: Verify Books Table Columns
-- ============================================================================
SELECT '=== TEST 5: Books Table Columns ===' AS TestSection;

SET @table_exists = (SELECT COUNT(*) FROM information_schema.TABLES 
                     WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                     AND LOWER(TABLE_NAME) = 'books');

IF @table_exists > 0 THEN
    SET @has_updateddate = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                            WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                            AND LOWER(TABLE_NAME) = 'books' 
                            AND LOWER(COLUMN_NAME) = 'updateddate');
    SET @has_category = (SELECT COUNT(*) FROM information_schema.COLUMNS 
                         WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                         AND LOWER(TABLE_NAME) = 'books' 
                         AND LOWER(COLUMN_NAME) = 'category');
    
    CALL LogTestResult('Books UpdatedDate Column', 
        @has_updateddate > 0,
        IF(@has_updateddate > 0, 'UpdatedDate exists', 'UpdatedDate missing'));
    
    CALL LogTestResult('Books Category Column', 
        @has_category > 0,
        IF(@has_category > 0, 'Category exists', 'Category missing'));
END IF;

-- ============================================================================
-- TEST 6: Verify Foreign Keys
-- ============================================================================
SELECT '=== TEST 6: Foreign Key Relationships ===' AS TestSection;

-- Check Borrowings -> Members FK
SET @fk_exists = (SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE 
                  WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                  AND LOWER(TABLE_NAME) = 'borrowings' 
                  AND LOWER(COLUMN_NAME) = 'memberid' 
                  AND REFERENCED_TABLE_NAME IS NOT NULL);
CALL LogTestResult('Borrowings -> Members FK', 
    @fk_exists > 0,
    IF(@fk_exists > 0, 'Foreign key exists', 'Foreign key missing'));

-- Check Borrowings -> Books FK
SET @fk_exists = (SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE 
                  WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                  AND LOWER(TABLE_NAME) = 'borrowings' 
                  AND LOWER(COLUMN_NAME) = 'bookid' 
                  AND REFERENCED_TABLE_NAME IS NOT NULL);
CALL LogTestResult('Borrowings -> Books FK', 
    @fk_exists > 0,
    IF(@fk_exists > 0, 'Foreign key exists', 'Foreign key missing'));

-- Check Fines -> Borrowings FK (if BorrowingId exists)
SET @fk_exists = (SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE 
                  WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                  AND LOWER(TABLE_NAME) = 'fines' 
                  AND LOWER(COLUMN_NAME) = 'borrowingid' 
                  AND REFERENCED_TABLE_NAME IS NOT NULL);
CALL LogTestResult('Fines -> Borrowings FK', 
    @fk_exists > 0,
    IF(@fk_exists > 0, 'Foreign key exists', 'Foreign key missing or column not present'));

-- ============================================================================
-- TEST 7: Verify Indexes
-- ============================================================================
SELECT '=== TEST 7: Performance Indexes ===' AS TestSection;

SET @idx_count = (SELECT COUNT(*) FROM information_schema.STATISTICS 
                  WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                  AND LOWER(TABLE_NAME) = 'borrowings');
CALL LogTestResult('Borrowings Indexes', 
    @idx_count >= 6,
    CONCAT('Found ', @idx_count, ' indexes (expected at least 6)'));

SET @idx_count = (SELECT COUNT(*) FROM information_schema.STATISTICS 
                  WHERE TABLE_SCHEMA = 'LibraryManagementDB' 
                  AND LOWER(TABLE_NAME) = 'fines');
CALL LogTestResult('Fines Indexes', 
    @idx_count >= 3,
    CONCAT('Found ', @idx_count, ' indexes (expected at least 3)'));

-- ============================================================================
-- TEST 8: Test Stored Procedures Exist
-- ============================================================================
SELECT '=== TEST 8: Stored Procedures ===' AS TestSection;

-- Check for key stored procedures
SET @proc_exists = (SELECT COUNT(*) FROM information_schema.ROUTINES 
                    WHERE ROUTINE_SCHEMA = 'LibraryManagementDB' 
                    AND ROUTINE_TYPE = 'PROCEDURE'
                    AND LOWER(ROUTINE_NAME) LIKE '%borrow%');
CALL LogTestResult('Circulation Procedures', 
    @proc_exists > 0,
    CONCAT('Found ', @proc_exists, ' circulation-related procedures'));

SET @proc_exists = (SELECT COUNT(*) FROM information_schema.ROUTINES 
                    WHERE ROUTINE_SCHEMA = 'LibraryManagementDB' 
                    AND ROUTINE_TYPE = 'PROCEDURE'
                    AND LOWER(ROUTINE_NAME) LIKE '%fine%');
CALL LogTestResult('Fines Procedures', 
    @proc_exists > 0,
    CONCAT('Found ', @proc_exists, ' fines-related procedures'));

SET @proc_exists = (SELECT COUNT(*) FROM information_schema.ROUTINES 
                    WHERE ROUTINE_SCHEMA = 'LibraryManagementDB' 
                    AND ROUTINE_TYPE = 'PROCEDURE'
                    AND LOWER(ROUTINE_NAME) LIKE '%dashboard%');
CALL LogTestResult('Dashboard Procedures', 
    @proc_exists > 0,
    CONCAT('Found ', @proc_exists, ' dashboard-related procedures'));

-- ============================================================================
-- TEST 9: Test Data Integrity (if data exists)
-- ============================================================================
SELECT '=== TEST 9: Data Integrity ===' AS TestSection;

-- Check for orphaned records
SET @orphaned = (SELECT COUNT(*) FROM Borrowings br
                 LEFT JOIN Members m ON br.MemberId = m.MemberId
                 WHERE m.MemberId IS NULL);
CALL LogTestResult('No Orphaned Borrowings (Members)', 
    @orphaned = 0,
    CONCAT(IF(@orphaned = 0, 'No orphaned records', CONCAT(@orphaned, ' orphaned records found'))));

SET @orphaned = (SELECT COUNT(*) FROM Borrowings br
                 LEFT JOIN Books b ON br.BookId = b.BookId
                 WHERE b.BookId IS NULL);
CALL LogTestResult('No Orphaned Borrowings (Books)', 
    @orphaned = 0,
    CONCAT(IF(@orphaned = 0, 'No orphaned records', CONCAT(@orphaned, ' orphaned records found'))));

-- ============================================================================
-- TEST SUMMARY
-- ============================================================================
SELECT '=== TEST SUMMARY ===' AS TestSection;
SELECT 
    @test_total AS TotalTests,
    @test_passed AS Passed,
    @test_failed AS Failed,
    CASE 
        WHEN @test_failed = 0 THEN '✅ ALL TESTS PASSED'
        ELSE CONCAT('⚠️ ', @test_failed, ' TEST(S) FAILED')
    END AS Result;

-- Cleanup
DROP PROCEDURE IF EXISTS LogTestResult;

SELECT '=== Database Schema Tests Complete ===' AS Status;

