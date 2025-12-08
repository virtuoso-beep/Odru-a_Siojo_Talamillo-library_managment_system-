# 🚀 Quick Start Guide

## Final Steps to Complete Setup

### Step 1: Install MySQL NuGet Package ⚠️ REQUIRED

**In Visual Studio:**
1. Right-click on **"Library Management System"** project in Solution Explorer
2. Select **"Manage NuGet Packages..."**
3. Click on **"Browse"** tab
4. Search for: **`MySql.Data`**
5. Select version **8.0.33** (or latest 8.x version)
6. Click **"Install"**
7. Accept any license agreements

**OR using Package Manager Console:**
```powershell
Install-Package MySql.Data -Version 8.0.33
```

### Step 2: Restore Packages

After installing, Visual Studio should automatically restore packages. If not:
- Right-click solution → **"Restore NuGet Packages"**

### Step 3: Create Database

1. **Open MySQL Workbench** (or MySQL command line)
2. **Connect** to your MySQL server:
   - Host: `localhost`
   - Username: `root`
   - Password: `Admin123!`
3. **Open** the file: `Database Scripts/CreateUsersTable_MySQL.sql`
4. **Execute** the entire script (Run button or F5)
5. Verify success message: "Database setup completed!"

### Step 4: Build Project

1. In Visual Studio, press **Ctrl+Shift+B** (or Build → Build Solution)
2. Check for any errors
3. If you see "MySql.Data" not found error → Go back to Step 1

### Step 5: Run and Test

1. Press **F5** to run the application
2. The SignIn form should appear
3. Test with these accounts:

| Role | Email | Password |
|------|-------|----------|
| **Administrator** | admin@library.com | admin123 |
| **Staff** | staff@library.com | staff123 |
| **Member** | member@library.com | member123 |

## ✅ Verification

After successful login, you should see:
- Welcome message with user's name
- Role description
- Form closes (Dashboard will be created next)

## 🐛 Common Issues

### Issue: "Could not load file or assembly 'MySql.Data'"
**Fix**: Install MySql.Data NuGet package (Step 1)

### Issue: "Unable to connect to MySQL server"
**Fix**: 
- Check MySQL server is running
- Verify password is `Admin123!`
- Check connection string in `App.config`

### Issue: "Unknown database 'LibraryManagementDB'"
**Fix**: Run the SQL script (Step 3)

### Issue: "Access denied for user 'root'@'localhost'"
**Fix**: 
- Verify MySQL password: `Admin123!`
- Check user has CREATE DATABASE permission

## 📝 Next Steps After Login Works

1. Create Dashboard form
2. Implement role-based navigation
3. Build Member Management module
4. Build Cataloging module
5. Build Circulation module

---

**You're almost there! Just install the NuGet package and run the database script!** 🎯

