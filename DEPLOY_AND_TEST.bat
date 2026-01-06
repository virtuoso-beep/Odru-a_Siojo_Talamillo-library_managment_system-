@echo off
echo ============================================
echo Library Management System - Deployment Script
echo ============================================
echo.

echo Step 1: Building project...
call msbuild "Library Management System\Library Management System.csproj" /t:Build /p:Configuration=Debug /v:minimal
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ Build failed! Please check errors above.
    pause
    exit /b 1
)

echo.
echo ✅ Build successful!
echo.
echo ============================================
echo Next Steps:
echo ============================================
echo.
echo 1. Deploy Database Scripts:
echo    - Open MySQL Workbench
echo    - Run: Database\StoredProcedures\009_Password_Reset_Procedures.sql
echo    - Run: Database\StoredProcedures\010_Reports_Procedures.sql
echo    - Run: Database\StoredProcedures\011_Settings_Procedures.sql
echo.
echo 2. Run Application:
echo    - Launch: Library Management System.exe
echo    - Login as Administrator
echo.
echo 3. Run Tests:
echo    - Open TestForm from application
echo    - Click "Run All Automated Tests"
echo.
echo ============================================
pause

