# Library Management System - Setup Instructions

## 📋 Quick Start

1. **Read**: `QUICK_SETUP.md` - For fast setup on a new PC
2. **Detailed Guide**: `DATABASE_SETUP_GUIDE.md` - Complete setup instructions
3. **Database Schema**: `database_schema_complete.sql` - Complete database structure

## 🚀 Quick Setup (3 Steps)

### 1. Install MySQL Server
- Download from: https://dev.mysql.com/downloads/
- Install and remember your root password

### 2. Configure Connection
- Open `App.config`
- Update the password in the connection string:
  ```xml
  connectionString="Server=localhost;Port=3306;Database=LMS_DB;Uid=root;Pwd=YOUR_PASSWORD;CharSet=utf8;"
  ```

### 3. Create Database
- **Option A**: Run the application (auto-creates database)
- **Option B**: Run `database_schema_complete.sql` in MySQL Workbench
- **Option C**: Run `setup_database.bat` script

## 📁 Files Overview

| File | Description |
|------|-------------|
| `QUICK_SETUP.md` | Quick 3-step setup guide |
| `DATABASE_SETUP_GUIDE.md` | Detailed setup instructions |
| `database_schema_complete.sql` | Complete database schema |
| `App.config.template` | Configuration template with instructions |
| `setup_database.bat` | Automated setup script (Windows) |

## 🔧 Configuration

### Local Database (Same PC)
```xml
Server=localhost;Port=3306;Database=LMS_DB;Uid=root;Pwd=yourpassword;CharSet=utf8;
```

### Remote Database (Different PC)
```xml
Server=192.168.1.100;Port=3306;Database=LMS_DB;Uid=username;Pwd=password;CharSet=utf8;
```

## 🔐 Default Credentials

After setup, you can login with:

- **Administrator**:
  - Email: `admin@library.com`
  - Password: `Admin123!`

- **Staff**:
  - Email: `staff@library.com`
  - Password: `Staff123!`

## ❓ Need Help?

1. Check `DATABASE_SETUP_GUIDE.md` for troubleshooting
2. Verify MySQL service is running
3. Check connection string format
4. Ensure firewall allows port 3306 (for remote)

## 📝 Notes

- The application will automatically create the database on first run if it doesn't exist
- Make sure the MySQL user has CREATE DATABASE privileges
- For production, create a dedicated MySQL user (not root)
- Always backup your database regularly

