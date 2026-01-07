USE LibraryManagementDB;
CREATE TABLE IF NOT EXISTS Users (
    UserId INT AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    Role TINYINT NOT NULL DEFAULT 3,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_email (Email),
    INDEX idx_role (Role)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS Members (
    MemberId INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    MemberNumber VARCHAR(50) NOT NULL UNIQUE,
    MemberType TINYINT NOT NULL,
    Status TINYINT NOT NULL DEFAULT 1,
    RegistrationDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    Phone VARCHAR(20),
    Address TEXT,
    IdNumber VARCHAR(50),
    DateOfBirth DATE,
    Gender VARCHAR(10),
    Department VARCHAR(100),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    INDEX idx_member_number (MemberNumber),
    INDEX idx_user_id (UserId),
    INDEX idx_status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS Categories (
    CategoryId INT AUTO_INCREMENT PRIMARY KEY,
    CategoryName VARCHAR(100) NOT NULL UNIQUE,
    Description TEXT,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS Books (
    BookId INT AUTO_INCREMENT PRIMARY KEY,
    ISBN VARCHAR(20) UNIQUE,
    Title VARCHAR(255) NOT NULL,
    Author VARCHAR(255) NOT NULL,
    Publisher VARCHAR(255),
    PublicationYear INT,
    CategoryId INT,
    Description TEXT,
    TotalCopies INT DEFAULT 0,
    AvailableCopies INT DEFAULT 0,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId) ON DELETE SET NULL,
    INDEX idx_isbn (ISBN),
    INDEX idx_title (Title),
    INDEX idx_author (Author),
    INDEX idx_category (CategoryId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS BookCopies (
    CopyId INT AUTO_INCREMENT PRIMARY KEY,
    BookId INT NOT NULL,
    CopyNumber VARCHAR(50) NOT NULL,
    Status VARCHAR(20) DEFAULT 'Available',
    Location VARCHAR(100),
    PurchaseDate DATE,
    FOREIGN KEY (BookId) REFERENCES Books(BookId) ON DELETE CASCADE,
    INDEX idx_book_id (BookId),
    INDEX idx_status (Status),
    UNIQUE KEY unique_copy (BookId, CopyNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS CirculationRecords (
    RecordId INT AUTO_INCREMENT PRIMARY KEY,
    MemberId INT NOT NULL,
    CopyId INT NOT NULL,
    BorrowDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    DueDate DATETIME NOT NULL,
    ReturnDate DATETIME NULL,
    Status VARCHAR(20) DEFAULT 'Borrowed',
    FineAmount DECIMAL(10,2) DEFAULT 0.00,
    Notes TEXT,
    FOREIGN KEY (MemberId) REFERENCES Members(MemberId) ON DELETE CASCADE,
    FOREIGN KEY (CopyId) REFERENCES BookCopies(CopyId) ON DELETE CASCADE,
    INDEX idx_member_id (MemberId),
    INDEX idx_copy_id (CopyId),
    INDEX idx_status (Status),
    INDEX idx_due_date (DueDate)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS Reservations (
    ReservationId INT AUTO_INCREMENT PRIMARY KEY,
    MemberId INT NOT NULL,
    BookId INT NOT NULL,
    ReservationDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    Status VARCHAR(20) DEFAULT 'Pending',
    NotificationSent BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (MemberId) REFERENCES Members(MemberId) ON DELETE CASCADE,
    FOREIGN KEY (BookId) REFERENCES Books(BookId) ON DELETE CASCADE,
    INDEX idx_member_id (MemberId),
    INDEX idx_book_id (BookId),
    INDEX idx_status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS Fines (
    FineId INT AUTO_INCREMENT PRIMARY KEY,
    MemberId INT NOT NULL,
    RecordId INT,
    Amount DECIMAL(10,2) NOT NULL,
    FineType VARCHAR(50),
    Status VARCHAR(20) DEFAULT 'Unpaid',
    DueDate DATE,
    PaidDate DATE,
    Notes TEXT,
    FOREIGN KEY (MemberId) REFERENCES Members(MemberId) ON DELETE CASCADE,
    FOREIGN KEY (RecordId) REFERENCES CirculationRecords(RecordId) ON DELETE SET NULL,
    INDEX idx_member_id (MemberId),
    INDEX idx_status (Status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS AuditLogs (
    LogId INT AUTO_INCREMENT PRIMARY KEY,
    UserEmail VARCHAR(255),
    Action VARCHAR(100) NOT NULL,
    TableName VARCHAR(100),
    RecordId INT,
    OldValues TEXT,
    NewValues TEXT,
    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    IPAddress VARCHAR(50),
    INDEX idx_user_email (UserEmail),
    INDEX idx_action (Action),
    INDEX idx_timestamp (Timestamp)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
CREATE TABLE IF NOT EXISTS LibrarySettings (
    SettingId INT AUTO_INCREMENT PRIMARY KEY,
    SettingKey VARCHAR(100) NOT NULL UNIQUE,
    SettingValue TEXT,
    Description TEXT,
    UpdatedDate DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
INSERT INTO Categories (CategoryName, Description) VALUES
('Fiction', 'Fictional literature and novels'),
('Non-Fiction', 'Non-fictional books and reference materials'),
('Science', 'Scientific books and research materials'),
('History', 'Historical books and documents'),
('Technology', 'Technology and computer science books'),
('Literature', 'Classic and modern literature'),
('Education', 'Educational and academic books'),
('Reference', 'Reference books and dictionaries')
ON DUPLICATE KEY UPDATE CategoryName = CategoryName;
