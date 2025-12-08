# PowerShell script to install MySQL NuGet package
# Run this script from the project root directory

Write-Host "Installing MySQL.Data NuGet Package..." -ForegroundColor Green

$projectPath = "Library Management System"
$packageName = "MySql.Data"
$packageVersion = "8.0.33"

# Check if NuGet is available
if (Get-Command nuget -ErrorAction SilentlyContinue) {
    Write-Host "Using NuGet CLI..." -ForegroundColor Yellow
    nuget install $packageName -Version $packageVersion -OutputDirectory packages
    Write-Host "Package installed successfully!" -ForegroundColor Green
} 
elseif (Get-Command dotnet -ErrorAction SilentlyContinue) {
    Write-Host "Using .NET CLI..." -ForegroundColor Yellow
    dotnet add "$projectPath\Library Management System.csproj" package $packageName --version $packageVersion
    Write-Host "Package installed successfully!" -ForegroundColor Green
}
else {
    Write-Host "`nNuGet CLI or .NET CLI not found." -ForegroundColor Red
    Write-Host "Please install the package manually:" -ForegroundColor Yellow
    Write-Host "1. Open Visual Studio" -ForegroundColor Cyan
    Write-Host "2. Right-click project -> Manage NuGet Packages" -ForegroundColor Cyan
    Write-Host "3. Search for 'MySql.Data'" -ForegroundColor Cyan
    Write-Host "4. Install version 8.0.33" -ForegroundColor Cyan
}

Write-Host "`nPress any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

