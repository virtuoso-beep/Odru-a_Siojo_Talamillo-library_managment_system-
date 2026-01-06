-- ============================================
-- Deploy All New Stored Procedures
-- ============================================
-- This script deploys all new stored procedures in the correct order
-- Run this script to set up all new functionality
-- ============================================

USE LibraryManagementDB;

-- Show current status
SELECT 'Starting deployment of new stored procedures...' AS Status;

-- ============================================
-- 1. Password Reset Procedures
-- ============================================
SOURCE Database/StoredProcedures/009_Password_Reset_Procedures.sql;

-- Verify password reset procedures
SELECT 
    ROUTINE_NAME,
    CREATED,
    'Password Reset' AS Category
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'LibraryManagementDB'
  AND ROUTINE_TYPE = 'PROCEDURE'
  AND ROUTINE_NAME LIKE 'SP_%Password%'
ORDER BY ROUTINE_NAME;

-- ============================================
-- 2. Reports Procedures
-- ============================================
SOURCE Database/StoredProcedures/010_Reports_Procedures.sql;

-- Verify reports procedures
SELECT 
    ROUTINE_NAME,
    CREATED,
    'Reports' AS Category
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'LibraryManagementDB'
  AND ROUTINE_TYPE = 'PROCEDURE'
  AND ROUTINE_NAME LIKE 'SP_Get%Report%'
ORDER BY ROUTINE_NAME;

-- ============================================
-- 3. Settings Procedures
-- ============================================
SOURCE Database/StoredProcedures/011_Settings_Procedures.sql;

-- Verify settings procedures
SELECT 
    ROUTINE_NAME,
    CREATED,
    'Settings' AS Category
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'LibraryManagementDB'
  AND ROUTINE_TYPE = 'PROCEDURE'
  AND ROUTINE_NAME LIKE 'SP_%Setting%'
ORDER BY ROUTINE_NAME;

-- ============================================
-- Summary
-- ============================================
SELECT 
    'Deployment Complete!' AS Status,
    COUNT(*) AS TotalProcedures
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'LibraryManagementDB'
  AND ROUTINE_TYPE = 'PROCEDURE';

SELECT 'All new stored procedures have been deployed successfully.' AS Message;

