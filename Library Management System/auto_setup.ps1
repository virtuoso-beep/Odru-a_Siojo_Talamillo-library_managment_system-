# Auto Setup Script for Library Management System
# This script attempts to automate the setup process

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Library Management System - Auto Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$projectPath = "Library Management System"
$packageName = "MySql.Data"
$packageVersion = "8.0.33"
$packagePath = "packages\MySql.Data.$packageVersion"
$dllPath = "$packagePath\lib\net452\MySql.Data.dll"

# Step 1: Install MySQL NuGet Package
Write-Host "[1/3] Installing MySQL NuGet Package..." -ForegroundColor Yellow

if (Test-Path $dllPath) {
    Write-Host "✓ MySQL package already exists!" -ForegroundColor Green
} 
else {
    Write-Host "Attempting to download MySQL package..." -ForegroundColor Yellow
    
    # Try NuGet CLI
    if (Get-Command nuget -ErrorAction SilentlyContinue) {
        Write-Host "Using NuGet CLI..." -ForegroundColor Cyan
        nuget install $packageName -Version $packageVersion -OutputDirectory packages -NonInteractive
        if (Test-Path $dllPath) {
            Write-Host "✓ Package installed via NuGet CLI!" -ForegroundColor Green
        }
    }
    
    # Try .NET CLI
    if (-not (Test-Path $dllPath) -and (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Host "Trying .NET CLI..." -ForegroundColor Cyan
        dotnet add "$projectPath\Library Management System.csproj" package $packageName --version $packageVersion
        if (Test-Path $dllPath) {
            Write-Host "✓ Package installed via .NET CLI!" -ForegroundColor Green
        }
    }
    
    # Try direct download
    if (-not (Test-Path $dllPath)) {
        Write-Host "Attempting direct download..." -ForegroundColor Cyan
        try {
            $nugetUrl = "https://www.nuget.org/api/v2/package/MySql.Data/$packageVersion"
            New-Item -ItemType Directory -Force -Path $packagePath | Out-Null
            $zipPath = "$packagePath\package.zip"
            
            Write-Host "Downloading from NuGet..." -ForegroundColor Gray
            Invoke-WebRequest -Uri $nugetUrl -OutFile $zipPath -UseBasicParsing -ErrorAction Stop
            
            if (Test-Path $zipPath) {
                Write-Host "Extracting package..." -ForegroundColor Gray
                Expand-Archive -Path $zipPath -DestinationPath $packagePath -Force
                Remove-Item $zipPath -Force
                
                if (Test-Path $dllPath) {
                    Write-Host "✓ Package downloaded and extracted!" -ForegroundColor Green
                }
            }
        }
        catch {
            Write-Host "✗ Direct download failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    if (-not (Test-Path $dllPath)) {
        Write-Host "✗ Could not install package automatically." -ForegroundColor Red
        Write-Host "  Please install manually in Visual Studio:" -ForegroundColor Yellow
        Write-Host "  1. Right-click project -> Manage NuGet Packages" -ForegroundColor Cyan
        Write-Host "  2. Search for 'MySql.Data'" -ForegroundColor Cyan
        Write-Host "  3. Install version $packageVersion" -ForegroundColor Cyan
    }
}

# Step 2: Verify Project Configuration
Write-Host ""
Write-Host "[2/3] Verifying project configuration..." -ForegroundColor Yellow

$csprojPath = "$projectPath\Library Management System.csproj"
if (Test-Path $csprojPath) {
    $csprojContent = Get-Content $csprojPath -Raw
    if ($csprojContent -match "MySql.Data") {
        Write-Host "✓ Project file references MySQL package" -ForegroundColor Green
    } else {
        Write-Host "⚠ Project file may need MySQL reference update" -ForegroundColor Yellow
    }
    
    if ($csprojContent -match "packages.config") {
        Write-Host "✓ packages.config referenced" -ForegroundColor Green
    }
} else {
    Write-Host "✗ Project file not found" -ForegroundColor Red
}

# Step 3: Database Setup Instructions
Write-Host ""
Write-Host "[3/3] Database setup instructions..." -ForegroundColor Yellow
Write-Host ""
Write-Host "To complete setup, you need to:" -ForegroundColor Cyan
Write-Host "1. Open MySQL Workbench" -ForegroundColor White
Write-Host "2. Connect with:" -ForegroundColor White
Write-Host "   - Username: root" -ForegroundColor Gray
Write-Host "   - Password: Admin123!" -ForegroundColor Gray
Write-Host "3. Run the script: Database Scripts\CreateUsersTable_MySQL.sql" -ForegroundColor White
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if (Test-Path $dllPath) {
    Write-Host "✓ MySQL Package: INSTALLED" -ForegroundColor Green
} else {
    Write-Host "✗ MySQL Package: NEEDS INSTALLATION" -ForegroundColor Red
}

Write-Host "⚠ Database: NEEDS MANUAL SETUP" -ForegroundColor Yellow
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. If package not installed, use Visual Studio NuGet Manager" -ForegroundColor White
Write-Host "2. Run the MySQL database script" -ForegroundColor White
Write-Host "3. Build and test the application" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

