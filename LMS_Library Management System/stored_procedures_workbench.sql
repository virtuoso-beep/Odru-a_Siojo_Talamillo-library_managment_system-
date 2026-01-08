-- =============================================
-- Library Management System - Stored Procedures
-- Database: LMS_DB
-- MySQL Workbench Compatible Version
-- =============================================

USE LMS_DB;

-- =============================================
-- Stored Procedure: sp_AuthenticateUser
-- Description: Authenticates a user by email and password
-- =============================================
DROP PROCEDURE IF EXISTS sp_AuthenticateUser;

DELIMITER $$

CREATE PROCEDURE sp_AuthenticateUser(
    IN p_Email VARCHAR(255),
    IN p_PasswordHash VARCHAR(255),
    IN p_ExpectedRole INT
)
BEGIN
    SELECT 
        UserId, 
        Email, 
        PasswordHash, 
        FirstName, 
        LastName, 
        Role, 
        IsActive,
        CreatedDate
    FROM Users
    WHERE Email = p_Email 
        AND PasswordHash = p_PasswordHash
        AND Role = p_ExpectedRole
        AND IsActive = 1;
END$$

DELIMITER ;

-- =============================================
-- Stored Procedure: sp_GetMemberDetails
-- Description: Gets member details by UserId
-- =============================================
DROP PROCEDURE IF EXISTS sp_GetMemberDetails;

DELIMITER $$

CREATE PROCEDURE sp_GetMemberDetails(
    IN p_UserId INT
)
BEGIN
    SELECT 
        MemberId, 
        MemberNumber,
        MemberType,
        Status,
        RegistrationDate
    FROM Members
    WHERE UserId = p_UserId;
END$$

DELIMITER ;

-- =============================================
-- Stored Procedure: sp_CheckUserExists
-- Description: Checks if a user exists by email
-- Returns: 1 if exists, 0 if not
-- =============================================
DROP PROCEDURE IF EXISTS sp_CheckUserExists;

DELIMITER $$

CREATE PROCEDURE sp_CheckUserExists(
    IN p_Email VARCHAR(255)
)
BEGIN
    SELECT COUNT(*) AS UserCount
    FROM Users
    WHERE Email = p_Email AND IsActive = 1;
END$$

DELIMITER ;

-- =============================================
-- Stored Procedure: sp_GetUserByEmail
-- Description: Retrieves user information by email
-- =============================================
DROP PROCEDURE IF EXISTS sp_GetUserByEmail;

DELIMITER $$

CREATE PROCEDURE sp_GetUserByEmail(
    IN p_Email VARCHAR(255)
)
BEGIN
    SELECT 
        UserId, 
        Email, 
        PasswordHash, 
        FirstName, 
        LastName, 
        Role, 
        IsActive,
        CreatedDate
    FROM Users
    WHERE Email = p_Email AND IsActive = 1;
END$$

DELIMITER ;

-- =============================================
-- Stored Procedure: sp_UpdateUserPassword
-- Description: Updates user password by email
-- Returns: Number of rows affected
-- =============================================
DROP PROCEDURE IF EXISTS sp_UpdateUserPassword;

DELIMITER $$

CREATE PROCEDURE sp_UpdateUserPassword(
    IN p_Email VARCHAR(255),
    IN p_PasswordHash VARCHAR(255)
)
BEGIN
    UPDATE Users 
    SET PasswordHash = p_PasswordHash
    WHERE Email = p_Email AND IsActive = 1;
    
    SELECT ROW_COUNT() AS RowsAffected;
END$$

DELIMITER ;

-- =============================================
-- Stored Procedure: sp_CreateUser
-- Description: Creates a new user in the system
-- Returns: The newly created UserId
-- =============================================
DROP PROCEDURE IF EXISTS sp_CreateUser;

DELIMITER $$

CREATE PROCEDURE sp_CreateUser(
    IN p_Email VARCHAR(255),
    IN p_PasswordHash VARCHAR(255),
    IN p_FirstName VARCHAR(100),
    IN p_LastName VARCHAR(100),
    IN p_Role INT
)
BEGIN
    INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role, IsActive, CreatedDate)
    VALUES (p_Email, p_PasswordHash, p_FirstName, p_LastName, p_Role, TRUE, NOW());
    
    SELECT LAST_INSERT_ID() AS UserId;
END$$

DELIMITER ;

-- =============================================
-- Stored Procedure: sp_GetUserCount
-- Description: Gets count of users by role
-- Returns: Count of users
-- =============================================
DROP PROCEDURE IF EXISTS sp_GetUserCount;

DELIMITER $$

CREATE PROCEDURE sp_GetUserCount(
    IN p_Role INT
)
BEGIN
    SELECT COUNT(*) AS UserCount
    FROM Users
    WHERE Role = p_Role AND IsActive = 1;
END$$

DELIMITER ;

-- =============================================
-- Stored Procedure: sp_GetUserCountByEmail
-- Description: Gets count of users by email and role
-- Returns: Count of users
-- =============================================
DROP PROCEDURE IF EXISTS sp_GetUserCountByEmail;

DELIMITER $$

CREATE PROCEDURE sp_GetUserCountByEmail(
    IN p_Email VARCHAR(255),
    IN p_Role INT
)
BEGIN
    SELECT COUNT(*) AS UserCount
    FROM Users
    WHERE Email = p_Email AND Role = p_Role AND IsActive = 1;
END$$

DELIMITER ;

