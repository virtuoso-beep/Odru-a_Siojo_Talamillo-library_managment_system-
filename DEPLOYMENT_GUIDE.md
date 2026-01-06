# Library Management System - Deployment Guide

## Quick Start Deployment

### Prerequisites
- MySQL Server 5.7+ or 8.0+
- .NET Framework 4.7.2 or higher
- Windows OS (for WinForms application)
- MySQL user with CREATE, ALTER, INDEX, and FOREIGN KEY privileges

---

## Step-by-Step Deployment

### Step 1: Database Setup

#### 1.1 Create Database
```sql
CREATE DATABASE IF NOT EXISTS LibraryManagementDB 
CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

#### 1.2 Run Schema Scripts (In Order)

**Execute in MySQL Workbench or command line:**

```bash
# 1. Create base tables
mysql -u your_user -p LibraryManagementDB < "001_Create_Database_Schema.sql"

# 2. Fix schema issues and add missing tables/columns
mysql -u your_user -p LibraryManagementDB < "002_Fix_Schema_Issues.sql"

# 3. Add performance indexes (optional but recommended)
mysql -u your_user -p LibraryManagementDB < "003_Add_Performance_Indexes.sql"

# 4. Verify foreign keys (verification only)
mysql -u your_user -p LibraryManagementDB < "004_Verify_Foreign_Keys.sql"
```

**Or use MySQL Workbench:**
1. Open MySQL Workbench
2. Connect to your MySQL server
3. Open each SQL file in order
4. Execute each script

#### 1.3 Deploy Stored Procedures

**Option A: Use Deployment Script**
```bash
# Windows
deploy_all_procedures.bat

# Linux/Mac
chmod +x deploy_all_procedures.sh
./deploy_all_procedures.sh
```

**Option B: Manual Deployment**
Execute each file in `StoredProcedures/` folder in order:
1. `001_Authentication_Procedures.sql`
2. `002_Create_Staff_User.sql`
3. `003_Members_Procedures.sql`
4. `004_Books_Procedures.sql`
5. `005_Circulation_Procedures.sql`
6. `006_Fines_Procedures.sql`
7. `007_Dashboard_Procedures.sql`
8. `008_GetBookCategories.sql`

---

### Step 2: Application Configuration

#### 2.1 Update Connection String

Edit `App.config` or `Web.config`:

```xml
<connectionStrings>
  <add name="LibraryManagementDB" 
       connectionString="Server=localhost;Database=LibraryManagementDB;Uid=your_username;Pwd=your_password;CharSet=utf8mb4;" 
       providerName="MySql.Data.MySqlClient" />
</connectionStrings>
```

**Or update in code:**
- File: `Helper/MYSqlHelper.cs`
- Update the connection string method

#### 2.2 Build the Project

**Using Visual Studio:**
1. Open `Library Management System.sln`
2. Build → Build Solution (Ctrl+Shift+B)
3. Check for any build errors

**Using Command Line:**
```bash
msbuild "Library Management System.sln" /p:Configuration=Release
```

---

### Step 3: Create Default Users

#### 3.1 Create Admin User

Run this SQL script or use the application:

```sql
USE LibraryManagementDB;

-- Create admin user
INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role, IsActive, CreatedDate)
VALUES (
    'admin@library.com',
    'hashed_password_here', -- Use your password hashing method
    'Admin',
    'User',
    1, -- Librarian role
    1,
    NOW()
);

-- Create corresponding member if needed
INSERT INTO Members (UserId, MemberNumber, MemberType, Status, RegistrationDate)
VALUES (
    LAST_INSERT_ID(),
    'ADMIN001',
    1, -- Staff member type
    1, -- Active
    NOW()
);
```

**Or use the application:**
1. First run will prompt for admin user creation
2. Or use `002_Create_Staff_User.sql` stored procedure

---

### Step 4: Initial Setup

#### 4.1 Launch Application

1. Run the executable or start from Visual Studio
2. Login with admin credentials
3. Navigate to Settings → General
4. Configure library information:
   - Library Name
   - Address
   - Contact Email
   - Phone Number

#### 4.2 Configure Settings

**General Settings:**
- Library Name
- Address
- Contact Information

**Notification Settings:**
- Email preferences
- Reminder settings

**Borrowing Settings:**
- Loan periods by member type
- Borrowing limits
- Renewal policies

**Fines Settings:**
- Fine rates
- Grace periods
- Fine limits

---

### Step 5: Verify Installation

#### 5.1 Database Verification

Run verification script:
```sql
SOURCE 004_Verify_Foreign_Keys.sql;
```

Check for:
- ✓ All foreign keys exist
- ✓ No orphaned records
- ✓ All tables created

#### 5.2 Application Testing

Test these features:
- [ ] Login/Logout
- [ ] Add new member
- [ ] Add new book
- [ ] Borrow book
- [ ] Return book
- [ ] Generate reports
- [ ] Search functionality
- [ ] Export reports
- [ ] Save settings

---

## Troubleshooting

### Database Connection Issues

**Error: "Unable to connect to database"**
- Check MySQL service is running
- Verify connection string
- Check firewall settings
- Verify user permissions

**Error: "Access denied for user"**
- Verify username and password
- Check user has required privileges
- Grant privileges: `GRANT ALL ON LibraryManagementDB.* TO 'user'@'localhost';`

### Schema Issues

**Error: "Table doesn't exist"**
- Run `001_Create_Database_Schema.sql` first
- Check database name is correct
- Verify script executed successfully

**Error: "Column already exists"**
- Scripts check for existence, but if error occurs:
- Column was already added (safe to ignore)
- Or manually check: `DESCRIBE TableName;`

**Error: "Foreign key constraint fails"**
- Run `002_Fix_Schema_Issues.sql`
- Verify referenced tables exist
- Check data integrity

### Application Issues

**Error: "Stored procedure not found"**
- Deploy stored procedures
- Check procedure names match
- Verify database connection

**Error: "Export failed"**
- Check file permissions
- Verify export path exists
- Check disk space

**Error: "Settings not saving"**
- Verify `LibrarySettings` table exists
- Check database connection
- Review error logs

---

## Post-Deployment

### 1. Backup Strategy

**Regular Backups:**
```bash
# Daily backup
mysqldump -u user -p LibraryManagementDB > backup_$(date +%Y%m%d).sql

# Weekly backup
mysqldump -u user -p LibraryManagementDB > backup_week_$(date +%V).sql
```

### 2. Monitoring

**Error Logs:**
- Check `error_log.txt` in application directory
- Review MySQL error logs
- Monitor application performance

**Database Monitoring:**
- Check table sizes
- Monitor query performance
- Review slow query log

### 3. Maintenance

**Regular Tasks:**
- [ ] Backup database weekly
- [ ] Review error logs
- [ ] Update indexes if needed
- [ ] Clean up old audit logs
- [ ] Archive old circulation records

**Monthly Tasks:**
- [ ] Review system performance
- [ ] Check for orphaned records
- [ ] Verify foreign key integrity
- [ ] Update documentation

---

## Security Considerations

### 1. Database Security
- Use strong passwords
- Limit user privileges
- Enable SSL for connections
- Regular security updates

### 2. Application Security
- Secure connection strings
- Implement password policies
- Regular security audits
- Keep .NET Framework updated

### 3. Access Control
- Role-based access control
- Audit logging enabled
- Session management
- Password encryption

---

## Performance Optimization

### 1. Database Optimization
- Indexes are already added
- Regular table optimization
- Query performance monitoring
- Connection pooling

### 2. Application Optimization
- Efficient data loading
- Pagination for large datasets
- Caching where appropriate
- Resource cleanup

---

## Support

### Error Logs Location
- Application: `error_log.txt` in application directory
- Database: MySQL error log

### Documentation
- `DATABASE_OVERVIEW.md` - Database structure
- `COMPLETION_SUMMARY.md` - Feature summary
- `PROJECT_STATUS.md` - Current status
- `IMPLEMENTATION_REQUIREMENTS.md` - Requirements

---

## Success Criteria

✅ Database created and configured  
✅ All tables and stored procedures deployed  
✅ Application connects to database  
✅ Default admin user created  
✅ All features tested and working  
✅ Settings configured  
✅ Reports generating correctly  
✅ Export functionality working  

---

**Deployment Status:** Ready for Production

*Last Updated: Current Date*

