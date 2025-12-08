# 🔧 Fix: "Unknown database 'librarymanagementdb'" Error

## Problem
The error shows the database doesn't exist yet. You need to create it first.

## ✅ Quick Fix (2 Steps)

### Step 1: Create Database

**Option A: Using MySQL Workbench (Easiest)**

1. Open **MySQL Workbench**
2. Connect to your MySQL server:
   - Username: `root`
   - Password: `Admin123!`
3. Open and run: `Database Scripts/CreateDatabase_Quick.sql`
   - This creates the database
4. Then run: `Database Scripts/CreateUsersTable_MySQL.sql`
   - This creates tables and test accounts

**Option B: Using MySQL Command Line**

```bash
mysql -u root -p
# Enter password: Admin123!

# Then run:
CREATE DATABASE IF NOT EXISTS LibraryManagementDB;
USE LibraryManagementDB;

# Then run the full script:
source "Database Scripts/CreateUsersTable_MySQL.sql"
```

### Step 2: Verify Connection String

The connection string in `App.config` should be:
```xml
Server=localhost;Database=LibraryManagementDB;Uid=root;Pwd=Admin123!;Port=3306;CharSet=utf8;
```

Make sure:
- Database name is: `LibraryManagementDB` (case-sensitive in some MySQL configurations)
- Username is: `root`
- Password is: `Admin123!`

---

## 🚀 Complete Setup Process

1. **Create Database** (run `CreateDatabase_Quick.sql`)
2. **Create Tables** (run `CreateUsersTable_MySQL.sql`)
3. **Restart Application** (close and reopen)
4. **Test Login**

---

## ✅ After Database is Created

The application should work! Test with:
- **Administrator**: `admin@library.com` / `admin123`
- **Staff**: `staff@library.com` / `staff123`
- **Member**: `member@library.com` / `member123`

---

## 🔍 Troubleshooting

### Still getting "Unknown database" error?
- Check database name spelling (case-sensitive)
- Verify database was created: `SHOW DATABASES;` in MySQL
- Check connection string in `App.config`

### "Access denied" error?
- Verify password is `Admin123!`
- Check MySQL user permissions

### Connection timeout?
- Check MySQL server is running
- Verify port 3306 is correct

---

**Run the database creation script and you're good to go!** 🎯

