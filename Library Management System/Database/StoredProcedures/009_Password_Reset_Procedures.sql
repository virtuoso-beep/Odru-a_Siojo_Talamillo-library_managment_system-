USE LibraryManagementDB;
DELIMITER $$

-- Create PasswordResetTokens table if it doesn't exist
CREATE TABLE IF NOT EXISTS PasswordResetTokens (
    TokenId INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    Token VARCHAR(255) NOT NULL UNIQUE,
    ExpiryDate DATETIME NOT NULL,
    Used BOOLEAN DEFAULT FALSE,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    INDEX idx_token (Token),
    INDEX idx_user_id (UserId),
    INDEX idx_expiry_date (ExpiryDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

DROP PROCEDURE IF EXISTS SP_CreatePasswordResetToken$$
CREATE PROCEDURE SP_CreatePasswordResetToken(
    IN p_UserId INT,
    IN p_Token VARCHAR(255),
    IN p_ExpiryDate DATETIME
)
BEGIN
    -- Invalidate any existing tokens for this user
    UPDATE PasswordResetTokens
    SET Used = TRUE
    WHERE UserId = p_UserId AND Used = FALSE;
    
    -- Create new token
    INSERT INTO PasswordResetTokens (UserId, Token, ExpiryDate)
    VALUES (p_UserId, p_Token, p_ExpiryDate);
    
    SELECT LAST_INSERT_ID() AS TokenId;
END$$

DROP PROCEDURE IF EXISTS SP_ValidatePasswordResetToken$$
CREATE PROCEDURE SP_ValidatePasswordResetToken(
    IN p_Token VARCHAR(255)
)
BEGIN
    SELECT 
        prt.TokenId,
        prt.UserId,
        prt.Token,
        prt.ExpiryDate,
        prt.Used,
        u.Email,
        u.FirstName,
        u.LastName
    FROM PasswordResetTokens prt
    INNER JOIN Users u ON prt.UserId = u.UserId
    WHERE prt.Token = p_Token
      AND prt.Used = FALSE
      AND prt.ExpiryDate > NOW();
END$$

DROP PROCEDURE IF EXISTS SP_UsePasswordResetToken$$
CREATE PROCEDURE SP_UsePasswordResetToken(
    IN p_Token VARCHAR(255)
)
BEGIN
    UPDATE PasswordResetTokens
    SET Used = TRUE
    WHERE Token = p_Token AND Used = FALSE;
    
    SELECT ROW_COUNT() AS RowsAffected;
END$$

DROP PROCEDURE IF EXISTS SP_GetUserByEmail$$
CREATE PROCEDURE SP_GetUserByEmail(
    IN p_Email VARCHAR(255)
)
BEGIN
    SELECT 
        UserId,
        Email,
        FirstName,
        LastName,
        Role,
        IsActive
    FROM Users
    WHERE BINARY LOWER(TRIM(Email)) = BINARY LOWER(TRIM(p_Email))
      AND IsActive = 1;
END$$

DROP PROCEDURE IF EXISTS SP_UpdateUserPassword$$
CREATE PROCEDURE SP_UpdateUserPassword(
    IN p_UserId INT,
    IN p_NewPasswordHash VARCHAR(255)
)
BEGIN
    UPDATE Users
    SET PasswordHash = p_NewPasswordHash
    WHERE UserId = p_UserId;
    
    SELECT ROW_COUNT() AS RowsAffected;
END$$

DELIMITER ;

