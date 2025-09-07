# MediaMinder

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Windows](https://img.shields.io/badge/Platform-Windows-blue.svg)](https://www.microsoft.com/windows)

MediaMinder是一个智能相机照片管理工具，采用客户端-服务端（C/S）分离架构，能够自动检测USB相机插入，下载照片到本地，并提供直观的照片展示界面。

## ✨ 主要特性

- 🎯 **智能相机识别**: 支持34个主流相机品牌和系列
- 📸 **自动照片下载**: 智能下载最新照片并验证完整性
- 🖼️ **现代化界面**: 自适应尺寸、无限滚动、Photoshop优先打开
- 🔄 **自动备份清理**: 智能备份和清理系统，避免文件句柄占用
- 🔗 **进程间通信**: 高效的命名管道通信机制
- ⚙️ **强类型配置**: 灵活的配置管理和验证
- 📊 **详细日志**: 结构化日志记录和监控

## 🚀 快速开始

### 系统要求

- **操作系统**: Windows 10/11 或 Windows Server 2019/2022
- **.NET Runtime**: .NET 9.0 Runtime
- **内存**: 4GB RAM (推荐8GB)
- **存储**: 1GB可用磁盘空间
- **权限**: 管理员权限（用于安装Windows服务）

### 安装步骤

1. **下载MediaMinder**
   ```bash
   git clone https://github.com/your-org/MediaMinder.git
   cd MediaMinder
   ```

2. **安装依赖**
   ```powershell
   # 安装.NET 9.0 Runtime
   # 从 https://dotnet.microsoft.com/download/dotnet/9.0 下载
   ```

3. **构建项目**
   ```powershell
   dotnet build
   ```

4. **发布应用**
   ```powershell
   dotnet publish MediaMinder.Service -c Release -o "C:\ProgramData\MediaMinder\Service"
   dotnet publish MediaMinder.UI -c Release -o "C:\ProgramData\MediaMinder\UI"
   ```

5. **安装服务**
   ```powershell
   sc create "MediaMinder" binPath="C:\ProgramData\MediaMinder\Service\MediaMinder.Service.exe" start=auto
   sc start "MediaMinder"
   ```

6. **启动UI**
   ```powershell
   cd "C:\ProgramData\MediaMinder\UI"
   .\MediaMinder.UI.exe
   ```

## 📖 使用指南

### 基本使用

1. **启动MediaMinder**: 双击桌面图标或从开始菜单启动
2. **连接相机**: 将相机通过USB连接到电脑
3. **自动下载**: 系统自动识别相机并下载照片
4. **查看照片**: 在界面中浏览下载的照片
5. **打开照片**: 双击照片使用Photoshop或默认程序打开

### 支持的相机品牌

- **Canon**: G16, EOS, PowerShot, Digital系列
- **Nikon**: D系列, Z系列, COOLPIX系列  
- **Sony**: DSC, α系列, ILCE系列
- **其他品牌**: FUJIFILM, Panasonic, Olympus, Leica, Pentax, Ricoh等

### 主要功能

- **智能识别**: 基于卷标名和DCIM目录结构识别相机
- **自动下载**: 只下载最新生成的子目录中的照片
- **完整性验证**: 验证下载文件的完整性
- **源文件删除**: 下载成功后自动删除相机中的源文件
- **自动备份**: 关闭窗口时自动备份所有照片
- **过期清理**: 自动清理超过1周的备份目录

## 🏗️ 项目结构

```
MediaMinder/
├── MediaMinder.Common/          # 共享库
│   ├── ServiceSettings.cs       # 强类型配置模型
│   ├── MessageTypes.cs          # IPC消息类型定义
│   ├── PhotoInfo.cs             # 照片信息模型
│   └── ICommunicationService.cs # 通信服务接口
├── MediaMinder.Service/         # 后台服务
│   ├── Program.cs               # 服务入口点
│   ├── CameraService.cs         # 相机检测和下载服务
│   ├── PhotoDisplayService.cs   # 照片目录监控服务
│   └── appsettings.json         # 服务配置文件
├── MediaMinder.UI/              # 前台应用
│   ├── Program.cs               # UI入口点
│   ├── PhotoDisplayForm.cs      # 照片展示窗体
│   ├── PhotoBackupCleanupService.cs    # 备份清理服务
│   └── PhotoBackupCleanupProcess.cs    # 备份清理进程
└── docs/                        # 项目文档
    ├── 项目创建总结.md
    ├── 技术架构文档.md
    ├── 功能特性文档.md
    ├── 部署指南.md
    ├── 用户使用手册.md
    └── 开发者文档.md
```

## 🔧 配置说明

### 基本配置

```json
{
  "ServiceSettings": {
    "CameraService": {
      "Enabled": true,
      "TargetDirectory": "C:\\ProgramData\\MediaMinder\\Photos",
      "CameraDrivePrefixes": ["Canon G16", "Nikon", "Sony"]
    },
    "PhotoDisplayService": {
      "Enabled": true,
      "PhotosDirectory": "C:\\ProgramData\\MediaMinder\\Photos"
    }
  }
}
```

### 高级配置

- **相机识别配置**: 品牌前缀、验证选项等
- **连接优化配置**: 冷却时间、重试次数等
- **下载配置**: 验证选项、删除选项等
- **显示配置**: 缩略图大小、支持格式等

详细配置说明请参考[部署指南](docs/部署指南.md)。

## 📚 文档

- [项目创建总结](docs/项目创建总结.md) - 项目概述和技术总结
- [技术架构文档](docs/技术架构文档.md) - 详细的技术架构说明
- [功能特性文档](docs/功能特性文档.md) - 完整的功能特性介绍
- [部署指南](docs/部署指南.md) - 详细的部署和配置指南
- [用户使用手册](docs/用户使用手册.md) - 用户使用指南
- [开发者文档](docs/开发者文档.md) - 开发者指南和API文档

## 🛠️ 开发

### 开发环境设置

1. **安装开发工具**
   ```powershell
   # 安装Visual Studio 2022
   # 安装.NET 9.0 SDK
   # 安装Git
   ```

2. **克隆项目**
   ```bash
   git clone https://github.com/your-org/MediaMinder.git
   cd MediaMinder
   ```

3. **还原依赖**
   ```powershell
   dotnet restore
   ```

4. **构建项目**
   ```powershell
   dotnet build
   ```

5. **运行测试**
   ```powershell
   dotnet test
   ```

### 开发指南

- 遵循C#编码规范
- 使用异步编程模式
- 实现完整的异常处理
- 编写单元测试
- 更新相关文档

详细开发指南请参考[开发者文档](docs/开发者文档.md)。

## 🐛 问题报告

如果您遇到问题或有建议，请：

1. 查看[用户使用手册](docs/用户使用手册.md)中的故障排除部分
2. 检查[常见问题](docs/用户使用手册.md#故障排除)
3. 在GitHub上创建[Issue](https://github.com/your-org/MediaMinder/issues)

## 🤝 贡献

我们欢迎各种形式的贡献：

- 🐛 报告bug
- 💡 提出新功能建议
- 📝 改进文档
- 🔧 提交代码修复
- 🧪 编写测试

请查看[贡献指南](docs/开发者文档.md#贡献指南)了解详细信息。

## 📄 许可证

本项目采用MIT许可证。详情请查看[LICENSE](LICENSE)文件。

## 🙏 致谢

感谢所有为MediaMinder项目做出贡献的开发者和用户。

## 📞 联系我们

- **项目主页**: https://github.com/your-org/MediaMinder
- **问题反馈**: https://github.com/your-org/MediaMinder/issues
- **技术支持**: support@mediaminder.com
- **用户论坛**: https://forum.mediaminder.com

---

**MediaMinder团队**  
2024年12月20日