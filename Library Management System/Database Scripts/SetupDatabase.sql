CREATE DATABASE IF NOT EXISTS LibraryManagementDB;
USE LibraryManagementDB;

CREATE TABLE IF NOT EXISTS Users (
    UserId INT AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Role INT NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LastLoginDate DATETIME NULL,
    CONSTRAINT CK_Users_Role CHECK (Role IN (1, 2, 3))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Members (
    MemberId INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    MemberNumber VARCHAR(50) NOT NULL UNIQUE,
    MemberType INT NULL,
    RegistrationDate DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ExpirationDate DATETIME NULL,
    Status INT NOT NULL DEFAULT 1,
    Phone VARCHAR(20) NULL,
    Address VARCHAR(255) NULL,
    CONSTRAINT FK_Members_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

DELIMITER $$

DROP PROCEDURE IF EXISTS CreateIndexIfNotExists$$

CREATE PROCEDURE CreateIndexIfNotExists(
    IN p_table_name VARCHAR(64),
    IN p_index_name VARCHAR(64),
    IN p_index_columns VARCHAR(255)
)
BEGIN
    DECLARE index_exists INT DEFAULT 0;
    
    SELECT COUNT(*) INTO index_exists
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE table_schema = DATABASE()
    AND table_name = p_table_name
    AND index_name = p_index_name;
    
    IF index_exists = 0 THEN
        SET @sql = CONCAT('CREATE INDEX ', p_index_name, ' ON ', p_table_name, '(', p_index_columns, ')');
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END$$

DELIMITER ;

CALL CreateIndexIfNotExists('Users', 'IX_Users_Email', 'Email');
CALL CreateIndexIfNotExists('Members', 'IX_Members_UserId', 'UserId');
CALL CreateIndexIfNotExists('Members', 'IX_Members_MemberNumber', 'MemberNumber');

DROP PROCEDURE IF EXISTS CreateIndexIfNotExists;

DELIMITER $$

DROP PROCEDURE IF EXISTS AddColumnIfNotExists$$

CREATE PROCEDURE AddColumnIfNotExists(
    IN p_table_name VARCHAR(64),
    IN p_column_name VARCHAR(64),
    IN p_column_definition VARCHAR(255)
)
BEGIN
    DECLARE column_exists INT DEFAULT 0;
    
    SELECT COUNT(*) INTO column_exists
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE table_schema = DATABASE()
    AND table_name = p_table_name
    AND column_name = p_column_name;
    
    IF column_exists = 0 THEN
        SET @sql = CONCAT('ALTER TABLE ', p_table_name, ' ADD COLUMN ', p_column_name, ' ', p_column_definition);
        PREPARE stmt FROM @sql;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END$$

DELIMITER ;

CALL AddColumnIfNotExists('Members', 'Phone', 'VARCHAR(20) NULL');
CALL AddColumnIfNotExists('Members', 'Address', 'VARCHAR(255) NULL');

DROP PROCEDURE IF EXISTS AddColumnIfNotExists;

INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role, IsActive)
SELECT 'a.dmin.123456.tc@umindanao.edu.ph', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'Admin', 'User', 1, TRUE
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'a.dmin.123456.tc@umindanao.edu.ph');

INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role, IsActive)
SELECT 's.taff.234567.tc@umindanao.edu.ph', '10176e7b7b24d317acfcf8d2064cfd2f24e154f7b5a96603077d5ef813d6a6b6', 'Staff', 'User', 2, TRUE
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Email = 's.taff.234567.tc@umindanao.edu.ph');

INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role, IsActive)
SELECT 't.odruna.142275.tc@umindanao.edu.ph', '5600376e863d2f57a053518f324ad3840b0bc2348b573af281a7b7cbe7a228c6', 'Member', 'User', 3, TRUE
WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Email = 't.odruna.142275.tc@umindanao.edu.ph');

INSERT INTO Members (UserId, MemberNumber, MemberType, Status)
SELECT u.UserId, 'MEM001', 1, 1
FROM Users u
WHERE u.Email = 't.odruna.142275.tc@umindanao.edu.ph'
AND NOT EXISTS (SELECT 1 FROM Members WHERE UserId = u.UserId);

SELECT '========================================' AS Separator;
SELECT 'Database setup completed successfully!' AS Status;
SELECT '========================================' AS Separator;
SELECT '' AS Blank;
SELECT 'Test accounts created:' AS Info;
SELECT '  - Administrator: a.dmin.123456.tc@umindanao.edu.ph / admin123' AS Account1;
SELECT '  - Staff: s.taff.234567.tc@umindanao.edu.ph / staff123' AS Account2;
SELECT '  - Member: t.odruna.142275.tc@umindanao.edu.ph / member123' AS Account3;
SELECT '' AS Blank;
SELECT 'Email format: firstname.lastname.IDnumber.tc@umindanao.edu.ph' AS Format;
SELECT '' AS Blank;
SELECT 'Tables created:' AS Tables;
SELECT '  - Users (with indexes)' AS Table1;
SELECT '  - Members (with Phone, Address, and indexes)' AS Table2;
