# Library Management System - Architecture Documentation

## Table of Contents
1. [System Overview](#system-overview)
2. [Architecture Layers](#architecture-layers)
3. [Database Schema](#database-schema)
4. [Service Layer](#service-layer)
5. [UI Layer](#ui-layer)
6. [Data Flow](#data-flow)
7. [Security Architecture](#security-architecture)

---

## System Overview

The Library Management System is a Windows Forms application built with:
- **.NET Framework 4.7.2**
- **C# WinForms**
- **MySQL Database**
- **Service-Oriented Architecture**

### Key Components

```
┌─────────────────────────────────────────┐
│         Presentation Layer (UI)         │
│  - DashboardForm                        │
│  - StaffDashboard                       │
│  - MembersDashboard                     │
│  - Authentication Forms                │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│          Service Layer                   │
│  - AuthenticationService                │
│  - BookService                          │
│  - MembersService                       │
│  - CirculationService                  │
│  - ReportsService                       │
│  - SearchService                        │
│  - SettingsService                      │
│  - EmailService                         │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│         Helper/Utility Layer            │
│  - ErrorHandler                         │
│  - ReportExportHelper                   │
│  - MYSqlHelper                          │
│  - StoredProcedureHelper                │
│  - AuditLogger                          │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│         Data Access Layer               │
│  - Stored Procedures                    │
│  - Direct SQL Queries                   │
│  - MySQL Connection                     │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│         Database Layer                  │
│  - MySQL Database                       │
│  - Tables, Views, Procedures            │
└─────────────────────────────────────────┘
```

---

## Architecture Layers

### 1. Presentation Layer (UI)

**Forms:**
- `SiginForm`: User authentication
- `DashboardForm`: Administrator dashboard
- `StaffDashboard`: Staff dashboard
- `MembersDashboard`: Member dashboard
- `ForgotPasswordForm`: Password reset request
- `ResetPasswordForm`: Password reset

**Responsibilities:**
- User interface rendering
- User input validation
- Display data from services
- Handle user interactions

### 2. Service Layer

**Services:**
- Business logic implementation
- Data validation
- Error handling
- Database interaction coordination

**Pattern:**
- Each service implements an interface
- Services are stateless
- Services use helper classes for database access

### 3. Helper/Utility Layer

**Helpers:**
- `ErrorHandler`: Centralized error logging
- `ReportExportHelper`: Report export functionality
- `MYSqlHelper`: Database connection management
- `StoredProcedureHelper`: Stored procedure execution
- `AuditLogger`: Audit trail logging
- `PlaceholderTextHelper`: UI placeholder text management

### 4. Data Access Layer

**Components:**
- Stored Procedures (MySQL)
- Direct SQL queries (for complex operations)
- Parameterized queries (SQL injection prevention)

**Stored Procedures:**
- Authentication procedures
- Member management procedures
- Book management procedures
- Circulation procedures
- Fines procedures
- Dashboard procedures
- Reports procedures
- Settings procedures
- Password reset procedures

### 5. Database Layer

**MySQL Database:**
- Tables: Users, Members, Books, Categories, BookCopies, Borrowings, Reservations, Fines, AuditLogs, LibrarySettings, PasswordResetTokens
- Indexes for performance
- Foreign keys for data integrity
- Stored procedures for business logic

---

## Database Schema

### Core Tables

**Users**
- UserId (PK)
- Email (Unique)
- PasswordHash
- FirstName, LastName
- Role
- IsActive
- CreatedDate

**Members**
- MemberId (PK)
- UserId (FK → Users)
- MemberNumber (Unique)
- MemberType
- Status
- RegistrationDate
- MembershipExpiryDate
- EmergencyContactName, EmergencyContactPhone

**Books**
- BookId (PK)
- ISBN (Unique)
- Title, Author
- Publisher, PublicationYear
- CategoryId (FK → Categories)
- TotalCopies, AvailableCopies
- CreatedDate, UpdatedDate

**Borrowings**
- BorrowingId (PK)
- MemberId (FK → Members)
- BookId (FK → Books)
- BorrowDate, DueDate, ReturnDate
- Status

**Fines**
- FineId (PK)
- MemberId (FK → Members)
- BorrowingId (FK → Borrowings)
- Amount
- Status (Unpaid/Paid/Waived)
- CreatedDate, PaidDate
- Reason

**LibrarySettings**
- SettingId (PK)
- SettingKey (Unique)
- SettingValue
- Description
- UpdatedDate

---

## Service Layer

### Service Pattern

Each service follows this pattern:

```csharp
public class ServiceName
{
    public ReturnType MethodName(Parameters)
    {
        try
        {
            using (var connection = new MySqlConnection(MYSqlHelper.GetConnectionString()))
            {
                connection.Open();
                // Database operations
                // Business logic
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.LogError(ex, "MethodName");
            // Handle error
        }
    }
}
```

### Service Responsibilities

1. **Business Logic**: Implement business rules
2. **Validation**: Validate input data
3. **Data Transformation**: Convert between database and domain models
4. **Error Handling**: Catch and log errors
5. **Transaction Management**: Manage database transactions

---

## UI Layer

### Form Structure

**Dashboard Forms:**
- Sidebar navigation
- Main content area
- Dynamic view switching
- Role-based menu items

**Form Lifecycle:**
1. Initialize components
2. Load user data
3. Setup event handlers
4. Display initial view
5. Handle user interactions
6. Update UI based on actions

### UI Patterns

**View Switching:**
- Hide/show panels
- Dynamic control creation
- State management

**Data Display:**
- DataGridView for tabular data
- Charts for statistics
- Custom cards for metrics

---

## Data Flow

### Authentication Flow

```
User Input → SiginForm
    ↓
AuthenticationService.Authenticate()
    ↓
Stored Procedure: SP_AuthenticateUser
    ↓
Database: Users table
    ↓
Return User object
    ↓
Set CurrentUser
    ↓
Navigate to Dashboard
```

### Checkout Flow

```
User Action → DashboardForm
    ↓
CirculationService.CheckoutBook()
    ↓
Validation (member status, book availability)
    ↓
Stored Procedure: SP_BorrowBook
    ↓
Database: Insert into Borrowings, Update Books
    ↓
Return success
    ↓
Update UI
```

### Search Flow

```
User Input → Search View
    ↓
SearchService.SearchBooks()
    ↓
Build SQL query with filters
    ↓
Execute query
    ↓
Return SearchResult list
    ↓
Display results in UI
```

---

## Security Architecture

### Authentication

- **Password Hashing**: SHA256
- **Role-Based Access**: Administrator, Staff, Member
- **Session Management**: 30-minute timeout
- **Password Reset**: Token-based with expiry

### Authorization

- **Role-Based Access Control (RBAC)**
- **UI Elements**: Hidden/shown based on role
- **Service Methods**: Role validation in services

### Data Protection

- **SQL Injection Prevention**: Parameterized queries
- **Input Validation**: Client and server-side
- **Error Handling**: No sensitive data in error messages
- **Audit Logging**: All critical operations logged

### Password Security

- **Complexity Requirements**:
  - Minimum 8 characters
  - Uppercase, lowercase, number, special character
  - No common weak patterns
- **Password Reset**:
  - Token-based (24-hour expiry)
  - One-time use tokens
  - Secure token generation

---

## Design Patterns

### Service Pattern
- Business logic encapsulated in services
- Services are stateless
- Interface-based design

### Repository Pattern (Partial)
- Stored procedures act as repositories
- Services interact with procedures

### Factory Pattern
- User creation based on role
- `CreateUserFromRole()` method

### Singleton Pattern
- Connection string management
- Error handler (static methods)

---

## Performance Considerations

### Database Optimization

- **Indexes**: On frequently queried columns
- **Stored Procedures**: Pre-compiled queries
- **Connection Pooling**: MySQL connection management
- **Query Optimization**: Efficient joins and filters

### UI Optimization

- **Lazy Loading**: Load data on demand
- **Pagination**: For large datasets
- **Caching**: User data caching
- **Async Operations**: Background data loading

---

## Extensibility

### Adding New Features

1. **New Service**: Create service class implementing interface
2. **Database**: Add tables/procedures if needed
3. **UI**: Add form/view in dashboard
4. **Integration**: Wire up service to UI

### Adding New Reports

1. Add method to `ReportsService`
2. Create stored procedure (optional)
3. Add report view in dashboard
4. Add export functionality

---

## Deployment Architecture

### Development Environment
- Local MySQL database
- Visual Studio development
- Debug mode

### Production Environment
- Production MySQL server
- Compiled application
- Release mode
- Error logging enabled
- Database backups

---

## Technology Stack

- **Framework**: .NET Framework 4.7.2
- **UI**: Windows Forms
- **Database**: MySQL 8.0+
- **ORM**: None (direct SQL)
- **Email**: System.Net.Mail (SMTP)
- **Charts**: System.Windows.Forms.DataVisualization

---

## Future Enhancements

### Potential Improvements

1. **Web API**: RESTful API for web/mobile clients
2. **Entity Framework**: ORM for database access
3. **Dependency Injection**: IoC container
4. **Unit Testing**: Comprehensive test coverage
5. **Logging Framework**: Structured logging (Serilog, NLog)
6. **Caching**: Redis or in-memory caching
7. **Background Jobs**: Scheduled tasks (email reminders)

---

**Last Updated**: Current Date  
**Version**: 1.0.0

