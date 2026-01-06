-- ============================================
-- Verify All Stored Procedures
-- ============================================
-- This script verifies that all stored procedures exist
-- ============================================

USE LibraryManagementDB;

SELECT '=== VERIFYING STORED PROCEDURES ===' AS Status;

-- Password Reset Procedures
SELECT 
    CASE 
        WHEN COUNT(*) >= 5 THEN '✅ PASS'
        ELSE CONCAT('❌ FAIL - Expected 5, Found ', COUNT(*))
    END AS PasswordResetProcedures,
    GROUP_CONCAT(ROUTINE_NAME) AS Procedures
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'LibraryManagementDB'
  AND ROUTINE_TYPE = 'PROCEDURE'
  AND ROUTINE_NAME IN (
    'SP_CreatePasswordResetToken',
    'SP_ValidatePasswordResetToken',
    'SP_UsePasswordResetToken',
    'SP_GetUserByEmail',
    'SP_UpdateUserPassword'
  );

-- Reports Procedures
SELECT 
    CASE 
        WHEN COUNT(*) >= 8 THEN '✅ PASS'
        ELSE CONCAT('❌ FAIL - Expected 8, Found ', COUNT(*))
    END AS ReportsProcedures,
    GROUP_CONCAT(ROUTINE_NAME) AS Procedures
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'LibraryManagementDB'
  AND ROUTINE_TYPE = 'PROCEDURE'
  AND ROUTINE_NAME IN (
    'SP_GetDailyCirculationReport',
    'SP_GetPopularBooksReport',
    'SP_GetOverdueBooksReport',
    'SP_GetMemberTypeDistribution',
    'SP_GetMemberActivitySummary',
    'SP_GetCollectionStatistics',
    'SP_GetCollectionByCategory',
    'SP_GetFineReport'
  );

-- Settings Procedures
SELECT 
    CASE 
        WHEN COUNT(*) >= 6 THEN '✅ PASS'
        ELSE CONCAT('❌ FAIL - Expected 6, Found ', COUNT(*))
    END AS SettingsProcedures,
    GROUP_CONCAT(ROUTINE_NAME) AS Procedures
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'LibraryManagementDB'
  AND ROUTINE_TYPE = 'PROCEDURE'
  AND ROUTINE_NAME IN (
    'SP_GetSetting',
    'SP_SetSetting',
    'SP_GetAllSettings',
    'SP_DeleteSetting',
    'SP_GetLibraryInfo',
    'SP_GetNotificationSettings',
    'SP_GetBorrowingSettings',
    'SP_GetFinesSettings'
  );

-- Check PasswordResetTokens table
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ PASS - PasswordResetTokens table exists'
        ELSE '❌ FAIL - PasswordResetTokens table missing'
    END AS PasswordResetTokensTable
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
  AND TABLE_NAME = 'PasswordResetTokens';

-- Summary
SELECT 
    '=== SUMMARY ===' AS Status,
    (SELECT COUNT(*) FROM information_schema.ROUTINES 
     WHERE ROUTINE_SCHEMA = 'LibraryManagementDB' 
     AND ROUTINE_TYPE = 'PROCEDURE') AS TotalProcedures,
    (SELECT COUNT(*) FROM information_schema.TABLES 
     WHERE TABLE_SCHEMA = 'LibraryManagementDB') AS TotalTables;

