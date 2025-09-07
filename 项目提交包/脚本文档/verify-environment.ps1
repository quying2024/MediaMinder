Write-Host "=== MediaMinder 环境验证 ===" -ForegroundColor Green

# 检查.NET SDK
$dotnetVersion = dotnet --version
if ($dotnetVersion -match "8\.0\." -or $dotnetVersion -match "9\.0\.") {
    Write-Host "✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "❌ .NET SDK版本过低: $dotnetVersion (需要8.0+)" -ForegroundColor Red
}

# 检查PowerShell版本
$psVersion = $PSVersionTable.PSVersion
if ($psVersion.Major -ge 5) {
    Write-Host "✅ PowerShell: $psVersion" -ForegroundColor Green
} else {
    Write-Host "❌ PowerShell版本过低: $psVersion (需要5.1+)" -ForegroundColor Red
}

# 检查Git
try {
    $gitVersion = git --version
    Write-Host "✅ Git: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Git未安装" -ForegroundColor Red
}

# 检查管理员权限
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if ($isAdmin) {
    Write-Host "✅ 管理员权限: 已获取" -ForegroundColor Green
} else {
    Write-Host "❌ 管理员权限: 未获取" -ForegroundColor Red
}

# 检查项目文件
$projectFiles = @(
    "MediaMinder.sln",
    "MediaMinder.Common\MediaMinder.Common.csproj",
    "MediaMinder.Service\MediaMinder.Service.csproj",
    "MediaMinder.UI\MediaMinder.UI.csproj"
)

Write-Host "`n=== 项目文件检查 ===" -ForegroundColor Green
foreach ($file in $projectFiles) {
    if (Test-Path $file) {
        Write-Host "✅ $file" -ForegroundColor Green
    } else {
        Write-Host "❌ $file" -ForegroundColor Red
    }
}

Write-Host "`n=== 环境验证完成 ===" -ForegroundColor Green
