# Library Management System - Administrator Guide

## Table of Contents
1. [Introduction](#introduction)
2. [System Setup](#system-setup)
3. [User Management](#user-management)
4. [Book Management](#book-management)
5. [Circulation Management](#circulation-management)
6. [Reports](#reports)
7. [Settings](#settings)
8. [Maintenance](#maintenance)

---

## Introduction

This guide is for administrators and library staff managing the Library Management System.

### Administrator Capabilities
- Full system access
- User and member management
- Book catalog management
- Circulation operations
- Report generation
- System configuration
- Fine management

---

## System Setup

### Initial Configuration

1. **Database Setup**
   - Run database scripts in order:
     - `001_Create_Database_Schema.sql`
     - `002_Fix_Schema_Issues.sql`
     - `003_Add_Performance_Indexes.sql`
   - Deploy all stored procedures

2. **Connection String**
   - Edit `App.config`
   - Update MySQL connection string with your database credentials

3. **Default Admin Account**
   - Email: `admin@umindanao.edu.ph`
   - Password: Set during initial setup
   - Role: Administrator

---

## User Management

### Creating Users

1. Navigate to **Members** section
2. Click **"Add New Member"**
3. Fill in required information:
   - First Name
   - Last Name
   - Email (must be unique)
   - Phone
   - Address
   - Member Type (Student/Faculty/Staff/Guest)
   - Status (Active/Suspended/Expired)
4. Click **"Register"**

### Managing Members

**View Members:**
- Use search to find specific members
- Filter by status or type
- View member details and borrowing history

**Update Member:**
- Click **"Edit"** on member row
- Modify information
- Save changes

**Suspend/Activate Member:**
- Change member status in edit form
- Suspended members cannot borrow books

---

## Book Management

### Adding Books

1. Navigate to **Catalog** section
2. Click **"Add Book"**
3. Enter book information:
   - Title (required)
   - Author (required)
   - ISBN (optional, unique)
   - Publisher
   - Publication Year
   - Category
   - Description
   - Total Copies
4. Click **"Save"**

### Managing Books

**Search Books:**
- Use search functionality
- Filter by category
- View availability

**Update Book:**
- Find book in catalog
- Click **"Edit"**
- Modify information
- Update copy counts if needed

**Delete Book:**
- Only if no active borrowings
- System will prevent deletion if books are borrowed

---

## Circulation Management

### Checkout Process

1. Navigate to **Circulation** section
2. Click **"Checkout"** button
3. Enter:
   - Member ID or search for member
   - Book ID or search for book
   - Loan period (days)
4. Click **"Checkout"**
5. System will:
   - Verify member eligibility
   - Check book availability
   - Create borrowing record
   - Update book availability

### Return Process

1. Navigate to **Circulation** section
2. Find the borrowing record
3. Click **"Return"** button
4. System will:
   - Calculate any fines
   - Update book availability
   - Mark record as returned

### Renewals

1. Find borrowing record
2. Click **"Renew"** button
3. System extends due date based on renewal settings

---

## Reports

### Available Reports

1. **Circulation Reports**
   - Daily borrowing statistics
   - Return statistics
   - Overdue books list
   - Popular books

2. **Member Reports**
   - Member type distribution
   - Member activity summary
   - Registration statistics

3. **Collection Reports**
   - Books by category
   - Collection statistics
   - Inventory overview

4. **Fines Reports**
   - Unpaid fines
   - Payment history
   - Revenue reports

### Generating Reports

1. Navigate to **Reports** section
2. Select report type
3. View charts and data
4. Click **"Export"** to save as:
   - CSV
   - Excel
   - HTML/PDF

---

## Settings

### General Settings

Configure library information:
- Library Name
- Email Address
- Phone Number
- Physical Address

### Notification Settings

Configure email notifications:
- Enable/disable email notifications
- Overdue reminders
- Reservation alerts
- Due date reminders
- Reminder days before due

### Borrowing Settings

Configure loan policies:
- Loan periods by member type:
  - Student: Default 14 days
  - Faculty: Default 30 days
  - Staff: Default 21 days
  - Guest: Default 7 days
- Borrowing limits by member type
- Renewal policies:
  - Renewal period (days)
  - Maximum renewals

### Fines Settings

Configure fine policies:
- Fine rate per day
- Grace period (days)
- Maximum fine amount
- Lost book fee

### Email Configuration

Configure SMTP settings for email notifications:
- SMTP Server
- SMTP Port
- SMTP Username
- SMTP Password
- Enable SSL

---

## Maintenance

### Database Maintenance

**Backup Database:**
- Regular backups recommended
- Use MySQL backup tools
- Backup before major updates

**Verify Data Integrity:**
- Run `004_Verify_Foreign_Keys.sql`
- Check for orphaned records
- Review error logs

### User Management

**Password Reset:**
- Users can reset via "Forgot Password"
- Admins can manually reset in database
- Use `SP_UpdateUserPassword` stored procedure

**Account Lockout:**
- Implemented for security
- Contact administrator if locked out

### System Updates

**Before Updates:**
1. Backup database
2. Test in development environment
3. Review migration scripts
4. Update stored procedures if needed

**After Updates:**
1. Verify all features work
2. Check error logs
3. Test critical workflows
4. Update documentation

---

## Security Best Practices

1. **Password Policy**
   - Enforce strong passwords
   - Regular password changes
   - No password sharing

2. **Access Control**
   - Role-based access
   - Regular access reviews
   - Disable inactive accounts

3. **Data Protection**
   - Regular backups
   - Secure database access
   - Encrypt sensitive data

4. **Audit Logging**
   - Monitor system logs
   - Review audit trails
   - Investigate anomalies

---

## Troubleshooting

### Common Issues

**Database Connection Errors:**
- Check connection string
- Verify MySQL server is running
- Check firewall settings
- Verify credentials

**Email Not Sending:**
- Check SMTP settings
- Verify email credentials
- Check network connectivity
- Review email service logs

**Performance Issues:**
- Check database indexes
- Review query performance
- Optimize stored procedures
- Consider database maintenance

---

## Support

For technical support:
- Review error logs: `error_log.txt`
- Check database foreign key verification
- Contact system administrator
- Review documentation

---

**Last Updated**: Current Date  
**Version**: 1.0.0

