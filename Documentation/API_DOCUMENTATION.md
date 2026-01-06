# Library Management System - API Documentation

## Table of Contents
1. [Overview](#overview)
2. [Authentication Service](#authentication-service)
3. [Book Service](#book-service)
4. [Members Service](#members-service)
5. [Circulation Service](#circulation-service)
6. [Reports Service](#reports-service)
7. [Search Service](#search-service)
8. [Settings Service](#settings-service)
9. [Email Service](#email-service)

---

## Overview

The Library Management System uses a service-oriented architecture with the following services:

- **AuthenticationService**: User authentication and password management
- **BookService**: Book catalog management
- **MembersService**: Member management
- **CirculationService**: Book checkout/return operations
- **ReportsService**: Report generation
- **SearchService**: Book search functionality
- **SettingsService**: Application settings management
- **EmailService**: Email notification system

---

## Authentication Service

### Methods

#### `Authenticate(string email, string password, UserRole expectedRole)`
Authenticates a user and returns a User object.

**Parameters:**
- `email` (string): User's email address
- `password` (string): User's password
- `expectedRole` (UserRole): Expected user role (Administrator, Staff, Member)

**Returns:** `User` object if successful, `null` if authentication fails

**Example:**
```csharp
var authService = new AuthenticationService();
var user = authService.Authenticate("user@example.com", "password123", UserRole.Member);
```

#### `RequestPasswordReset(string email)`
Requests a password reset token for a user.

**Parameters:**
- `email` (string): User's email address

**Returns:** Reset token (string) if user exists, `null` otherwise

**Example:**
```csharp
string token = authService.RequestPasswordReset("user@example.com");
```

#### `ValidateResetToken(string token)`
Validates a password reset token.

**Parameters:**
- `token` (string): Reset token

**Returns:** Tuple `(bool IsValid, int? UserId, string Email)`

**Example:**
```csharp
var (isValid, userId, email) = authService.ValidateResetToken(token);
```

#### `ResetPassword(string token, string newPassword)`
Resets a user's password using a reset token.

**Parameters:**
- `token` (string): Reset token
- `newPassword` (string): New password (must meet complexity requirements)

**Returns:** `bool` - true if successful

**Example:**
```csharp
bool success = authService.ResetPassword(token, "NewPassword123!");
```

---

## Book Service

### Methods

#### `GetAllBooks(string searchText, string categoryFilter)`
Retrieves all books with optional filtering.

**Parameters:**
- `searchText` (string): Search term (optional)
- `categoryFilter` (string): Category filter (optional)

**Returns:** `List<Book>` - List of books

#### `GetBookById(string bookId)`
Retrieves a book by ID.

**Parameters:**
- `bookId` (string): Book ID

**Returns:** `Book` object or `null`

#### `AddBook(Book book)`
Adds a new book to the catalog.

**Parameters:**
- `book` (Book): Book object with all details

**Returns:** `bool` - true if successful

#### `UpdateBook(string bookId, Book book)`
Updates an existing book.

**Parameters:**
- `bookId` (string): Book ID
- `book` (Book): Updated book object

**Returns:** `bool` - true if successful

#### `DeleteBook(string bookId)`
Deletes a book (only if no active borrowings).

**Parameters:**
- `bookId` (string): Book ID

**Returns:** `bool` - true if successful

---

## Members Service

### Methods

#### `GetMembers(string searchText, string statusFilter, string typeFilter)`
Retrieves members with optional filtering.

**Parameters:**
- `searchText` (string): Search term
- `statusFilter` (string): Status filter (Active/Suspended/Expired)
- `typeFilter` (string): Type filter (Student/Faculty/Staff/Guest)

**Returns:** `List<MemberInfo>` - List of members

#### `RegisterMember(...)`
Registers a new member.

**Parameters:**
- `firstName`, `lastName`, `email`, `phone`, `address`, `memberType`, `status`

**Returns:** `bool` - true if successful

#### `UpdateMember(string memberId, ...)`
Updates member information.

**Parameters:**
- `memberId` and updated fields

**Returns:** `bool` - true if successful

#### `GetMemberStatistics()`
Gets member statistics.

**Returns:** `MemberStatistics` object

---

## Circulation Service

### Methods

#### `CheckoutBook(string memberId, string bookId, int loanPeriodDays)`
Checks out a book to a member.

**Parameters:**
- `memberId` (string): Member ID
- `bookId` (string): Book ID
- `loanPeriodDays` (int): Loan period in days

**Returns:** `bool` - true if successful

**Throws:** Exception if member/book not found, member suspended, or book unavailable

#### `ReturnBook(string borrowingId)`
Returns a borrowed book.

**Parameters:**
- `borrowingId` (string): Borrowing record ID

**Returns:** `bool` - true if successful

#### `RenewBook(string borrowingId)`
Renews a borrowed book.

**Parameters:**
- `borrowingId` (string): Borrowing record ID

**Returns:** `bool` - true if successful

#### `GetBorrowings(string memberId, string statusFilter)`
Gets borrowing records.

**Parameters:**
- `memberId` (string): Member ID (optional)
- `statusFilter` (string): Status filter (optional)

**Returns:** `List<BorrowingRecord>` - List of borrowing records

---

## Reports Service

### Methods

#### `GetDailyCirculationData(DateTime startDate, DateTime endDate)`
Gets daily circulation statistics.

**Parameters:**
- `startDate` (DateTime): Start date
- `endDate` (DateTime): End date

**Returns:** `List<DailyCirculationData>` - Daily statistics

#### `GetPopularBooks(int limit, int daysRange)`
Gets most popular books.

**Parameters:**
- `limit` (int): Maximum number of results
- `daysRange` (int): Number of days to look back

**Returns:** `List<PopularBook>` - Popular books list

#### `GetOverdueBooks()`
Gets list of overdue books.

**Returns:** `List<OverdueBook>` - Overdue books list

#### `GetMemberTypeDistribution()`
Gets member distribution by type.

**Returns:** `List<MemberTypeDistribution>` - Distribution data

#### `GetMemberActivitySummary()`
Gets member activity summary.

**Returns:** `MemberActivitySummary` - Summary statistics

#### `GetCollectionStatistics()`
Gets collection statistics.

**Returns:** `CollectionStatistics` - Collection stats

#### `GetCollectionByCategory()`
Gets books grouped by category.

**Returns:** `List<CategoryCollection>` - Category breakdown

#### `GetFineReportData()`
Gets comprehensive fine report data.

**Returns:** `FineReportData` - Fine report information

---

## Search Service

### Methods

#### `SearchBooks(string searchText, SearchFilters filters)`
Searches for books with filters.

**Parameters:**
- `searchText` (string): Search term
- `filters` (SearchFilters): Optional filters

**Returns:** `List<SearchResult>` - Search results

**SearchFilters Properties:**
- `Category` (string): Filter by category
- `AvailableOnly` (bool): Only available books
- `MinYear` (int?): Minimum publication year
- `MaxYear` (int?): Maximum publication year
- `Publisher` (string): Filter by publisher

#### `SearchByTitle(string title)`
Searches books by title.

**Parameters:**
- `title` (string): Book title

**Returns:** `List<SearchResult>`

#### `SearchByAuthor(string author)`
Searches books by author.

**Parameters:**
- `author` (string): Author name

**Returns:** `List<SearchResult>`

#### `SearchByISBN(string isbn)`
Searches books by ISBN.

**Parameters:**
- `isbn` (string): ISBN

**Returns:** `List<SearchResult>`

#### `GetAvailableCategories()`
Gets list of available categories.

**Returns:** `List<string>` - Category names

#### `GetAvailablePublishers()`
Gets list of available publishers.

**Returns:** `List<string>` - Publisher names

---

## Settings Service

### Methods

#### `GetLibraryInfo()` / `SaveLibraryInfo(LibraryInfo info)`
Gets/saves library information.

**LibraryInfo Properties:**
- `LibraryName` (string)
- `Email` (string)
- `Phone` (string)
- `Address` (string)

#### `GetNotificationSettings()` / `SaveNotificationSettings(NotificationSettings settings)`
Gets/saves notification settings.

**NotificationSettings Properties:**
- `EmailNotifications` (bool)
- `OverdueReminders` (bool)
- `ReservationAlerts` (bool)
- `DueDateReminders` (bool)
- `ReminderDaysBeforeDue` (int)

#### `GetBorrowingSettings()` / `SaveBorrowingSettings(BorrowingSettings settings)`
Gets/saves borrowing settings.

**BorrowingSettings Properties:**
- `StudentLoanPeriod`, `FacultyLoanPeriod`, `StaffLoanPeriod`, `GuestLoanPeriod` (int)
- `StudentBorrowLimit`, `FacultyBorrowLimit`, `StaffBorrowLimit`, `GuestBorrowLimit` (int)
- `RenewalDays` (int)
- `MaxRenewals` (int)

#### `GetFinesSettings()` / `SaveFinesSettings(FinesSettings settings)`
Gets/saves fines settings.

**FinesSettings Properties:**
- `FineRatePerDay` (decimal)
- `GracePeriodDays` (int)
- `MaxFineAmount` (decimal)
- `LostBookFee` (decimal)

---

## Email Service

### Methods

#### `SendEmail(string toEmail, string subject, string body, bool isHtml)`
Sends a generic email.

**Parameters:**
- `toEmail` (string): Recipient email
- `subject` (string): Email subject
- `body` (string): Email body
- `isHtml` (bool): Whether body is HTML

**Returns:** `bool` - true if sent successfully

#### `SendOverdueReminder(string memberEmail, string memberName, string bookTitle, DateTime dueDate, decimal fineAmount)`
Sends overdue book reminder.

**Returns:** `bool` - true if sent

#### `SendDueDateReminder(string memberEmail, string memberName, string bookTitle, DateTime dueDate, int daysUntilDue)`
Sends due date reminder.

**Returns:** `bool` - true if sent

#### `SendReservationAlert(string memberEmail, string memberName, string bookTitle)`
Sends reservation available alert.

**Returns:** `bool` - true if sent

#### `SendPasswordResetEmail(string userEmail, string resetToken, string resetLink)`
Sends password reset email.

**Returns:** `bool` - true if sent

---

## Error Handling

All services use centralized error handling via `ErrorHandler` class:

```csharp
try
{
    // Service operation
}
catch (Exception ex)
{
    ErrorHandler.LogError(ex, "MethodName");
    // Handle error
}
```

Error logs are written to `error_log.txt`.

---

## Data Models

### User Roles
- `Administrator` (0): Full system access
- `Staff` (1): Limited access (no settings)
- `Member` (2): Member-only access

### Member Types
- `Student`
- `Faculty`
- `Staff`
- `Guest`

### Member Status
- `Active` (1)
- `Inactive` (2)
- `Suspended` (3)
- `Expired` (4)

---

## Best Practices

1. **Always validate input** before calling service methods
2. **Handle exceptions** properly
3. **Use transactions** for multi-step operations
4. **Log errors** for debugging
5. **Check return values** before proceeding
6. **Use parameterized queries** (already implemented in services)

---

**Last Updated**: Current Date  
**Version**: 1.0.0

