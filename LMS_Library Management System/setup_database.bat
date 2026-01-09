@echo off
REM ============================================
REM Library Management System - Database Setup
REM ============================================
echo.
echo ============================================
echo Library Management System - Database Setup
echo ============================================
echo.

REM Check if MySQL is installed
where mysql >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] MySQL is not installed or not in PATH
    echo Please install MySQL Server first
    echo Download from: https://dev.mysql.com/downloads/
    pause
    exit /b 1
)

echo [INFO] MySQL found!
echo.

REM Get MySQL root password
set /p MYSQL_PASSWORD="Enter MySQL root password: "

REM Check if database exists
echo.
echo [INFO] Checking if database exists...
mysql -u root -p%MYSQL_PASSWORD% -e "USE LMS_DB;" >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo [INFO] Database LMS_DB already exists
    set /p RECREATE="Do you want to recreate it? (y/n): "
    if /i "%RECREATE%"=="y" (
        echo [INFO] Dropping existing database...
        mysql -u root -p%MYSQL_PASSWORD% -e "DROP DATABASE IF EXISTS LMS_DB;"
    ) else (
        echo [INFO] Using existing database
        goto :update_config
    )
)

REM Create database
echo.
echo [INFO] Creating database and tables...
if exist "database_schema_complete.sql" (
    mysql -u root -p%MYSQL_PASSWORD% < "database_schema_complete.sql"
    if %ERRORLEVEL% EQU 0 (
        echo [SUCCESS] Database created successfully!
    ) else (
        echo [ERROR] Failed to create database
        pause
        exit /b 1
    )
) else (
    echo [WARNING] database_schema_complete.sql not found
    echo [INFO] Database will be created automatically when you run the application
)

:update_config
echo.
echo [INFO] Updating App.config...
echo.
echo Please update App.config manually with your MySQL password:
echo.
echo Find this line in App.config:
echo     connectionString="Server=localhost;Port=3306;Database=LMS_DB;Uid=root;Pwd=Admin123!;CharSet=utf8;"
echo.
echo Change Pwd=Admin123! to Pwd=%MYSQL_PASSWORD%
echo.

REM Option to open App.config
set /p OPEN_CONFIG="Do you want to open App.config now? (y/n): "
if /i "%OPEN_CONFIG%"=="y" (
    if exist "App.config" (
        notepad "App.config"
    ) else (
        echo [WARNING] App.config not found in current directory
        echo Please navigate to the project directory
    )
)

echo.
echo ============================================
echo Setup Complete!
echo ============================================
echo.
echo Default login credentials:
echo   Admin: admin@library.com / Admin123!
echo   Staff: staff@library.com / Staff123!
echo.
pause

