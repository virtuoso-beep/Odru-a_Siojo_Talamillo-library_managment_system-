# MySQL Setup Instructions

## ✅ Configuration Complete

Your application has been configured to use **MySQL** with the password **Admin123!**

## 📋 Setup Steps

### Step 1: Install MySQL Connector/NET

You need to install the MySQL .NET connector. You have two options:

#### Option A: Using NuGet Package Manager (Recommended)
1. Right-click on your project in Solution Explorer
2. Select "Manage NuGet Packages"
3. Search for "MySql.Data"
4. Install version 8.0.33 or later

#### Option B: Manual Installation
1. Download MySQL Connector/NET from: https://dev.mysql.com/downloads/connector/net/
2. Install it on your system
3. Add reference to `MySql.Data.dll` in your project

### Step 2: Update Connection String (if needed)

The connection string in `App.config` is already configured with:
- **Server**: localhost
- **Database**: LibraryManagementDB
- **Username**: root
- **Password**: Admin123!
- **Port**: 3306

If your MySQL server is on a different host or uses different credentials, update `App.config`:

```xml
<add name="LibraryDB" 
     connectionString="Server=YOUR_SERVER;Database=LibraryManagementDB;Uid=YOUR_USER;Pwd=YOUR_PASSWORD;Port=3306;CharSet=utf8;" 
     providerName="MySql.Data.MySqlClient" />
```

### Step 3: Create Database and Tables

1. Open MySQL Workbench or command line
2. Connect to your MySQL server
3. Run the script: `Database Scripts/CreateUsersTable_MySQL.sql`

This will:
- Create the `LibraryManagementDB` database
- Create `Users` and `Members` tables
- Insert sample test accounts

### Step 4: Generate Password Hashes

The sample accounts in the SQL script use placeholder hashes. You need to generate proper SHA256 hashes.

**Using PowerShell:**
```powershell
$password = "admin123"
$bytes = [System.Text.Encoding]::UTF8.GetBytes($password)
$hash = [System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)
$hashString = [System.BitConverter]::ToString($hash).Replace('-', '').ToLower()
Write-Output $hashString
```

**Or use the PasswordHashGenerator helper** in your code:
```csharp
string hash = PasswordHashGenerator.GenerateHash("admin123");
```

### Step 5: Update Sample Accounts

After generating hashes, update the database:

```sql
USE LibraryManagementDB;

-- Update Administrator password hash
UPDATE Users 
SET PasswordHash = 'YOUR_GENERATED_HASH_HERE' 
WHERE Email = 'admin@library.com';

-- Update Staff password hash
UPDATE Users 
SET PasswordHash = 'YOUR_GENERATED_HASH_HERE' 
WHERE Email = 'staff@library.com';

-- Update Member password hash
UPDATE Users 
SET PasswordHash = 'YOUR_GENERATED_HASH_HERE' 
WHERE Email = 'member@library.com';
```

## 🧪 Test Accounts

After setup, you can test with:
- **Administrator**: `admin@library.com` / `admin123`
- **Staff**: `staff@library.com` / `staff123`
- **Member**: `member@library.com` / `member123`

*(Remember to update password hashes first!)*

## ✅ Files Updated for MySQL

1. ✅ `App.config` - MySQL connection string configured
2. ✅ `DatabaseHelper.cs` - Now uses `MySqlConnection`
3. ✅ `AuthenticationService.cs` - Now uses `MySqlCommand`
4. ✅ `Library Management System.csproj` - MySQL.Data reference added
5. ✅ `CreateUsersTable_MySQL.sql` - MySQL-compatible database script

## 🔍 Troubleshooting

### "Could not load file or assembly 'MySql.Data'"
- Install MySQL Connector/NET via NuGet
- Or manually add reference to MySql.Data.dll

### "Unable to connect to any of the specified MySQL hosts"
- Check MySQL server is running
- Verify connection string (host, port, credentials)
- Check firewall settings

### "Access denied for user 'root'@'localhost'"
- Verify password is correct (Admin123!)
- Check MySQL user permissions
- Try creating a new MySQL user with proper permissions

### "Unknown database 'LibraryManagementDB'"
- Run the `CreateUsersTable_MySQL.sql` script first
- Or manually create the database:
  ```sql
  CREATE DATABASE LibraryManagementDB;
  ```

## 📝 Next Steps

1. Build the project (should compile without errors)
2. Run the MySQL database script
3. Test the SignIn form
4. Create Dashboard form (next step)

## 🔒 Security Note

For production, consider:
- Using a dedicated MySQL user (not root)
- Storing connection string securely (encrypted)
- Using stronger password hashing (bcrypt/PBKDF2 instead of SHA256)
- Implementing connection pooling

