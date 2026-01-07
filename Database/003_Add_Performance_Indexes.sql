USE LibraryManagementDB;

-- This script adds additional performance indexes for frequently queried columns
-- It uses IF NOT EXISTS pattern where possible, or checks before creating

-- Indexes for Users table
CREATE INDEX IF NOT EXISTS idx_users_active ON Users(IsActive);
CREATE INDEX IF NOT EXISTS idx_users_created_date ON Users(CreatedDate);

-- Indexes for Members table (additional to existing ones)
CREATE INDEX IF NOT EXISTS idx_members_type ON Members(MemberType);
CREATE INDEX IF NOT EXISTS idx_members_registration_date ON Members(RegistrationDate);
CREATE INDEX IF NOT EXISTS idx_members_expiry_status ON Members(MembershipExpiryDate, Status);

-- Indexes for Books table (additional to existing ones)
CREATE INDEX IF NOT EXISTS idx_books_available ON Books(AvailableCopies);
CREATE INDEX IF NOT EXISTS idx_books_created_date ON Books(CreatedDate);
CREATE INDEX IF NOT EXISTS idx_books_updated_date ON Books(UpdatedDate);
CREATE INDEX IF NOT EXISTS idx_books_publisher ON Books(Publisher);
CREATE INDEX IF NOT EXISTS idx_books_publication_year ON Books(PublicationYear);

-- Indexes for Borrowings table (additional to existing ones)
CREATE INDEX IF NOT EXISTS idx_borrowings_member_status ON Borrowings(MemberId, Status);
CREATE INDEX IF NOT EXISTS idx_borrowings_book_status ON Borrowings(BookId, Status);
CREATE INDEX IF NOT EXISTS idx_borrowings_due_status ON Borrowings(DueDate, Status);
CREATE INDEX IF NOT EXISTS idx_borrowings_return_date ON Borrowings(ReturnDate);

-- Indexes for Reservations table (additional to existing ones)
CREATE INDEX IF NOT EXISTS idx_reservations_expiry_status ON Reservations(ExpiryDate, Status);
CREATE INDEX IF NOT EXISTS idx_reservations_reserved_date ON Reservations(ReservedDate);
CREATE INDEX IF NOT EXISTS idx_reservations_notification ON Reservations(NotificationSent, Status);

-- Indexes for Fines table (additional to existing ones)
CREATE INDEX IF NOT EXISTS idx_fines_member_status ON Fines(MemberId, Status);
CREATE INDEX IF NOT EXISTS idx_fines_created_date ON Fines(CreatedDate);
CREATE INDEX IF NOT EXISTS idx_fines_paid_date ON Fines(PaidDate);
CREATE INDEX IF NOT EXISTS idx_fines_amount ON Fines(Amount);

-- Indexes for AuditLogs table (additional to existing ones)
CREATE INDEX IF NOT EXISTS idx_auditlogs_table_action ON AuditLogs(TableName, Action);
CREATE INDEX IF NOT EXISTS idx_auditlogs_record ON AuditLogs(TableName, RecordId);

-- Composite indexes for common query patterns
-- These help with queries that filter by multiple columns

-- For finding active members with specific criteria
CREATE INDEX IF NOT EXISTS idx_members_active_type ON Members(Status, MemberType, MembershipExpiryDate);

-- For finding available books in a category
CREATE INDEX IF NOT EXISTS idx_books_category_available ON Books(CategoryId, AvailableCopies);

-- For finding overdue borrowings
CREATE INDEX IF NOT EXISTS idx_borrowings_overdue ON Borrowings(ReturnDate, DueDate, Status);

-- For finding unpaid fines by member
CREATE INDEX IF NOT EXISTS idx_fines_unpaid_member ON Fines(MemberId, Status, Amount);

-- Verification query
SELECT 
    TABLE_NAME,
    INDEX_NAME,
    COLUMN_NAME,
    SEQ_IN_INDEX,
    NON_UNIQUE
FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = 'LibraryManagementDB'
AND TABLE_NAME IN ('Users', 'Members', 'Books', 'Borrowings', 'Reservations', 'Fines', 'AuditLogs')
ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX;

