# Library Management System - Database Setup Guide

This guide will help you set up the database connection for running the Library Management System on a new PC.

## Prerequisites

1. **MySQL Server** (Version 5.7 or higher, or MySQL 8.0+)
2. **MySQL Workbench** (Optional, but recommended for database management)
3. **.NET Framework 4.7.2** or higher

---

## Step 1: Install MySQL Server

### Windows Installation:
1. Download MySQL Installer from: https://dev.mysql.com/downloads/installer/
2. Run the installer and select "MySQL Server"
3. During installation:
   - Choose "Developer Default" or "Server only"
   - Set root password (remember this password!)
   - Note the port number (default is 3306)
   - Complete the installation

### Verify Installation:
- Open Command Prompt and run: `mysql --version`
- Or check MySQL service in Windows Services (should be running)

---

## Step 2: Create the Database

You have two options:

### Option A: Using MySQL Workbench (Recommended)

1. Open MySQL Workbench
2. Connect to your MySQL server (use root credentials)
3. Open the file: `database_schema_complete.sql`
4. Execute the entire script (File → Run SQL Script)
5. Verify the database `LMS_DB` was created

### Option B: Using Command Line

1. Open Command Prompt
2. Navigate to MySQL bin directory (usually `C:\Program Files\MySQL\MySQL Server 8.0\bin`)
3. Run:
   ```bash
   mysql -u root -p < "path\to\database_schema_complete.sql"
   ```
4. Enter your root password when prompted

### Option C: Automatic Setup (Application will create it)

The application will automatically create the database and tables on first run if:
- MySQL server is running
- Connection string is correct
- User has CREATE DATABASE privileges

---

## Step 3: Configure Connection String

### For Local Database (Same PC):
1. Open `App.config` in the project root
2. Find the `<connectionStrings>` section
3. Update the connection string:

```xml
<connectionStrings>
    <add name="MySQLConnection" 
         connectionString="Server=localhost;Port=3306;Database=LMS_DB;Uid=root;Pwd=YOUR_PASSWORD;CharSet=utf8;" />
</connectionStrings>
```

**Replace `YOUR_PASSWORD` with your MySQL root password**

### For Remote Database (Different PC/Server):
1. Open `App.config`
2. Update the connection string:

```xml
<connectionStrings>
    <add name="MySQLConnection" 
         connectionString="Server=IP_ADDRESS_OR_HOSTNAME;Port=3306;Database=LMS_DB;Uid=USERNAME;Pwd=PASSWORD;CharSet=utf8;" />
</connectionStrings>
```

**Example:**
```xml
<connectionStrings>
    <add name="MySQLConnection" 
         connectionString="Server=192.168.1.100;Port=3306;Database=LMS_DB;Uid=library_user;Pwd=SecurePass123;CharSet=utf8;" />
</connectionStrings>
```

### Connection String Parameters:
- **Server**: IP address or hostname of MySQL server
  - `localhost` or `127.0.0.1` for local database
  - IP address (e.g., `192.168.1.100`) for remote database
  - Hostname (e.g., `mysql-server.company.com`) for remote database
- **Port**: MySQL port (default: 3306)
- **Database**: Database name (default: `LMS_DB`)
- **Uid**: MySQL username (default: `root` for local)
- **Pwd**: MySQL password
- **CharSet**: Character encoding (default: `utf8`)

---

## Step 4: Configure MySQL for Remote Access (If Needed)

If connecting from a different PC, you need to:

1. **Enable Remote Access in MySQL:**
   ```sql
   -- In MySQL, run these commands:
   CREATE USER 'library_user'@'%' IDENTIFIED BY 'your_password';
   GRANT ALL PRIVILEGES ON LMS_DB.* TO 'library_user'@'%';
   FLUSH PRIVILEGES;
   ```

2. **Configure MySQL Server:**
   - Edit `my.ini` (Windows) or `my.cnf` (Linux)
   - Find `bind-address` and change to:
     ```
     bind-address = 0.0.0.0
     ```
   - Restart MySQL service

3. **Configure Firewall:**
   - Allow port 3306 through Windows Firewall
   - Allow port 3306 through router firewall (if needed)

---

## Step 5: Test the Connection

1. Build and run the application
2. The application will automatically:
   - Create the database if it doesn't exist
   - Create all tables if they don't exist
   - Create stored procedures
   - Insert default admin and staff users

3. **Default Login Credentials:**
   - **Admin**: 
     - Email: `admin@library.com`
     - Password: `Admin123!`
   - **Staff**: 
     - Email: `staff@library.com`
     - Password: `Staff123!`

---

## Troubleshooting

### Connection Error: "Unable to connect to any of the specified MySQL hosts"
- Check if MySQL service is running
- Verify the server IP/address is correct
- Check if port 3306 is open in firewall
- Verify username and password are correct

### Connection Error: "Access denied for user"
- Verify username and password
- Check if user has privileges on the database
- For remote access, ensure user is allowed to connect from your IP

### Connection Error: "Unknown database 'LMS_DB'"
- The database doesn't exist
- Run `database_schema_complete.sql` to create it
- Or let the application create it automatically (ensure user has CREATE DATABASE privilege)

### Application Crashes on Startup
- Check if MySQL server is running
- Verify connection string in `App.config`
- Check Windows Event Viewer for detailed error messages

### Can't Find App.config
- After building, the config file is copied to `bin\Debug\` or `bin\Release\`
- Edit the config file in the build output folder, not the source folder
- Or edit the source `App.config` and rebuild

---

## Quick Setup Checklist

- [ ] MySQL Server installed and running
- [ ] Database `LMS_DB` created (or will be auto-created)
- [ ] Connection string updated in `App.config`
- [ ] MySQL credentials verified
- [ ] Firewall configured (if remote access)
- [ ] Application builds successfully
- [ ] Application connects to database on startup
- [ ] Can login with default credentials

---

## Security Recommendations

1. **Don't use root user in production:**
   - Create a dedicated user for the application
   - Grant only necessary privileges

2. **Use strong passwords:**
   - At least 12 characters
   - Mix of uppercase, lowercase, numbers, and symbols

3. **Restrict remote access:**
   - Only allow connections from specific IPs
   - Use VPN for remote connections

4. **Regular backups:**
   - Set up automated MySQL backups
   - Test restore procedures

---

## Support

If you encounter issues:
1. Check MySQL error logs
2. Check application debug output
3. Verify all prerequisites are installed
4. Review connection string format

---

## Connection String Examples

### Local Development:
```
Server=localhost;Port=3306;Database=LMS_DB;Uid=root;Pwd=mypassword;CharSet=utf8;
```

### Remote Server:
```
Server=192.168.1.50;Port=3306;Database=LMS_DB;Uid=library_app;Pwd=SecurePass123;CharSet=utf8;
```

### With Connection Timeout:
```
Server=localhost;Port=3306;Database=LMS_DB;Uid=root;Pwd=mypassword;CharSet=utf8;Connection Timeout=30;
```

### With SSL (if required):
```
Server=mysql.example.com;Port=3306;Database=LMS_DB;Uid=user;Pwd=pass;CharSet=utf8;SslMode=Required;
```

