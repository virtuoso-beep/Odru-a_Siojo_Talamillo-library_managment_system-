@echo off
REM Pre-build script to kill running instances
REM Always exits with code 0 to prevent build errors
taskkill /F /IM "LMS_Library Management System.exe" >nul 2>&1
exit /b 0

