# Quick Setup Guide - Library Management System

## For Running on Another PC

### Step 1: Install MySQL
1. Download and install MySQL Server from https://dev.mysql.com/downloads/
2. Remember your root password during installation

### Step 2: Configure Database Connection
1. Open `App.config` file
2. Find this line:
   ```xml
   <add name="MySQLConnection" connectionString="Server=localhost;Port=3306;Database=LMS_DB;Uid=root;Pwd=Admin123!;CharSet=utf8;" />
   ```
3. Change `Pwd=Admin123!` to your MySQL root password:
   ```xml
   <add name="MySQLConnection" connectionString="Server=localhost;Port=3306;Database=LMS_DB;Uid=root;Pwd=YOUR_MYSQL_PASSWORD;CharSet=utf8;" />
   ```

### Step 3: Create Database (Choose One Method)

**Method A - Automatic (Easiest):**
- Just run the application - it will create everything automatically!

**Method B - Manual (Using MySQL Workbench):**
1. Open MySQL Workbench
2. Connect to your MySQL server
3. Open `database_schema_complete.sql`
4. Execute the script (Ctrl+Shift+Enter)

**Method C - Command Line:**
```bash
mysql -u root -p < database_schema_complete.sql
```

### Step 4: Run the Application
1. Build the project
2. Run the executable
3. Login with:
   - **Admin**: admin@library.com / Admin123!
   - **Staff**: staff@library.com / Staff123!

---

## For Remote Database Connection

If the database is on a different PC:

1. Update `App.config` connection string:
   ```xml
   <add name="MySQLConnection" 
        connectionString="Server=192.168.1.100;Port=3306;Database=LMS_DB;Uid=username;Pwd=password;CharSet=utf8;" />
   ```
   Replace:
   - `192.168.1.100` with the database server IP
   - `username` with MySQL username
   - `password` with MySQL password

2. Ensure MySQL allows remote connections
3. Open port 3306 in firewall

---

## Troubleshooting

**Can't connect?**
- Check MySQL service is running
- Verify password in App.config
- Check firewall settings

**Database not found?**
- Run `database_schema_complete.sql` manually
- Or let the app create it (check user has CREATE DATABASE privilege)

**Need more help?**
- See `DATABASE_SETUP_GUIDE.md` for detailed instructions

