USE LibraryManagementDB;
DELIMITER $$
DROP PROCEDURE IF EXISTS SP_GetMemberStatistics$$
CREATE PROCEDURE SP_GetMemberStatistics()
BEGIN
    SELECT 
        (SELECT COUNT(*) FROM Members) AS TotalMembers,
        (SELECT COUNT(*) FROM Members 
         WHERE Status = 1 AND (MembershipExpiryDate IS NULL OR MembershipExpiryDate > NOW())) AS ActiveMembers,
        (SELECT COUNT(*) FROM Members WHERE Status = 3) AS SuspendedMembers,
        (SELECT COUNT(*) FROM Members 
         WHERE Status = 4 OR (MembershipExpiryDate IS NOT NULL AND MembershipExpiryDate < NOW())) AS ExpiredMembers;
END$$
DROP PROCEDURE IF EXISTS SP_GetAllMembers$$
CREATE PROCEDURE SP_GetAllMembers(
    IN p_SearchText VARCHAR(255),
    IN p_StatusFilter VARCHAR(50),
    IN p_TypeFilter VARCHAR(50)
)
BEGIN
    SET @search = CONCAT('%', IFNULL(p_SearchText, ''), '%');
    SET @statusFilter = IFNULL(p_StatusFilter, 'All Status');
    SET @typeFilter = IFNULL(p_TypeFilter, 'All Types');
    SELECT
        m.MemberNumber,
        u.FirstName,
        u.LastName,
        CASE
            WHEN m.MemberType = 1 THEN 'Student'
            WHEN m.MemberType = 2 THEN 'Faculty'
            WHEN m.MemberType = 3 THEN 'Staff'
            ELSE 'Guest'
        END AS MemberType,
        u.Email,
        CASE
            WHEN m.Status = 1 AND (m.MembershipExpiryDate IS NULL OR m.MembershipExpiryDate > NOW()) THEN 'Active'
            WHEN m.Status = 2 THEN 'Inactive'
            WHEN m.Status = 3 THEN 'Suspended'
            WHEN m.Status = 4 OR (m.MembershipExpiryDate IS NOT NULL AND m.MembershipExpiryDate < NOW()) THEN 'Expired'
            ELSE 'Unknown'
        END AS Status,
        COALESCE((SELECT COUNT(*) FROM Borrowings b WHERE b.MemberId = m.MemberId AND b.ReturnDate IS NULL), 0) AS BooksBorrowed,
        5 AS BooksLimit,
        COALESCE((SELECT SUM(f.Amount) FROM Fines f WHERE f.MemberId = m.MemberId AND f.Status IN ('Pending', 'Unpaid')), 0) AS Fines,
        m.Phone,
        m.Address,
        m.IdNumber,
        m.DateOfBirth,
        m.Gender,
        m.Department,
        m.EmergencyContactName,
        m.EmergencyContactPhone,
        m.MembershipExpiryDate,
        m.RegistrationDate
    FROM Members m
    INNER JOIN Users u ON m.UserId = u.UserId
    WHERE (p_SearchText IS NULL OR p_SearchText = '' OR
           u.FirstName LIKE @search OR 
           u.LastName LIKE @search OR 
           u.Email LIKE @search OR 
           m.MemberNumber LIKE @search)
      AND (p_StatusFilter = 'All Status' OR
           (p_StatusFilter = 'Active' AND m.Status = 1 AND (m.MembershipExpiryDate IS NULL OR m.MembershipExpiryDate > NOW())) OR
           (p_StatusFilter = 'Suspended' AND m.Status = 3) OR
           (p_StatusFilter = 'Expired' AND (m.Status = 4 OR (m.MembershipExpiryDate IS NOT NULL AND m.MembershipExpiryDate < NOW()))) OR
           (p_StatusFilter = 'Inactive' AND m.Status = 2))
      AND (p_TypeFilter = 'All Types' OR
           (p_TypeFilter = 'Student' AND m.MemberType = 1) OR
           (p_TypeFilter = 'Faculty' AND m.MemberType = 2) OR
           (p_TypeFilter = 'Staff' AND m.MemberType = 3) OR
           (p_TypeFilter = 'Guest' AND m.MemberType IS NULL))
    ORDER BY u.LastName, u.FirstName;
END$$
DROP PROCEDURE IF EXISTS SP_GetMemberByNumber$$
CREATE PROCEDURE SP_GetMemberByNumber(
    IN p_MemberNumber VARCHAR(20)
)
BEGIN
    SELECT 
        m.MemberNumber,
        u.FirstName,
        u.LastName,
        CASE
            WHEN m.MemberType = 1 THEN 'Student'
            WHEN m.MemberType = 2 THEN 'Faculty'
            WHEN m.MemberType = 3 THEN 'Staff'
            ELSE 'Guest'
        END AS MemberType,
        u.Email,
        CASE
            WHEN m.Status = 1 AND (m.MembershipExpiryDate IS NULL OR m.MembershipExpiryDate > NOW()) THEN 'Active'
            WHEN m.Status = 2 THEN 'Inactive'
            WHEN m.Status = 3 THEN 'Suspended'
            WHEN m.Status = 4 OR (m.MembershipExpiryDate IS NOT NULL AND m.MembershipExpiryDate < NOW()) THEN 'Expired'
            ELSE 'Unknown'
        END AS Status,
        COALESCE((SELECT COUNT(*) FROM Borrowings b WHERE b.MemberId = m.MemberId AND b.ReturnDate IS NULL), 0) AS BooksBorrowed,
        5 AS BooksLimit,
        COALESCE((SELECT SUM(f.Amount) FROM Fines f WHERE f.MemberId = m.MemberId AND f.Status IN ('Pending', 'Unpaid')), 0) AS Fines,
        m.Phone,
        m.Address,
        m.RegistrationDate,
        m.MembershipExpiryDate,
        COALESCE((SELECT COUNT(*) FROM Borrowings b WHERE b.MemberId = m.MemberId), 0) AS TotalBorrowed,
        m.IdNumber,
        m.DateOfBirth,
        m.Gender,
        m.Department,
        m.EmergencyContactName,
        m.EmergencyContactPhone
    FROM Members m
    INNER JOIN Users u ON m.UserId = u.UserId
    WHERE m.MemberNumber = p_MemberNumber;
END$$
DROP PROCEDURE IF EXISTS SP_RegisterMember$$
CREATE PROCEDURE SP_RegisterMember(
    IN p_FirstName VARCHAR(100),
    IN p_LastName VARCHAR(100),
    IN p_Email VARCHAR(255),
    IN p_PasswordHash VARCHAR(255),
    IN p_MemberType VARCHAR(50),
    IN p_Status VARCHAR(50),
    IN p_Phone VARCHAR(20),
    IN p_Address TEXT,
    IN p_IdNumber VARCHAR(20),
    IN p_DateOfBirth DATE,
    IN p_Gender VARCHAR(10),
    IN p_Department VARCHAR(100),
    IN p_EmergencyContactName VARCHAR(100),
    IN p_EmergencyContactPhone VARCHAR(20),
    IN p_MembershipExpiryDate DATE
)
BEGIN
    DECLARE v_UserId INT;
    DECLARE v_MemberNumber VARCHAR(20);
    DECLARE v_MemberTypeValue INT;
    DECLARE v_StatusValue INT;
    IF EXISTS (SELECT 1 FROM Users WHERE LOWER(Email) = LOWER(TRIM(p_Email))) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Email already exists in the system';
    END IF;
    SET v_MemberTypeValue = CASE p_MemberType
        WHEN 'Student' THEN 1
        WHEN 'Faculty' THEN 2
        WHEN 'Staff' THEN 3
        ELSE NULL
    END;
    SET v_StatusValue = CASE p_Status
        WHEN 'Active' THEN 1
        WHEN 'Inactive' THEN 2
        WHEN 'Suspended' THEN 3
        WHEN 'Expired' THEN 4
        ELSE 1
    END;
    INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role, IsActive, CreatedDate)
    VALUES (LOWER(TRIM(p_Email)), p_PasswordHash, p_FirstName, p_LastName, 3, TRUE, NOW());
    SET v_UserId = LAST_INSERT_ID();
    SET v_MemberNumber = CONCAT('MEM-', YEAR(NOW()), '-', LPAD(v_UserId, 4, '0'));
    INSERT INTO Members (
        UserId, MemberNumber, MemberType, Status, RegistrationDate,
        Phone, Address, IdNumber, DateOfBirth, Gender, Department,
        EmergencyContactName, EmergencyContactPhone, MembershipExpiryDate
    )
    VALUES (
        v_UserId, v_MemberNumber, v_MemberTypeValue, v_StatusValue, NOW(),
        p_Phone, p_Address, p_IdNumber, p_DateOfBirth, p_Gender, p_Department,
        p_EmergencyContactName, p_EmergencyContactPhone, p_MembershipExpiryDate
    );
    SELECT v_MemberNumber AS MemberNumber, v_UserId AS UserId;
END$$
DROP PROCEDURE IF EXISTS SP_UpdateMember$$
CREATE PROCEDURE SP_UpdateMember(
    IN p_MemberNumber VARCHAR(20),
    IN p_FirstName VARCHAR(100),
    IN p_LastName VARCHAR(100),
    IN p_Email VARCHAR(255),
    IN p_MemberType VARCHAR(50),
    IN p_Status VARCHAR(50),
    IN p_Phone VARCHAR(20),
    IN p_Address TEXT,
    IN p_IdNumber VARCHAR(20),
    IN p_DateOfBirth DATE,
    IN p_Gender VARCHAR(10),
    IN p_Department VARCHAR(100),
    IN p_EmergencyContactName VARCHAR(100),
    IN p_EmergencyContactPhone VARCHAR(20),
    IN p_MembershipExpiryDate DATE
)
BEGIN
    DECLARE v_UserId INT;
    DECLARE v_MemberTypeValue INT;
    DECLARE v_StatusValue INT;
    SELECT UserId INTO v_UserId FROM Members WHERE MemberNumber = p_MemberNumber;
    IF v_UserId IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Member not found';
    END IF;
    IF EXISTS (SELECT 1 FROM Users WHERE LOWER(Email) = LOWER(TRIM(p_Email)) AND UserId != v_UserId) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Email already exists for another member';
    END IF;
    SET v_MemberTypeValue = CASE p_MemberType
        WHEN 'Student' THEN 1
        WHEN 'Faculty' THEN 2
        WHEN 'Staff' THEN 3
        ELSE NULL
    END;
    SET v_StatusValue = CASE p_Status
        WHEN 'Active' THEN 1
        WHEN 'Inactive' THEN 2
        WHEN 'Suspended' THEN 3
        WHEN 'Expired' THEN 4
        ELSE 1
    END;
    UPDATE Users
    SET FirstName = p_FirstName,
        LastName = p_LastName,
        Email = LOWER(TRIM(p_Email))
    WHERE UserId = v_UserId;
    UPDATE Members
    SET MemberType = v_MemberTypeValue,
        Status = v_StatusValue,
        Phone = p_Phone,
        Address = p_Address,
        IdNumber = p_IdNumber,
        DateOfBirth = p_DateOfBirth,
        Gender = p_Gender,
        Department = p_Department,
        EmergencyContactName = p_EmergencyContactName,
        EmergencyContactPhone = p_EmergencyContactPhone,
        MembershipExpiryDate = p_MembershipExpiryDate
    WHERE MemberNumber = p_MemberNumber;
    SELECT ROW_COUNT() AS RowsAffected;
END$$
DROP PROCEDURE IF EXISTS SP_DeleteMember$$
CREATE PROCEDURE SP_DeleteMember(
    IN p_MemberNumber VARCHAR(20)
)
BEGIN
    DECLARE v_HasActiveBorrowings INT;
    DECLARE v_HasUnpaidFines INT;
    SELECT COUNT(*) INTO v_HasActiveBorrowings
    FROM Borrowings b
    INNER JOIN Members m ON b.MemberId = m.MemberId
    WHERE m.MemberNumber = p_MemberNumber AND b.ReturnDate IS NULL;
    IF v_HasActiveBorrowings > 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Cannot delete member with active borrowings';
    END IF;
    SELECT COUNT(*) INTO v_HasUnpaidFines
    FROM Fines f
    INNER JOIN Members m ON f.MemberId = m.MemberId
    WHERE m.MemberNumber = p_MemberNumber AND f.Status IN ('Pending', 'Unpaid');
    IF v_HasUnpaidFines > 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Cannot delete member with unpaid fines';
    END IF;
    UPDATE Members SET Status = 2 WHERE MemberNumber = p_MemberNumber;
    UPDATE Users u
    INNER JOIN Members m ON u.UserId = m.UserId
    SET u.IsActive = FALSE
    WHERE m.MemberNumber = p_MemberNumber;
    SELECT ROW_COUNT() AS RowsAffected;
END$$
DROP PROCEDURE IF EXISTS SP_CheckEmailRegistered$$
CREATE PROCEDURE SP_CheckEmailRegistered(
    IN p_Email VARCHAR(255)
)
BEGIN
    SELECT COUNT(*) AS EmailCount
    FROM Users
    WHERE LOWER(Email) = LOWER(TRIM(p_Email));
END$$
DROP PROCEDURE IF EXISTS SP_CheckIdNumberRegistered$$
CREATE PROCEDURE SP_CheckIdNumberRegistered(
    IN p_IdNumber VARCHAR(20),
    IN p_ExcludeMemberId INT
)
BEGIN
    IF p_ExcludeMemberId IS NOT NULL THEN
        SELECT COUNT(*) AS IdCount
        FROM Members
        WHERE IdNumber = TRIM(p_IdNumber) AND MemberId != p_ExcludeMemberId;
    ELSE
        SELECT COUNT(*) AS IdCount
        FROM Members
        WHERE IdNumber = TRIM(p_IdNumber);
    END IF;
END$$
DELIMITER ;
SELECT 
    ROUTINE_NAME AS ProcedureName,
    CREATED,
    LAST_ALTERED
FROM information_schema.ROUTINES
WHERE ROUTINE_SCHEMA = 'LibraryManagementDB'
  AND ROUTINE_TYPE = 'PROCEDURE'
  AND (ROUTINE_NAME LIKE 'SP_GetMember%' OR ROUTINE_NAME LIKE 'SP_%Member%')
ORDER BY ROUTINE_NAME;
