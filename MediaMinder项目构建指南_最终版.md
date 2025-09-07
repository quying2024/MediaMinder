# 🚀 MediaMinder项目构建指南 (最终版)

## 📋 项目整体目标

**项目名称**: `MediaMinder`  
**核心功能**: 自动检测USB相机插入，下载图片到本地，并提供照片展示界面  
**架构模式**: 客户端-服务端分离架构 (C/S Architecture)  
**设计原则**: 高可维护性、高扩展性、高可靠性、生产级健壮性

## 🛠️ 开发环境要求

### 必需软件环境
- **.NET SDK**: .NET 8.0 或更高版本
- **开发IDE**: Visual Studio 2022 (推荐) 或 Visual Studio Code
- **操作系统**: Windows 10/11 或 Windows Server 2019/2022
- **PowerShell**: 5.1 或更高版本 (用于部署脚本)
- **Git**: 2.30+ (用于版本控制)

### 推荐开发工具
- **NuGet Package Manager**: 最新版本
- **Windows SDK**: 10.0.19041.0 或更高版本
- **.NET Framework**: 4.8 (用于某些Windows API兼容性)

### 系统权限要求
- **管理员权限**: 用于安装和配置Windows服务
- **开发权限**: 用于创建和修改系统目录
- **网络权限**: 用于下载NuGet包和依赖项

### 目标部署环境
- **.NET Runtime**: .NET 8.0 Runtime
- **Windows版本**: Windows 10 版本 1903 或更高版本
- **架构支持**: x64 (推荐) 或 x86
- **内存要求**: 最少 4GB RAM
- **磁盘空间**: 最少 1GB 可用空间

## 🏗️ 整体架构设计

```
MediaMinder/
├── MediaMinder.Common/     # 共享库 (.NET Standard 2.0+)
├── MediaMinder.Service/    # 后台服务 (Worker Service)
├── MediaMinder.UI/         # 前台UI应用 (WinForms/WPF)
├── docs/                   # 项目文档
├── scripts/                # 部署和维护脚本
└── MediaMinder.sln         # 解决方案文件
```

## 📦 各模块详细说明

### 1. MediaMinder.Common (共享库)

**目标**: 提供服务和UI之间的共享数据模型、配置类、通信协议和消息类型定义

**核心文件**:
- `ServiceSettings.cs` - 强类型配置模型
- `PhotoInfo.cs` - 照片数据模型  
- `CommunicationProtocol.cs` - IPC通信协议抽象接口
- `MessageTypes.cs` - 消息类型和枚举定义
- `IPCMessage.cs` - 统一IPC消息模型
- `ICommunicationService.cs` - 通信服务抽象接口

**需要解决的具体问题**:
1. **命名空间设计**: 使用 `MediaMinder.Common` 避免冲突
2. **序列化兼容性**: 确保JSON序列化在不同.NET版本间兼容
3. **配置验证**: 添加配置验证逻辑，确保必要字段不为空
4. **版本兼容性**: 使用 .NET Standard 2.0+ 确保跨平台兼容
5. **消息类型扩展**: 设计可扩展的消息类型系统
6. **通信抽象**: 使用接口抽象通信行为，便于未来切换通信方式

**创建步骤**:
```bash
# 1. 创建类库项目
dotnet new classlib -n MediaMinder.Common

# 2. 设置目标框架为.NET Standard 2.0
# 编辑 MediaMinder.Common.csproj 文件，确保包含：
# <TargetFramework>netstandard2.0</TargetFramework>

# 3. 添加必要的NuGet包
dotnet add package System.Text.Json
dotnet add package Microsoft.Extensions.Configuration.Abstractions
dotnet add package Microsoft.Extensions.Options
```

**关键代码示例**:
```csharp
// MessageTypes.cs
public enum MessageType
{
    NewPhotosAvailable,
    StatusUpdate,
    CameraEvent,
    ServiceStarted,
    ServiceStopped,
    PrintRequest  // 为未来打印功能预留
}

// IPCMessage.cs
public class IPCMessage
{
    public MessageType Type { get; set; }
    public string Data { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

// ICommunicationService.cs - 通信服务抽象接口（明确定义方法签名）
public interface ICommunicationService
{
    Task SendMessageAsync(IPCMessage message);
    void StartListening(Action<IPCMessage> messageHandler);
    void StopListening();
    bool IsConnected { get; }
    event EventHandler<IPCMessage> MessageReceived;
    event EventHandler<bool> ConnectionStatusChanged;
}

// CommunicationProtocol.cs - 抽象通信协议
public abstract class CommunicationProtocol
{
    public abstract Task WriteMessageAsync(Stream stream, IPCMessage message);
    public abstract Task<IPCMessage> ReadMessageAsync(Stream stream);
    
    protected static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}

// NamedPipeCommunicationService.cs - 具体实现示例
public class NamedPipeCommunicationService : ICommunicationService
{
    private NamedPipeServerStream? _pipeServer;
    private CancellationTokenSource? _cancellationTokenSource;
    private Action<IPCMessage>? _messageHandler;
    
    public bool IsConnected => _pipeServer?.IsConnected ?? false;
    public event EventHandler<IPCMessage>? MessageReceived;
    public event EventHandler<bool>? ConnectionStatusChanged;
    
    public async Task SendMessageAsync(IPCMessage message)
    {
        if (!IsConnected) return;
        
        try
        {
            await CommunicationProtocol.WriteMessageAsync(_pipeServer!, message);
        }
        catch (Exception ex)
        {
            // 处理发送异常
            throw;
        }
    }
    
    public void StartListening(Action<IPCMessage> messageHandler)
    {
        _messageHandler = messageHandler;
        _cancellationTokenSource = new CancellationTokenSource();
        
        // 启动监听逻辑
        _ = Task.Run(async () => await ListenForMessagesAsync(_cancellationTokenSource.Token));
    }
    
    public void StopListening()
    {
        _cancellationTokenSource?.Cancel();
        _pipeServer?.Dispose();
    }
    
    private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
    {
        // 实现具体的监听逻辑
    }
}
```

### 2. MediaMinder.Service (后台服务)

**目标**: 作为Windows服务运行，处理相机检测、文件下载、文件监控等后台任务

**核心组件**:
- `PlatformService` - 主服务协调器 (IHostedService)
- `CameraService` - 相机检测和图片下载
- `PhotoDisplayService` - 照片目录监控
- `CommunicationService` - IPC服务端实现
- `LoggingService` - 统一日志管理

**需要解决的具体问题**:
1. **WMI权限问题**: 确保服务有足够权限访问WMI事件
2. **文件路径问题**: 使用 `C:\ProgramData` 而非用户目录
3. **服务启动超时**: 实现快速启动，避免1053错误
4. **异常处理**: 完善的异常处理和日志记录
5. **资源释放**: 确保WMI监听器和文件监控器正确释放
6. **线程管理**: 避免阻塞操作，确保服务响应停止信号
7. **健壮性**: 处理文件权限、网络异常等各种边界情况
8. **配置绑定**: 使用强类型配置注入，避免配置错误
9. **日志记录**: 同时写入事件日志和文件日志
10. **权限管理**: 正确处理LocalSystem账户权限限制

**创建步骤**:
```bash
# 1. 创建Worker Service项目
dotnet new worker -n MediaMinder.Service

# 2. 设置目标框架为.NET 8.0
# 编辑 MediaMinder.Service.csproj 文件，确保包含：
# <TargetFramework>net8.0</TargetFramework>

# 3. 添加必要的NuGet包
dotnet add package Microsoft.Extensions.Hosting.WindowsServices
dotnet add package System.Management
dotnet add package System.IO.Pipes
dotnet add package Serilog.Extensions.Hosting
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.EventLog

# 4. 添加Common项目引用
dotnet add reference ../MediaMinder.Common/MediaMinder.Common.csproj
```

**关键配置**:
```json
{
  "ServiceSettings": {
    "CameraService": {
      "Enabled": true,
      "CanonDrivePrefix": "Canon G16",
      "DcimPath": "DCIM",
      "TargetDirectory": "C:\\ProgramData\\MediaMinder\\G16",
      "CooldownSeconds": 30,
      "MaxRetryAttempts": 5,
      "RetryDelayMs": 1000
    },
    "PhotoDisplayService": {
      "Enabled": true,
      "PhotosDirectory": "C:\\ProgramData\\MediaMinder\\G16",
      "MaxThumbnailSize": 200,
      "RefreshDelayMs": 1000,
      "MaxDisplayPhotos": 6,
      "SupportedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".raw", ".cr2"]
    },
    "Communication": {
      "PipeName": "MediaMinder_Pipe",
      "ConnectionTimeoutMs": 5000,
      "MessageTimeoutMs": 10000
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.File", "Serilog.Sinks.EventLog"],
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "C:\\ProgramData\\MediaMinder\\Logs\\service-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "EventLog",
        "Args": {
          "source": "MediaMinder",
          "logName": "Application"
        }
      }
    ]
  }
}
```

**配置绑定优化实现**:
```csharp
// Program.cs - 配置绑定和日志配置
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseWindowsService(options => { 
            options.ServiceName = "MediaMinder"; 
        })
        .UseSerilog((hostContext, services, configuration) =>
        {
            configuration.ReadFrom.Configuration(hostContext.Configuration);
        })
        .ConfigureServices((hostContext, services) =>
        {
            // 强类型配置绑定
            services.Configure<ServiceSettings>(hostContext.Configuration.GetSection("ServiceSettings"));
            
            // 注册服务
            services.AddHostedService<PlatformService>();
            services.AddSingleton<CameraService>();
            services.AddSingleton<PhotoDisplayService>();
            services.AddSingleton<ICommunicationService, NamedPipeCommunicationService>();
        });

// CameraService.cs - 使用强类型配置
public class CameraService
{
    private readonly ILogger<CameraService> _logger;
    private readonly ServiceSettings _settings;
    private readonly ICommunicationService _communicationService;

    public CameraService(
        ILogger<CameraService> logger,
        IOptions<ServiceSettings> settings,
        ICommunicationService communicationService)
    {
        _logger = logger;
        _settings = settings.Value;  // 强类型配置访问
        _communicationService = communicationService;
    }
    
    // 使用配置
    private string GetTargetDirectory() => _settings.CameraService.TargetDirectory;
}
```

**健壮性实现要点**:
```csharp
// 文件操作异常处理示例
try
{
    if (!File.Exists(sourceFile))
    {
        _logger.LogWarning("源文件不存在: {SourceFile}", sourceFile);
        return false;
    }
    
    File.Copy(sourceFile, targetPath, true);
    _logger.LogInformation("文件复制成功: {SourceFile} -> {TargetPath}", sourceFile, targetPath);
    return true;
}
catch (UnauthorizedAccessException ex)
{
    _logger.LogError(ex, "文件访问权限不足: {SourceFile}", sourceFile);
    return false;
}
catch (IOException ex)
{
    _logger.LogError(ex, "文件IO异常: {SourceFile}", sourceFile);
    return false;
}
catch (Exception ex)
{
    _logger.LogError(ex, "文件操作未知异常: {SourceFile}", sourceFile);
    return false;
}
```

### 3. MediaMinder.UI (前台UI应用)

**目标**: 作为普通Windows应用程序运行，提供照片展示界面和用户交互

**核心组件**:
- `PhotoDisplayForm` - 照片展示窗口
- `CommunicationClient` - IPC客户端实现
- `PhotoManager` - 照片管理逻辑
- `BackupManager` - 备份和清理逻辑
- `ProcessManager` - 进程唯一性管理
- `AutoStartManager` - 自启动管理

**需要解决的具体问题**:
1. **UI线程管理**: 确保UI操作在正确的线程上执行
2. **图片加载优化**: 异步加载，避免UI阻塞
3. **内存管理**: 及时释放图片资源，防止内存泄漏
4. **文件锁定**: 处理图片被其他程序占用的情况
5. **用户体验**: 提供加载状态、错误提示等
6. **进程唯一性**: 确保只有一个UI实例运行
7. **IPC客户端稳定性**: 处理连接断开和重连
8. **资源释放**: 确保IPC客户端和图片资源正确释放
9. **自启动管理**: 使用任务计划程序实现自启动

**创建步骤**:
```bash
# 1. 创建Windows Forms应用
dotnet new winforms -n MediaMinder.UI

# 2. 设置目标框架为.NET 8.0
# 编辑 MediaMinder.UI.csproj 文件，确保包含：
# <TargetFramework>net8.0-windows</TargetFramework>

# 3. 添加必要的NuGet包
dotnet add package System.IO.Pipes
dotnet add package System.Drawing.Common
dotnet add package System.Threading
dotnet add package TaskScheduler

# 4. 添加Common项目引用
dotnet add reference ../MediaMinder.Common/MediaMinder.Common.csproj
```

**进程唯一性实现**:
```csharp
// ProcessManager.cs
public class ProcessManager
{
    private static Mutex _mutex;
    
    public static bool IsSingleInstance()
    {
        _mutex = new Mutex(true, "MediaMinder_UI_SingleInstance", out bool createdNew);
        return createdNew;
    }
    
    public static void ReleaseMutex()
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }
}
```

**自启动管理实现**:
```csharp
// AutoStartManager.cs - 使用任务计划程序管理自启动
public class AutoStartManager
{
    private const string TaskName = "MediaMinder_UI_AutoStart";
    
    public static bool IsAutoStartEnabled()
    {
        try
        {
            var task = TaskService.Instance.GetTask(TaskName);
            return task != null && task.Enabled;
        }
        catch
        {
            return false;
        }
    }
    
    public static void EnableAutoStart(string executablePath)
    {
        try
        {
            using (var taskService = new TaskService())
            {
                var taskDefinition = taskService.NewTask();
                taskDefinition.RegistrationInfo.Description = "MediaMinder UI Auto Start";
                taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
                
                var trigger = new LogonTrigger();
                taskDefinition.Triggers.Add(trigger);
                
                var action = new ExecAction(executablePath);
                taskDefinition.Actions.Add(action);
                
                taskService.RootFolder.RegisterTaskDefinition(TaskName, taskDefinition);
            }
        }
        catch (Exception ex)
        {
            // 处理任务创建异常
            throw new InvalidOperationException("无法创建自启动任务", ex);
        }
    }
    
    public static void DisableAutoStart()
    {
        try
        {
            using (var taskService = new TaskService())
            {
                taskService.RootFolder.DeleteTask(TaskName);
            }
        }
        catch
        {
            // 忽略删除异常
        }
    }
}
```

**UI线程安全实现**:
```csharp
// 在后台线程收到服务消息时
private void OnServiceMessageReceived(IPCMessage message)
{
    if (this.InvokeRequired)
    {
        this.BeginInvoke(new Action<IPCMessage>(OnServiceMessageReceived), message);
        return;
    }
    
    // 在UI线程上执行UI更新
    UpdatePhotoDisplay(message);
}
```

**异步图片加载优化**:
```csharp
// PhotoDisplayForm.cs - 异步图片加载
private async Task LoadImageAsync(string imagePath, PictureBox pictureBox)
{
    try
    {
        // 在后台线程加载图片
        var image = await Task.Run(() =>
        {
            using (var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                return Image.FromStream(fs);
            }
        });

        // 在UI线程更新显示
        if (this.InvokeRequired)
        {
            this.BeginInvoke(new Action(() =>
            {
                pictureBox.Image?.Dispose(); // 释放旧图片
                pictureBox.Image = image;
            }));
        }
        else
        {
            pictureBox.Image?.Dispose();
            pictureBox.Image = image;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "加载图片失败: {ImagePath}", imagePath);
    }
}
```

**资源释放实现**:
```csharp
// PhotoDisplayForm.cs - 确保资源正确释放
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        // 释放IPC客户端
        _communicationClient?.Dispose();
        
        // 释放所有图片资源
        foreach (Control control in _photoPanel.Controls)
        {
            if (control is PictureBox pictureBox)
            {
                pictureBox.Image?.Dispose();
            }
        }
        
        // 释放其他资源
        _refreshCancellationTokenSource?.Dispose();
    }
    
    base.Dispose(disposing);
}
```

## 🔧 创建过程中的关键问题解决

### 问题1: 项目引用和命名空间冲突
**解决方案**:
- 使用明确的命名空间前缀
- 在项目文件中正确配置引用路径
- 避免循环引用
- 使用 .NET Standard 2.0+ 确保兼容性

### 问题2: 配置文件管理
**解决方案**:
- 使用强类型配置类
- 实现配置验证
- 支持环境特定配置
- 使用 IOptions<T> 进行配置绑定

### 问题3: IPC通信稳定性
**解决方案**:
- 使用命名管道进行可靠通信
- 实现连接重试机制
- 添加心跳检测
- 处理连接断开和重连
- 使用抽象接口便于未来扩展

### 问题4: 文件权限和路径
**解决方案**:
- 使用 `C:\ProgramData\MediaMinder` 作为共享目录
- 确保服务有足够权限
- 实现目录自动创建
- 处理权限异常

### 问题5: 服务部署和调试
**解决方案**:
- 支持调试模式和服务模式切换
- 提供详细的日志记录
- 实现优雅的服务停止
- 编写自动化部署脚本

### 问题6: 线程管理和UI响应性
**解决方案**:
- 避免在服务主线程进行阻塞操作
- 使用异步/等待模式
- 确保UI更新在正确线程执行
- 实现进程唯一性检查

### 问题7: 资源管理和内存泄漏
**解决方案**:
- 实现IDisposable接口
- 在Dispose方法中释放所有资源
- 使用using语句管理资源生命周期
- 定期进行垃圾回收

### 问题8: 日志记录和故障排查
**解决方案**:
- 同时使用事件日志和文件日志
- 配置日志轮转和保留策略
- 提供详细的错误信息和堆栈跟踪
- 支持远程日志查看

### 问题9: 自启动和任务管理
**解决方案**:
- 使用任务计划程序而非注册表
- 提供自启动管理接口
- 支持多种触发条件
- 处理权限和异常情况

## 📝 详细创建步骤

### 第零步: 环境验证
```bash
# 验证.NET SDK版本
dotnet --version
# 应该显示 8.0.x 或更高版本

# 验证PowerShell版本
$PSVersionTable.PSVersion
# 应该显示 5.1.x 或更高版本

# 验证Git安装
git --version
# 应该显示 2.30.x 或更高版本

# 验证管理员权限
# 在PowerShell中运行以下命令，应该返回True
([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
```

### 第一步: 创建解决方案结构
```bash
# 创建解决方案
dotnet new sln -n MediaMinder

# 创建各项目
dotnet new classlib -n MediaMinder.Common
dotnet new worker -n MediaMinder.Service  
dotnet new winforms -n MediaMinder.UI

# 创建文档和脚本目录
mkdir docs
mkdir scripts

# 添加到解决方案
dotnet sln add MediaMinder.Common/MediaMinder.Common.csproj
dotnet sln add MediaMinder.Service/MediaMinder.Service.csproj
dotnet sln add MediaMinder.UI/MediaMinder.UI.csproj
```

### 第二步: 配置项目依赖
```bash
# Service项目添加引用
cd MediaMinder.Service
dotnet add reference ../MediaMinder.Common/MediaMinder.Common.csproj

# UI项目添加引用  
cd ../MediaMinder.UI
dotnet add reference ../MediaMinder.Common/MediaMinder.Common.csproj
```

### 第三步: 添加必要的NuGet包
```bash
# Common项目
cd ../MediaMinder.Common
dotnet add package System.Text.Json
dotnet add package Microsoft.Extensions.Configuration.Abstractions
dotnet add package Microsoft.Extensions.Options

# Service项目
cd ../MediaMinder.Service
dotnet add package Microsoft.Extensions.Hosting.WindowsServices
dotnet add package System.Management
dotnet add package System.IO.Pipes
dotnet add package Serilog.Extensions.Hosting
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.EventLog

# UI项目
cd ../MediaMinder.UI
dotnet add package System.IO.Pipes
dotnet add package System.Drawing.Common
dotnet add package System.Threading
dotnet add package TaskScheduler
```

### 第四步: 配置多项目启动
在Visual Studio中：
1. 右键解决方案 → 属性
2. 选择"多个启动项目"
3. 设置Service和UI项目都为"启动"

## 🎯 开发优先级

1. **第一阶段**: 创建Common库，定义数据模型和通信协议
2. **第二阶段**: 实现Service项目，重点解决相机检测和文件下载
3. **第三阶段**: 实现UI项目，重点解决照片展示和用户交互
4. **第四阶段**: 集成测试，解决IPC通信和部署问题
5. **第五阶段**: 性能优化和错误处理完善
6. **第六阶段**: 生产级部署和监控

## 🧪 测试策略

1. **单元测试**: 为每个服务类编写单元测试
2. **集成测试**: 测试服务间通信
3. **端到端测试**: 完整流程测试
4. **部署测试**: 在真实Windows服务环境中测试
5. **压力测试**: 测试大量文件处理能力
6. **异常测试**: 测试各种异常情况的处理
7. **权限测试**: 测试不同权限环境下的运行情况

## 🚀 部署自动化

### 部署脚本 (deploy.bat) - MediaMinder版
```batch
@echo off
echo 开始部署MediaMinder...

REM 停止现有服务
sc stop MediaMinder
sc delete MediaMinder

REM 发布服务
dotnet publish MediaMinder.Service -c Release -o ./publish

REM 创建日志目录
if not exist "C:\ProgramData\MediaMinder\Logs" mkdir "C:\ProgramData\MediaMinder\Logs"

REM 安装服务（添加显示名称）
sc create MediaMinder binPath="%CD%\publish\MediaMinder.Service.exe" start=auto DisplayName="MediaMinder Photo Service"

REM 启动服务（添加错误处理）
sc start MediaMinder
if %errorlevel% neq 0 (
    echo 服务启动失败，错误代码: %errorlevel%
    pause
    exit /b %errorlevel%
)

echo 部署完成！
pause
```

### PowerShell部署脚本 (deploy.ps1) - MediaMinder版
```powershell
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
```

### 自启动管理脚本 (setup-autostart.ps1) - MediaMinder版
```powershell
param(
    [string]$Action = "enable",  # enable, disable, status
    [string]$ExecutablePath = ""
)

$TaskName = "MediaMinder_UI_AutoStart"

switch ($Action.ToLower()) {
    "enable" {
        if ([string]::IsNullOrEmpty($ExecutablePath)) {
            Write-Host "错误: 请提供可执行文件路径" -ForegroundColor Red
            exit 1
        }
        
        try {
            # 创建任务计划
            $action = New-ScheduledTaskAction -Execute $ExecutablePath
            $trigger = New-ScheduledTaskTrigger -AtLogOn
            $principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType InteractiveToken
            $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
            
            Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description "MediaMinder UI Auto Start"
            
            Write-Host "自启动已启用" -ForegroundColor Green
        } catch {
            Write-Host "启用自启动失败: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    "disable" {
        try {
            Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
            Write-Host "自启动已禁用" -ForegroundColor Green
        } catch {
            Write-Host "禁用自启动失败: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    "status" {
        try {
            $task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
            if ($task) {
                Write-Host "自启动状态: $($task.State)" -ForegroundColor Green
            } else {
                Write-Host "自启动未配置" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "检查自启动状态失败: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    default {
        Write-Host "用法: .\setup-autostart.ps1 -Action [enable|disable|status] -ExecutablePath [路径]" -ForegroundColor Yellow
    }
}
```

## 📋 项目检查清单

### 环境验证检查清单
- [ ] .NET SDK 8.0+ 已安装并验证
- [ ] Visual Studio 2022 或 VS Code 已安装
- [ ] PowerShell 5.1+ 已安装并可用
- [ ] Git 2.30+ 已安装并配置
- [ ] 管理员权限已获取
- [ ] Windows SDK 已安装
- [ ] 网络连接正常（用于下载NuGet包）

### Common库检查清单
- [ ] 命名空间设计合理
- [ ] 数据模型完整
- [ ] 通信协议稳定
- [ ] 配置类验证完善
- [ ] 消息类型可扩展
- [ ] .NET Standard 2.0+ 兼容性
- [ ] 抽象接口设计完善
- [ ] 方法签名明确定义

### Service项目检查清单
- [ ] WMI事件监听正常
- [ ] 文件下载功能完整
- [ ] 异常处理完善
- [ ] 资源释放正确
- [ ] 服务启动快速
- [ ] 线程管理正确
- [ ] 日志记录完整
- [ ] 配置绑定正确
- [ ] 强类型配置使用
- [ ] 文件日志和事件日志
- [ ] 权限管理正确

### UI项目检查清单
- [ ] UI线程管理正确
- [ ] 图片加载异步
- [ ] 内存管理优化
- [ ] 文件锁定处理
- [ ] 用户体验良好
- [ ] 进程唯一性实现
- [ ] IPC客户端稳定
- [ ] 异常处理完善
- [ ] 资源释放正确
- [ ] 自启动管理实现

### 集成测试检查清单
- [ ] IPC通信稳定
- [ ] 端到端流程正常
- [ ] 部署配置正确
- [ ] 错误处理完善
- [ ] 多项目启动正常
- [ ] 服务重启正常
- [ ] 资源泄漏检查
- [ ] 日志记录完整
- [ ] 自启动功能正常

## 🔮 未来扩展规划

### 打印功能扩展
当需要添加打印功能时：
1. 在Common库中添加 `PrintRequest` 消息类型
2. 在Service中添加 `PrintService` 组件
3. 在UI中添加打印对话框和打印逻辑
4. 通过IPC通信协调打印任务

### 其他功能扩展
- 图片编辑功能
- 批量处理功能
- 云存储同步
- 多相机支持
- 移动端应用
- 远程监控和管理

## 📚 版本控制和文档管理

### Git工作流
```bash
# 初始化Git仓库
git init
git add .
git commit -m "Initial commit: MediaMinder project structure and documentation"

# 创建开发分支
git checkout -b develop
git checkout -b feature/camera-service
git checkout -b feature/ui-application
git checkout -b feature/ipc-communication

# 合并到主分支
git checkout main
git merge develop
git tag -a v1.0.0 -m "Release version 1.0.0"
```

### 文档维护
- 保持构建指南的实时更新
- 记录所有架构决策和变更
- 维护API文档和接口说明
- 定期更新部署和运维文档

## 🔧 环境配置故障排除

### 常见环境问题及解决方案

#### 1. .NET SDK版本问题
**问题**: `dotnet --version` 显示版本过低
**解决方案**:
```bash
# 下载并安装最新的.NET 8.0 SDK
# 访问: https://dotnet.microsoft.com/download/dotnet/8.0
# 或使用winget安装
winget install Microsoft.DotNet.SDK.8
```

#### 2. PowerShell执行策略问题
**问题**: 无法执行PowerShell脚本
**解决方案**:
```powershell
# 以管理员身份运行PowerShell，然后执行：
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

#### 3. 权限不足问题
**问题**: 无法创建系统目录或安装服务
**解决方案**:
- 确保以管理员身份运行命令提示符或PowerShell
- 检查用户账户控制(UAC)设置
- 验证用户是否在Administrators组中

#### 4. NuGet包下载失败
**问题**: `dotnet restore` 失败
**解决方案**:
```bash
# 清除NuGet缓存
dotnet nuget locals all --clear

# 配置NuGet源
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org

# 重新还原包
dotnet restore
```

#### 5. Windows SDK缺失
**问题**: 编译时出现Windows API相关错误
**解决方案**:
- 安装Visual Studio 2022时选择"Windows 11 SDK"
- 或单独下载安装Windows SDK 10.0.19041.0

#### 6. 目标框架不匹配
**问题**: 项目引用失败或编译错误
**解决方案**:
- 确保Common项目使用 `netstandard2.0`
- 确保Service项目使用 `net8.0`
- 确保UI项目使用 `net8.0-windows`

### 环境验证脚本
创建一个 `verify-environment.ps1` 脚本来验证所有环境要求：

```powershell
Write-Host "=== MediaMinder 环境验证 ===" -ForegroundColor Green

# 检查.NET SDK
$dotnetVersion = dotnet --version
if ($dotnetVersion -match "8\.0\.") {
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

Write-Host "`n=== 环境验证完成 ===" -ForegroundColor Green
```

---

**注意**: 这个MediaMinder项目构建指南基于实际项目经验和最佳实践，确保了项目的**高可维护性、高扩展性和高可靠性**。通过完善的抽象接口设计、强类型配置绑定、全面的日志记录、健壮的部署脚本和自启动管理，您的MediaMinder项目将具备生产级别的质量。建议严格按照步骤执行，确保每个阶段都经过充分测试后再进入下一阶段。
