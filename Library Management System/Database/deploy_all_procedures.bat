@echo off
echo.
echo ============================================
echo Deploying All Stored Procedures
echo ============================================
echo.
cd StoredProcedures
echo [1/8] Deploying Authentication Procedures...
mysql -u root -p LibraryManagementDB < 001_Authentication_Procedures.sql
if %errorlevel% neq 0 (
    echo ERROR: Failed to deploy Authentication Procedures
    pause
    exit /b 1
)
echo [2/8] Creating Staff User...
mysql -u root -p LibraryManagementDB < 002_Create_Staff_User.sql
if %errorlevel% neq 0 (
    echo ERROR: Failed to create staff user
    pause
    exit /b 1
)
echo [3/8] Deploying Members Procedures...
mysql -u root -p LibraryManagementDB < 003_Members_Procedures.sql
if %errorlevel% neq 0 (
    echo ERROR: Failed to deploy Members Procedures
    pause
    exit /b 1
)
echo [4/8] Deploying Books Procedures...
mysql -u root -p LibraryManagementDB < 004_Books_Procedures.sql
if %errorlevel% neq 0 (
    echo ERROR: Failed to deploy Books Procedures
    pause
    exit /b 1
)
echo [5/8] Deploying Circulation Procedures...
mysql -u root -p LibraryManagementDB < 005_Circulation_Procedures.sql
if %errorlevel% neq 0 (
    echo ERROR: Failed to deploy Circulation Procedures
    pause
    exit /b 1
)
echo [6/8] Deploying Fines Procedures...
mysql -u root -p LibraryManagementDB < 006_Fines_Procedures.sql
if %errorlevel% neq 0 (
    echo ERROR: Failed to deploy Fines Procedures
    pause
    exit /b 1
)
echo [7/8] Deploying Dashboard Procedures...
mysql -u root -p LibraryManagementDB < 007_Dashboard_Procedures.sql
if %errorlevel% neq 0 (
    echo ERROR: Failed to deploy Dashboard Procedures
    pause
    exit /b 1
)
echo [8/8] Deploying Category Procedure...
mysql -u root -p LibraryManagementDB < 008_GetBookCategories.sql
if %errorlevel% neq 0 (
    echo ERROR: Failed to deploy Category Procedure
    pause
    exit /b 1
)
cd ..
echo.
echo ============================================
echo All stored procedures deployed successfully!
echo ============================================
echo.
echo Next steps:
echo 1. Verify procedures using the verification queries in README.md
echo 2. Update App.config with your database credentials
echo 3. Run the application and test login
echo.
pause
