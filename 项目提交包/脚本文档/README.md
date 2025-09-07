# MediaMinder 项目

MediaMinder 是一个自动化照片管理系统，用于检测 USB 相机插入，自动下载图片到本地，并提供照片展示界面。

## 🏗️ 项目架构

```
MediaMinder/
├── MediaMinder.Common/     # 共享库 (.NET Standard 2.0)
├── MediaMinder.Service/    # 后台服务 (Worker Service)
├── MediaMinder.UI/         # 前台UI应用 (WinForms)
├── docs/                   # 项目文档
├── scripts/                # 部署和维护脚本
└── MediaMinder.sln         # 解决方案文件
```

## 🚀 快速开始

### 环境要求

- **.NET SDK**: 8.0 或更高版本
- **操作系统**: Windows 10/11
- **PowerShell**: 5.1 或更高版本
- **管理员权限**: 用于安装和配置Windows服务

### 编译项目

```bash
# 克隆项目（如果从Git）
git clone <repository-url>
cd MediaMinder

# 验证环境
.\scripts\verify-environment.ps1

# 编译解决方案
dotnet build

# 发布Release版本
dotnet publish MediaMinder.Service -c Release -o ./publish
```

### 部署服务

```powershell
# 以管理员身份运行PowerShell
.\scripts\deploy.ps1
```

### 启动UI应用

```bash
# 直接运行
dotnet run --project MediaMinder.UI

# 或运行编译后的exe
.\MediaMinder.UI\bin\Debug\net9.0-windows\MediaMinder.UI.exe
```

## 📋 主要功能

### MediaMinder.Service (后台服务)
- 🔍 **相机检测**: 自动检测Canon相机插入
- 📸 **照片下载**: 自动从相机DCIM目录下载照片
- 📁 **文件监控**: 监控照片目录变化
- 💬 **IPC通信**: 通过命名管道与UI通信
- 📝 **日志记录**: 详细的日志记录和错误处理

### MediaMinder.UI (前台应用)
- 🖼️ **照片展示**: 6格照片展示界面
- 🔄 **实时更新**: 自动刷新最新照片
- 📱 **进程管理**: 单实例运行保护
- ⚙️ **自启动**: 支持Windows自启动管理
- 💬 **服务通信**: 与后台服务实时通信

### MediaMinder.Common (共享库)
- 📦 **数据模型**: 统一的数据结构定义
- 🔧 **配置管理**: 强类型配置支持
- 💬 **通信协议**: IPC消息协议实现
- 🔌 **抽象接口**: 可扩展的通信接口

## ⚙️ 配置说明

主要配置文件：`MediaMinder.Service/appsettings.json`

```json
{
  "ServiceSettings": {
    "CameraService": {
      "Enabled": true,
      "CanonDrivePrefix": "Canon G16",
      "DcimPath": "DCIM",
      "TargetDirectory": "C:\\ProgramData\\MediaMinder\\G16",
      "CooldownSeconds": 30
    },
    "PhotoDisplayService": {
      "Enabled": true,
      "PhotosDirectory": "C:\\ProgramData\\MediaMinder\\G16",
      "MaxDisplayPhotos": 6
    },
    "Communication": {
      "PipeName": "MediaMinder_Pipe",
      "ConnectionTimeoutMs": 5000
    }
  }
}
```

## 🔧 维护脚本

### 环境验证
```powershell
.\scripts\verify-environment.ps1
```

### 部署服务
```powershell
.\scripts\deploy.ps1
```

### 自启动管理
```powershell
# 启用自启动
.\scripts\setup-autostart.ps1 -Action enable -ExecutablePath "C:\Path\To\MediaMinder.UI.exe"

# 禁用自启动
.\scripts\setup-autostart.ps1 -Action disable

# 检查状态
.\scripts\setup-autostart.ps1 -Action status
```

## 📁 目录结构

- **C:\ProgramData\MediaMinder\G16**: 照片存储目录
- **C:\ProgramData\MediaMinder\Logs**: 服务日志目录
- **应用程序事件日志**: Windows事件查看器中的"MediaMinder"

## 🔍 故障排除

### 服务无法启动
1. 检查管理员权限
2. 查看事件日志中的错误信息
3. 确认目录权限和可用空间

### UI无法连接服务
1. 确认服务正在运行：`Get-Service MediaMinder`
2. 检查命名管道权限
3. 查看服务日志文件

### 相机检测不工作
1. 确认相机驱动器标签包含"Canon G16"
2. 检查DCIM目录是否存在
3. 验证文件访问权限

## 🏷️ 版本信息

- **当前版本**: 1.0.0
- **.NET版本**: .NET 9.0
- **目标平台**: Windows x64

## 📄 许可证

本项目采用 MIT 许可证。详见 LICENSE 文件。

## 🤝 贡献

欢迎提交Issue和Pull Request来改进项目。

## 📞 支持

如有问题或建议，请通过以下方式联系：
- 创建GitHub Issue
- 发送邮件至项目维护者

---

**注意**: 本项目专为Windows平台设计，使用了Windows特有的API（如WMI、任务计划程序等）。