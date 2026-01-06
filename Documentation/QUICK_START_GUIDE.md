# Library Management System - Quick Start Guide

## 🚀 Getting Started in 5 Minutes

### Step 1: Database Setup (5 minutes)

1. **Create Database**
   ```sql
   CREATE DATABASE LibraryManagementDB;
   ```

2. **Run Schema Scripts** (in order):
   - `Database/001_Create_Database_Schema.sql`
   - `Database/002_Fix_Schema_Issues.sql`
   - `Database/003_Add_Performance_Indexes.sql` (optional)

3. **Deploy Stored Procedures**:
   - `Database/StoredProcedures/001_Authentication_Procedures.sql`
   - `Database/StoredProcedures/002_Create_Staff_User.sql`
   - `Database/StoredProcedures/003_Members_Procedures.sql`
   - `Database/StoredProcedures/004_Books_Procedures.sql`
   - `Database/StoredProcedures/005_Circulation_Procedures.sql`
   - `Database/StoredProcedures/006_Fines_Procedures.sql`
   - `Database/StoredProcedures/007_Dashboard_Procedures.sql`
   - `Database/StoredProcedures/008_GetBookCategories.sql`
   - `Database/StoredProcedures/009_Password_Reset_Procedures.sql`
   - `Database/StoredProcedures/010_Reports_Procedures.sql`
   - `Database/StoredProcedures/011_Settings_Procedures.sql`

---

### Step 2: Configure Application (2 minutes)

1. **Edit `App.config`**:
   ```xml
   <connectionStrings>
     <add name="MySQLConnection"
          connectionString="Server=localhost;Database=LibraryManagementDB;Uid=your_username;Pwd=your_password;Port=3306;CharSet=utf8;"
          providerName="MySql.Data.MySqlClient"/>
   </connectionStrings>
   ```

2. **Build Project**:
   - Open solution in Visual Studio
   - Restore NuGet packages
   - Build solution (F6)

---

### Step 3: Create Admin Account (1 minute)

1. **Run Application**
2. **Default Admin** (if created by script):
   - Email: `admin@umindanao.edu.ph`
   - Password: (set during database setup)
   - Role: Administrator

3. **Or Create Admin**:
   - Use `002_Create_Staff_User.sql` to create admin user
   - Or register through application (if registration enabled)

---

### Step 4: First Login (1 minute)

1. **Launch Application**
2. **Enter Credentials**:
   - Email: `admin@umindanao.edu.ph`
   - Password: (your admin password)
   - Login As: **Administrator**
3. **Click "Sign In"**

---

### Step 5: Initial Setup (5 minutes)

1. **Add Library Information**:
   - Go to **Settings** → **General**
   - Enter library name, email, phone, address
   - Click **"Save"**

2. **Configure Email** (Optional):
   - Go to **Settings** → **Notifications**
   - Enter SMTP settings
   - Test email sending

3. **Add First Book**:
   - Go to **Catalog**
   - Click **"Add Book"**
   - Enter book details
   - Save

4. **Add First Member**:
   - Go to **Members**
   - Click **"Add New Member"**
   - Enter member details
   - Register

---

## ✅ Verification Checklist

- [ ] Database created and scripts executed
- [ ] Connection string configured
- [ ] Application builds successfully
- [ ] Can log in as administrator
- [ ] Can add a book
- [ ] Can add a member
- [ ] Can checkout a book
- [ ] Can return a book
- [ ] Reports generate correctly
- [ ] Settings save and load

---

## 🎯 Common First Tasks

### Add Books
1. **Catalog** → **Add Book**
2. Fill in details
3. Set total copies
4. Save

### Register Members
1. **Members** → **Add New Member**
2. Enter member information
3. Select member type
4. Register

### Checkout Books
1. **Circulation** → **Checkout**
2. Enter member ID
3. Enter book ID
4. Set loan period
5. Checkout

### Generate Reports
1. **Reports** → Select report type
2. View data and charts
3. Click **"Export"** to save

---

## 🔧 Troubleshooting

### Can't Connect to Database?
- Check MySQL server is running
- Verify connection string
- Check firewall settings
- Test connection with MySQL Workbench

### Build Errors?
- Restore NuGet packages
- Check .NET Framework version (4.7.2)
- Verify all files are included in project

### Login Not Working?
- Verify user exists in database
- Check password hash
- Verify role is correct
- Check IsActive = 1

---

## 📚 Next Steps

1. **Read Full Documentation**:
   - `USER_MANUAL.md` - For end users
   - `ADMIN_GUIDE.md` - For administrators
   - `API_DOCUMENTATION.md` - For developers

2. **Configure Settings**:
   - Set up borrowing policies
   - Configure fine rates
   - Set up email notifications

3. **Import Data** (Optional):
   - Import existing books
   - Import existing members
   - Import historical data

4. **Train Users**:
   - Provide user manual
   - Conduct training sessions
   - Set up support channels

---

## 💡 Tips

- **Start Small**: Add a few books and members first
- **Test Workflows**: Test checkout/return before going live
- **Backup Regularly**: Set up automated database backups
- **Monitor Logs**: Check error logs regularly
- **Update Settings**: Configure policies before heavy use

---

**Ready to Go!** 🎉

If you encounter any issues, refer to the full documentation or contact support.

---

**Last Updated**: Current Date  
**Version**: 1.0.0

