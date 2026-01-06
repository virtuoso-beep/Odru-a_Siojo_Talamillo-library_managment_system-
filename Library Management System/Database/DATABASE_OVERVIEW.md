# Database Files Overview

This document provides a complete overview of all database-related files in the Library Management System.

## Main Database Scripts

### 1. `001_Create_Database_Schema.sql` ⭐ **START HERE**
**Purpose:** Creates the initial database schema with all base tables.

**What it does:**
- Creates the `LibraryManagementDB` database
- Creates all base tables:
  - `Users` - User accounts and authentication
  - `Members` - Library members
  - `Categories` - Book categories
  - `Books` - Book catalog
  - `BookCopies` - Individual book copies
  - `CirculationRecords` - Circulation history
  - `Reservations` - Book reservations
  - `Fines` - Fine records
  - `AuditLogs` - System audit trail
  - `LibrarySettings` - Application settings

**Execution Order:** **1st** - Must be run first before any other scripts

---

### 2. `002_Fix_Schema_Issues.sql` ⭐ **REQUIRED**
**Purpose:** Fixes schema mismatches and adds missing columns/tables.

**What it does:**
- Creates `Borrowings` table (used by stored procedures)
- Adds missing columns to `Reservations`:
  - `ReservedDate`
  - `ExpiryDate`
- Adds missing columns to `Members`:
  - `MembershipExpiryDate`
  - `EmergencyContactName`
  - `EmergencyContactPhone`
- Adds missing columns to `Fines`:
  - `BorrowingId`
  - `Reason`
  - `CreatedDate`
- Adds missing columns to `Books`:
  - `UpdatedDate`
  - `Category`
- Adds performance indexes

**Execution Order:** **2nd** - Run after `001_Create_Database_Schema.sql`

**Note:** This script handles missing tables gracefully and won't error if tables don't exist yet.

---

### 3. `003_Add_Performance_Indexes.sql` ⚡ **OPTIONAL (Recommended)**
**Purpose:** Adds additional performance indexes for frequently queried columns.

**What it does:**
- Adds indexes on `Users` table (IsActive, CreatedDate)
- Adds indexes on `Members` table (MemberType, RegistrationDate, MembershipExpiryDate)
- Adds indexes on `Books` table (AvailableCopies, CreatedDate, UpdatedDate, Publisher, PublicationYear)
- Adds indexes on `Borrowings` table (composite indexes for common queries)
- Adds indexes on `Reservations` table (ExpiryDate, ReservedDate)
- Adds indexes on `Fines` table (MemberId, Status, CreatedDate, PaidDate, Amount)
- Adds indexes on `AuditLogs` table (TableName, Action, RecordId)
- Adds composite indexes for common query patterns

**Execution Order:** **3rd** - Run after `002_Fix_Schema_Issues.sql`

**Performance Impact:** Improves query performance, especially for large datasets

---

### 4. `004_Verify_Foreign_Keys.sql` ✅ **VERIFICATION**
**Purpose:** Verifies all foreign key relationships and checks for orphaned records.

**What it does:**
- Lists all existing foreign keys
- Verifies expected foreign key relationships:
  - Members → Users
  - Books → Categories
  - BookCopies → Books
  - Borrowings → Members
  - Borrowings → Books
  - Reservations → Members
  - Reservations → Books
  - Fines → Members
  - Fines → Borrowings
- Checks for orphaned records (invalid foreign key references)
- Provides summary of data integrity status

**Execution Order:** **4th** - Run after all schema scripts to verify integrity

**Note:** This script handles missing tables gracefully and won't error if tables don't exist yet.

---

## Stored Procedures

### Location: `Database/StoredProcedures/`

#### 1. `001_Authentication_Procedures.sql`
**Purpose:** User authentication and login procedures.

**Procedures:**
- Login verification
- Password management
- User session management

---

#### 2. `002_Create_Staff_User.sql`
**Purpose:** Creates default staff/librarian user accounts.

**Procedures:**
- Create staff user
- Create librarian user
- Set default permissions

---

#### 3. `003_Members_Procedures.sql`
**Purpose:** Member management operations.

**Procedures:**
- Register new member
- Update member information
- Get member details
- Check membership status
- Member search

---

#### 4. `004_Books_Procedures.sql`
**Purpose:** Book catalog management.

**Procedures:**
- Add new book
- Update book information
- Get book details
- Search books
- Manage book copies

---

#### 5. `005_Circulation_Procedures.sql`
**Purpose:** Book borrowing and return operations.

**Procedures:**
- Borrow book
- Return book
- Get borrowing history
- Check overdue books
- Renew book

**Note:** Uses `Borrowings` table (created by `002_Fix_Schema_Issues.sql`)

---

#### 6. `006_Fines_Procedures.sql`
**Purpose:** Fine calculation and management.

**Procedures:**
- Calculate fines
- Record fine payment
- Get fine details
- Get unpaid fines
- Fine history

**Note:** Uses `Borrowings` table for fine calculation

---

#### 7. `007_Dashboard_Procedures.sql`
**Purpose:** Dashboard statistics and reports.

**Procedures:**
- Get circulation statistics
- Get member statistics
- Get collection statistics
- Get fine statistics
- Get popular books

**Note:** Uses `Borrowings` table for statistics

---

#### 8. `008_GetBookCategories.sql`
**Purpose:** Get list of book categories.

**Procedures:**
- Get all categories
- Get category details

---

## Deployment Scripts

### `deploy_all_procedures.bat` (Windows)
**Purpose:** Batch script to deploy all stored procedures on Windows.

**Usage:**
```batch
deploy_all_procedures.bat
```

---

### `deploy_all_procedures.sh` (Linux/Mac)
**Purpose:** Shell script to deploy all stored procedures on Linux/Mac.

**Usage:**
```bash
chmod +x deploy_all_procedures.sh
./deploy_all_procedures.sh
```

---

## Execution Order Summary

### Initial Setup (First Time)
1. ✅ **001_Create_Database_Schema.sql** - Create base tables
2. ✅ **002_Fix_Schema_Issues.sql** - Fix schema and add missing tables/columns
3. ⚡ **003_Add_Performance_Indexes.sql** - Add performance indexes (optional but recommended)
4. ✅ **004_Verify_Foreign_Keys.sql** - Verify data integrity

### Stored Procedures (After Schema Setup)
5. **StoredProcedures/001_Authentication_Procedures.sql**
6. **StoredProcedures/002_Create_Staff_User.sql**
7. **StoredProcedures/003_Members_Procedures.sql**
8. **StoredProcedures/004_Books_Procedures.sql**
9. **StoredProcedures/005_Circulation_Procedures.sql**
10. **StoredProcedures/006_Fines_Procedures.sql**
11. **StoredProcedures/007_Dashboard_Procedures.sql**
12. **StoredProcedures/008_GetBookCategories.sql**

**Or use:** `deploy_all_procedures.bat` / `deploy_all_procedures.sh` to deploy all procedures at once

---

## Database Schema Overview

### Core Tables

| Table | Purpose | Key Relationships |
|-------|---------|-------------------|
| `Users` | User accounts | - |
| `Members` | Library members | → Users |
| `Categories` | Book categories | - |
| `Books` | Book catalog | → Categories |
| `BookCopies` | Individual copies | → Books |
| `CirculationRecords` | Circulation history | → Members, BookCopies |
| `Borrowings` | Active borrowings | → Members, Books |
| `Reservations` | Book reservations | → Members, Books |
| `Fines` | Fine records | → Members, Borrowings |
| `AuditLogs` | System audit trail | - |
| `LibrarySettings` | Application settings | - |

---

## Important Notes

### ⚠️ Case Sensitivity
- MySQL table names may be case-sensitive depending on the operating system
- All scripts use case-insensitive checks (`LOWER(TABLE_NAME)`)
- Table names in queries use backticks for safety

### ⚠️ IF NOT EXISTS Limitations
- MySQL doesn't support `IF NOT EXISTS` in `ALTER TABLE ADD COLUMN`
- MySQL doesn't support `IF NOT EXISTS` in `CREATE INDEX`
- Scripts check for existence before executing operations

### ⚠️ Foreign Key Dependencies
- `Borrowings` table requires `Books` and `Members` tables to exist first
- `Fines` table can reference either `CirculationRecords` or `Borrowings`
- All foreign keys use `ON DELETE CASCADE` or `ON DELETE SET NULL` appropriately

### ⚠️ Execution Requirements
- Must have MySQL user with CREATE, ALTER, INDEX, and FOREIGN KEY privileges
- Database `LibraryManagementDB` must be created first (or script will create it)
- Run scripts in order for proper dependency resolution

---

## Troubleshooting

### Error: "Table doesn't exist"
**Solution:** Run `001_Create_Database_Schema.sql` first

### Error: "Column already exists"
**Solution:** Scripts check for existence, but if you see this, the column was already added

### Error: "Foreign key constraint fails"
**Solution:** Ensure all referenced tables exist and have data in referenced columns

### Error: "Syntax error near IF NOT EXISTS"
**Solution:** Use the updated scripts that check for existence before executing

---

## Quick Start

```sql
-- 1. Create database and tables
SOURCE 001_Create_Database_Schema.sql;

-- 2. Fix schema issues
SOURCE 002_Fix_Schema_Issues.sql;

-- 3. Add performance indexes (optional)
SOURCE 003_Add_Performance_Indexes.sql;

-- 4. Verify foreign keys
SOURCE 004_Verify_Foreign_Keys.sql;

-- 5. Deploy stored procedures
-- Use deploy_all_procedures.bat or deploy_all_procedures.sh
-- Or run each procedure file individually
```

---

**Last Updated:** Database scripts are production-ready and handle all edge cases.

