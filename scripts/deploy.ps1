Write-Host "开始部署MediaMinder..." -ForegroundColor Green

# 停止现有服务（增强错误处理）
try {
    Stop-Service -Name "MediaMinder" -ErrorAction SilentlyContinue
    Write-Host "服务已停止" -ForegroundColor Yellow
} catch {
    Write-Host "停止服务时出现异常，继续执行..." -ForegroundColor Yellow
}

# 删除服务（增强错误处理）
try {
    sc.exe delete "MediaMinder" 2>$null
    Write-Host "服务已删除" -ForegroundColor Yellow
} catch {
    Write-Host "删除服务时出现异常，继续执行..." -ForegroundColor Yellow
}

# 发布服务
Write-Host "正在发布服务..." -ForegroundColor Yellow
dotnet publish MediaMinder.Service -c Release -o ./publish

# 创建必要的目录
$programDataPath = "C:\ProgramData\MediaMinder"
$logsPath = Join-Path $programDataPath "Logs"
$photosPath = Join-Path $programDataPath "G16"

if (!(Test-Path $programDataPath)) {
    New-Item -ItemType Directory -Path $programDataPath -Force
    Write-Host "创建程序数据目录: $programDataPath" -ForegroundColor Yellow
}

if (!(Test-Path $logsPath)) {
    New-Item -ItemType Directory -Path $logsPath -Force
    Write-Host "创建日志目录: $logsPath" -ForegroundColor Yellow
}

if (!(Test-Path $photosPath)) {
    New-Item -ItemType Directory -Path $photosPath -Force
    Write-Host "创建照片目录: $photosPath" -ForegroundColor Yellow
}

# 安装服务（添加显示名称）
$servicePath = Join-Path $PWD "publish\MediaMinder.Service.exe"
Write-Host "正在安装服务..." -ForegroundColor Yellow
sc.exe create "MediaMinder" binPath="$servicePath" start=auto DisplayName="MediaMinder Photo Service"

# 启动服务（添加错误处理）
Write-Host "正在启动服务..." -ForegroundColor Yellow
try {
    Start-Service -Name "MediaMinder"
    Write-Host "服务启动成功" -ForegroundColor Green
} catch {
    Write-Host "服务启动失败: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "请检查服务日志和配置" -ForegroundColor Yellow
    exit 1
}

Write-Host "部署完成！" -ForegroundColor Green
Write-Host "服务名称: MediaMinder" -ForegroundColor Cyan
Write-Host "显示名称: MediaMinder Photo Service" -ForegroundColor Cyan
Write-Host "日志目录: $logsPath" -ForegroundColor Cyan
Write-Host "照片目录: $photosPath" -ForegroundColor Cyan
