USE LibraryManagementDB;
DELIMITER $$
DROP PROCEDURE IF EXISTS SP_AuthenticateUser$$
CREATE PROCEDURE SP_AuthenticateUser(
    IN p_Email VARCHAR(255),
    IN p_Role INT
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
    WHERE BINARY LOWER(TRIM(Email)) = BINARY LOWER(TRIM(p_Email))
      AND Role = p_Role
      AND IsActive = 1;
END$$
DROP PROCEDURE IF EXISTS SP_GetMemberDetails$$
CREATE PROCEDURE SP_GetMemberDetails(
    IN p_UserId INT
)
BEGIN
    SELECT 
        MemberId,
        MemberNumber,
        MemberType,
        Status,
        RegistrationDate,
        Phone,
        Address,
        IdNumber,
        DateOfBirth,
        Gender,
        Department,
        EmergencyContactName,
        EmergencyContactPhone,
        MembershipExpiryDate
    FROM Members
    WHERE UserId = p_UserId;
END$$
DROP PROCEDURE IF EXISTS SP_CheckUserExists$$
CREATE PROCEDURE SP_CheckUserExists(
    IN p_Email VARCHAR(255)
)
BEGIN
    SELECT COUNT(1) AS UserCount
    FROM Users
    WHERE BINARY LOWER(TRIM(Email)) = BINARY LOWER(TRIM(p_Email));
END$$
DROP PROCEDURE IF EXISTS SP_CreateUser$$
CREATE PROCEDURE SP_CreateUser(
    IN p_Email VARCHAR(255),
    IN p_PasswordHash VARCHAR(255),
    IN p_FirstName VARCHAR(100),
    IN p_LastName VARCHAR(100),
    IN p_Role INT,
    IN p_IsActive BOOLEAN
)
BEGIN
    INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role, IsActive, CreatedDate)
    VALUES (
        LOWER(TRIM(p_Email)),
        p_PasswordHash,
        p_FirstName,
        p_LastName,
        p_Role,
        p_IsActive,
        NOW()
    );
    SELECT LAST_INSERT_ID() AS UserId;
END$$
DROP PROCEDURE IF EXISTS SP_GetUserById$$
CREATE PROCEDURE SP_GetUserById(
    IN p_UserId INT
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
    WHERE UserId = p_UserId;
END$$
DROP PROCEDURE IF EXISTS SP_CreateStaffUser$$
CREATE PROCEDURE SP_CreateStaffUser(
    IN p_Email VARCHAR(255),
    IN p_Password VARCHAR(255),
    IN p_FirstName VARCHAR(100),
    IN p_LastName VARCHAR(100)
)
BEGIN
    DECLARE v_PasswordHash VARCHAR(255);
    SET v_PasswordHash = SHA2(p_Password, 256);
    INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role, IsActive, CreatedDate)
    VALUES (
        LOWER(TRIM(p_Email)),
        v_PasswordHash,
        p_FirstName,
        p_LastName,
        2,
        TRUE,
        NOW()
    );
    SELECT LAST_INSERT_ID() AS UserId;
END$$
DELIMITER ;
