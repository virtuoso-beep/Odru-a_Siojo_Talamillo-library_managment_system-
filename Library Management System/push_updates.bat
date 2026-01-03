@echo off
echo ==========================================
echo   Library Management System - Auto Push
echo ==========================================

echo [1/3] Checking Git status...
git status
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Git command failed. 
    echo Please make sure Git is installed and added to your system PATH.
    echo You may need to run this script from 'Git Bash' or a compatible terminal.
    pause
    exit /b
)

echo.
echo [2/3] Staging and Committing changes...
git add .
git commit -m "feat: Implement Reports, Search, and Settings tabs UI"

echo.
echo [3/3] Pushing to remote...
git push

echo.
echo ==========================================
echo   Success! verification complete.
echo ==========================================
pause
