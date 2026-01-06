# Fix Build Errors - Quick Guide

## Errors Fixed

### 1. ✅ SettingsService Not Found
**Fixed:** Added `SettingsService.cs` to the project file

### 2. ✅ MySql.Data Reference Not Found  
**Fixed:** Updated `packages.config` with MySql.Data package reference

## What Was Done

1. **Updated `Library Management System.csproj`:**
   - Added `<Compile Include="Service\SettingsService.cs" />`
   - Added `<Compile Include="Helper\ErrorHandler.cs" />`
   - Added `<Compile Include="Helper\ReportExportHelper.cs" />`

2. **Updated `packages.config`:**
   - Added MySql.Data package reference (version 8.0.33)
   - Added System.Resources.Extensions package reference

## Next Steps to Resolve Build Errors

### Option 1: Restore NuGet Packages (Visual Studio)
1. Right-click on the solution in Solution Explorer
2. Select "Restore NuGet Packages"
3. Wait for packages to restore
4. Rebuild the solution

### Option 2: Restore NuGet Packages (Command Line)
```powershell
# Navigate to project directory
cd "Library Management System"

# Restore packages
nuget restore "Library Management System.sln"
```

### Option 3: Manual Package Installation
If NuGet restore doesn't work:

1. **In Visual Studio:**
   - Right-click on "References" in Solution Explorer
   - Select "Manage NuGet Packages"
   - Search for "MySql.Data"
   - Install version 8.0.33

2. **Or use Package Manager Console:**
   ```powershell
   Install-Package MySql.Data -Version 8.0.33
   ```

### Option 4: Clean and Rebuild
1. In Visual Studio: Build → Clean Solution
2. Close Visual Studio
3. Delete `bin` and `obj` folders
4. Reopen Visual Studio
5. Build → Rebuild Solution

## Verification

After restoring packages, verify:
- [ ] No red squiggles under `SettingsService` in DashboardForm.cs
- [ ] No red squiggles under `MySql.Data` references
- [ ] Solution builds without errors
- [ ] All references resolve correctly

## Files Modified

- ✅ `Library Management System.csproj` - Added missing file references
- ✅ `packages.config` - Added package references

## If Errors Persist

1. **Check .NET Framework Version:**
   - Project targets .NET Framework 4.7.2
   - MySql.Data 8.0.33 supports .NET Framework 4.6.2+

2. **Check NuGet Package Source:**
   - Tools → NuGet Package Manager → Package Manager Settings
   - Ensure nuget.org is enabled

3. **Check Assembly References:**
   - In Solution Explorer, expand References
   - Verify MySql.Data appears and has no warning icon

4. **Manual Reference (Last Resort):**
   - Download MySql.Data.dll manually
   - Add as file reference instead of NuGet package

---

**Status:** Project file updated. Restore NuGet packages to complete the fix.

