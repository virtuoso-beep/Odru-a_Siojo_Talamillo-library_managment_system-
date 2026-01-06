# How to Restore NuGet Packages

## Method 1: Visual Studio Package Manager Console (Recommended)

### Steps:
1. **Open Visual Studio** with your solution loaded
2. **Open Package Manager Console:**
   - Go to: **Tools** → **NuGet Package Manager** → **Package Manager Console**
   - Or use shortcut: **View** → **Other Windows** → **Package Manager Console**
   - The console will appear at the bottom of Visual Studio

3. **In the Package Manager Console**, type:
   ```powershell
   Update-Package -reinstall
   ```
   Press Enter

4. **Wait for completion** - You'll see messages like:
   ```
   Restoring NuGet packages...
   Successfully restored 'MySql.Data 8.0.33'
   ```

5. **Rebuild the solution:**
   - Build → Rebuild Solution
   - Or press: **Ctrl+Shift+B**

---

## Method 2: Visual Studio Solution Context Menu (Easier)

### Steps:
1. **Right-click on the solution** in Solution Explorer (the top-level item)
2. Select **"Restore NuGet Packages"**
3. Wait for the restore to complete
4. **Rebuild the solution**

---

## Method 3: Command Line (If NuGet CLI is installed)

### Steps:
1. **Open Command Prompt or PowerShell**
2. **Navigate to your solution directory:**
   ```powershell
   cd "C:\Users\Twinkle Pril\source\repos\library_managment_system"
   ```

3. **Run NuGet restore:**
   ```powershell
   nuget restore "Library Management System.sln"
   ```

   **Note:** If `nuget` command is not found, you need to:
   - Download NuGet.exe from https://www.nuget.org/downloads
   - Add it to your PATH, or
   - Use the full path to nuget.exe

---

## Method 4: Visual Studio - Manage NuGet Packages UI

### Steps:
1. **Right-click on the project** (not solution) in Solution Explorer
2. Select **"Manage NuGet Packages..."**
3. Go to the **"Installed"** tab
4. Find **MySql.Data**
5. Click **"Uninstall"** (if needed)
6. Go to **"Browse"** tab
7. Search for **"MySql.Data"**
8. Select version **8.0.33**
9. Click **"Install"**

---

## Verification

After restoring packages, verify:

1. **Check References:**
   - In Solution Explorer, expand your project
   - Expand **"References"**
   - Look for **MySql.Data**
   - It should have **no yellow warning icon**

2. **Check packages folder:**
   - Navigate to: `%USERPROFILE%\.nuget\packages\mysql.data\8.0.33\`
   - Or: `C:\Users\Twinkle Pril\.nuget\packages\mysql.data\8.0.33\`
   - Verify the folder exists and contains `lib\net462\MySql.Data.dll`

3. **Build the solution:**
   - Build → Rebuild Solution
   - Should complete without errors

---

## Troubleshooting

### If Package Manager Console is not available:
- Make sure you're using Visual Studio (not Visual Studio Code)
- Install NuGet Package Manager extension if missing

### If restore fails:
- Check your internet connection
- Verify NuGet package source is enabled:
  - Tools → NuGet Package Manager → Package Manager Settings
  - Go to "Package Sources"
  - Ensure "nuget.org" is checked and enabled

### If MySql.Data still not found after restore:
1. Close Visual Studio
2. Delete `bin` and `obj` folders in your project
3. Delete `packages` folder (if exists in solution directory)
4. Reopen Visual Studio
5. Restore packages again
6. Rebuild

---

## Quick Reference

**Package Manager Console Location:**
- Tools → NuGet Package Manager → Package Manager Console

**Command to run:**
```powershell
Update-Package -reinstall
```

**Or simply:**
- Right-click Solution → Restore NuGet Packages

---

**Status:** Choose Method 1 or Method 2 - both are the easiest options!

