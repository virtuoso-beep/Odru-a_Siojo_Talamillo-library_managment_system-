# Library Management System

A comprehensive Windows Forms application for managing library operations including book cataloging, member management, circulation, reservations, fines, and reporting.

## Features

- ✅ User Authentication & Role-Based Access Control
- ✅ Book Catalog Management
- ✅ Member Management
- ✅ Circulation (Checkout/Return/Renewal)
- ✅ Reservations System
- ✅ Fines Management
- ✅ Advanced Search
- ✅ Comprehensive Reports
- ✅ Email Notifications
- ✅ Password Reset
- ✅ Settings Management

## Technology Stack

- **.NET Framework 4.7.2**
- **C# Windows Forms**
- **MySQL Database**
- **Service-Oriented Architecture**

## Project Structure

```
Library Management System/
├── Database/              # Database scripts and stored procedures
├── Forms/                 # UI Forms
│   ├── Authentication/    # Login, password reset
│   ├── Dashboard/        # Main dashboard
│   ├── members/         # Member dashboard
│   └── staff/          # Staff dashboard
├── Service/             # Business logic services
├── Models/              # Data models
├── Interfaces/          # Service interfaces
├── Helper/              # Utility classes
└── Properties/          # Application properties
```

## Getting Started

### Prerequisites

- Visual Studio 2019 or later
- .NET Framework 4.7.2
- MySQL Server 8.0+
- MySQL Workbench (for database setup)

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   ```

2. **Database Setup**
   - Create database: `LibraryManagementDB`
   - Run scripts in order:
     - `Database/001_Create_Database_Schema.sql`
     - `Database/002_Fix_Schema_Issues.sql`
     - `Database/003_Add_Performance_Indexes.sql`
   - Deploy stored procedures from `Database/StoredProcedures/`

3. **Configure Connection**
   - Edit `App.config`
   - Update MySQL connection string

4. **Build and Run**
   - Open solution in Visual Studio
   - Restore NuGet packages
   - Build solution (F6)
   - Run application (F5)

## Documentation

- [User Manual](Documentation/USER_MANUAL.md)
- [Admin Guide](Documentation/ADMIN_GUIDE.md)
- [Quick Start Guide](Documentation/QUICK_START_GUIDE.md)
- [API Documentation](Documentation/API_DOCUMENTATION.md)
- [Architecture](Documentation/ARCHITECTURE.md)

## Default Credentials

- **Email**: `admin@umindanao.edu.ph`
- **Password**: (Set during database setup)
- **Role**: Administrator

## License

This project is for educational purposes.

## Authors

Library Management System Development Team

---

For detailed setup instructions, see [QUICK_START_GUIDE.md](Documentation/QUICK_START_GUIDE.md)

