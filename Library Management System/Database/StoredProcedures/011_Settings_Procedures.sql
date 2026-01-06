USE LibraryManagementDB;
DELIMITER $$

-- Get Setting Value
DROP PROCEDURE IF EXISTS SP_GetSetting$$
CREATE PROCEDURE SP_GetSetting(
    IN p_SettingKey VARCHAR(100)
)
BEGIN
    SELECT 
        SettingId,
        SettingKey,
        SettingValue,
        Description,
        UpdatedDate
    FROM LibrarySettings
    WHERE SettingKey = p_SettingKey;
END$$

-- Set Setting Value
DROP PROCEDURE IF EXISTS SP_SetSetting$$
CREATE PROCEDURE SP_SetSetting(
    IN p_SettingKey VARCHAR(100),
    IN p_SettingValue TEXT,
    IN p_Description TEXT
)
BEGIN
    INSERT INTO LibrarySettings (SettingKey, SettingValue, Description, UpdatedDate)
    VALUES (p_SettingKey, p_SettingValue, p_Description, NOW())
    ON DUPLICATE KEY UPDATE
        SettingValue = p_SettingValue,
        Description = IFNULL(p_Description, Description),
        UpdatedDate = NOW();
    
    SELECT ROW_COUNT() AS RowsAffected;
END$$

-- Get All Settings
DROP PROCEDURE IF EXISTS SP_GetAllSettings$$
CREATE PROCEDURE SP_GetAllSettings()
BEGIN
    SELECT 
        SettingId,
        SettingKey,
        SettingValue,
        Description,
        UpdatedDate
    FROM LibrarySettings
    ORDER BY SettingKey;
END$$

-- Delete Setting
DROP PROCEDURE IF EXISTS SP_DeleteSetting$$
CREATE PROCEDURE SP_DeleteSetting(
    IN p_SettingKey VARCHAR(100)
)
BEGIN
    DELETE FROM LibrarySettings
    WHERE SettingKey = p_SettingKey;
    
    SELECT ROW_COUNT() AS RowsAffected;
END$$

-- Get Library Information Settings
DROP PROCEDURE IF EXISTS SP_GetLibraryInfo$$
CREATE PROCEDURE SP_GetLibraryInfo()
BEGIN
    SELECT 
        MAX(CASE WHEN SettingKey = 'LibraryName' THEN SettingValue END) AS LibraryName,
        MAX(CASE WHEN SettingKey = 'LibraryEmail' THEN SettingValue END) AS LibraryEmail,
        MAX(CASE WHEN SettingKey = 'LibraryPhone' THEN SettingValue END) AS LibraryPhone,
        MAX(CASE WHEN SettingKey = 'LibraryAddress' THEN SettingValue END) AS LibraryAddress
    FROM LibrarySettings
    WHERE SettingKey IN ('LibraryName', 'LibraryEmail', 'LibraryPhone', 'LibraryAddress');
END$$

-- Get Notification Settings
DROP PROCEDURE IF EXISTS SP_GetNotificationSettings$$
CREATE PROCEDURE SP_GetNotificationSettings()
BEGIN
    SELECT 
        MAX(CASE WHEN SettingKey = 'EmailNotifications' THEN SettingValue END) AS EmailNotifications,
        MAX(CASE WHEN SettingKey = 'OverdueReminders' THEN SettingValue END) AS OverdueReminders,
        MAX(CASE WHEN SettingKey = 'ReservationAlerts' THEN SettingValue END) AS ReservationAlerts,
        MAX(CASE WHEN SettingKey = 'DueDateReminders' THEN SettingValue END) AS DueDateReminders,
        MAX(CASE WHEN SettingKey = 'ReminderDaysBeforeDue' THEN SettingValue END) AS ReminderDaysBeforeDue
    FROM LibrarySettings
    WHERE SettingKey IN ('EmailNotifications', 'OverdueReminders', 'ReservationAlerts', 
                         'DueDateReminders', 'ReminderDaysBeforeDue');
END$$

-- Get Borrowing Settings
DROP PROCEDURE IF EXISTS SP_GetBorrowingSettings$$
CREATE PROCEDURE SP_GetBorrowingSettings()
BEGIN
    SELECT 
        MAX(CASE WHEN SettingKey = 'StudentLoanPeriod' THEN SettingValue END) AS StudentLoanPeriod,
        MAX(CASE WHEN SettingKey = 'FacultyLoanPeriod' THEN SettingValue END) AS FacultyLoanPeriod,
        MAX(CASE WHEN SettingKey = 'StaffLoanPeriod' THEN SettingValue END) AS StaffLoanPeriod,
        MAX(CASE WHEN SettingKey = 'GuestLoanPeriod' THEN SettingValue END) AS GuestLoanPeriod,
        MAX(CASE WHEN SettingKey = 'StudentBorrowLimit' THEN SettingValue END) AS StudentBorrowLimit,
        MAX(CASE WHEN SettingKey = 'FacultyBorrowLimit' THEN SettingValue END) AS FacultyBorrowLimit,
        MAX(CASE WHEN SettingKey = 'StaffBorrowLimit' THEN SettingValue END) AS StaffBorrowLimit,
        MAX(CASE WHEN SettingKey = 'GuestBorrowLimit' THEN SettingValue END) AS GuestBorrowLimit,
        MAX(CASE WHEN SettingKey = 'RenewalDays' THEN SettingValue END) AS RenewalDays,
        MAX(CASE WHEN SettingKey = 'MaxRenewals' THEN SettingValue END) AS MaxRenewals
    FROM LibrarySettings
    WHERE SettingKey IN ('StudentLoanPeriod', 'FacultyLoanPeriod', 'StaffLoanPeriod', 'GuestLoanPeriod',
                         'StudentBorrowLimit', 'FacultyBorrowLimit', 'StaffBorrowLimit', 'GuestBorrowLimit',
                         'RenewalDays', 'MaxRenewals');
END$$

-- Get Fines Settings
DROP PROCEDURE IF EXISTS SP_GetFinesSettings$$
CREATE PROCEDURE SP_GetFinesSettings()
BEGIN
    SELECT 
        MAX(CASE WHEN SettingKey = 'FineRatePerDay' THEN SettingValue END) AS FineRatePerDay,
        MAX(CASE WHEN SettingKey = 'GracePeriodDays' THEN SettingValue END) AS GracePeriodDays,
        MAX(CASE WHEN SettingKey = 'MaxFineAmount' THEN SettingValue END) AS MaxFineAmount,
        MAX(CASE WHEN SettingKey = 'LostBookFee' THEN SettingValue END) AS LostBookFee
    FROM LibrarySettings
    WHERE SettingKey IN ('FineRatePerDay', 'GracePeriodDays', 'MaxFineAmount', 'LostBookFee');
END$$

DELIMITER ;

