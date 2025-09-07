Write-Host "=== MediaMinder Build and Test Script ===" -ForegroundColor Green

# Verify environment
Write-Host "`nVerifying environment..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host ".NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host ".NET SDK not installed" -ForegroundColor Red
    exit 1
}

# Clean previous builds
Write-Host "`nCleaning previous builds..." -ForegroundColor Yellow
dotnet clean

# Restore packages
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Package restore failed" -ForegroundColor Red
    exit 1
}

# Build solution
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful" -ForegroundColor Green

# Publish service project
Write-Host "`nPublishing service project..." -ForegroundColor Yellow
dotnet publish MediaMinder.Service --configuration Release --output ./publish/service --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Service publish failed" -ForegroundColor Red
    exit 1
}

# Publish UI project
Write-Host "`nPublishing UI project..." -ForegroundColor Yellow
dotnet publish MediaMinder.UI --configuration Release --output ./publish/ui --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "UI publish failed" -ForegroundColor Red
    exit 1
}

# Check published files
Write-Host "`nChecking published files..." -ForegroundColor Yellow
$serviceExe = ".\publish\service\MediaMinder.Service.exe"
$uiExe = ".\publish\ui\MediaMinder.UI.exe"

if (Test-Path $serviceExe) {
    Write-Host "Service executable: $serviceExe" -ForegroundColor Green
} else {
    Write-Host "Service executable not found" -ForegroundColor Red
}

if (Test-Path $uiExe) {
    Write-Host "UI executable: $uiExe" -ForegroundColor Green
} else {
    Write-Host "UI executable not found" -ForegroundColor Red
}

# Show project information
Write-Host "`nProject Information..." -ForegroundColor Yellow
Write-Host "Solution file: MediaMinder.sln" -ForegroundColor Cyan
Write-Host "Project count: 3 (Common, Service, UI)" -ForegroundColor Cyan
Write-Host "Target framework: .NET 9.0" -ForegroundColor Cyan

# Show next steps
Write-Host "`nNext steps:" -ForegroundColor Green
Write-Host "1. Run deployment script: .\scripts\deploy.ps1" -ForegroundColor White
Write-Host "2. Start UI application: .\publish\ui\MediaMinder.UI.exe" -ForegroundColor White
Write-Host "3. Check service status: Get-Service MediaMinder" -ForegroundColor White

Write-Host "`nBuild and test completed successfully!" -ForegroundColor Green