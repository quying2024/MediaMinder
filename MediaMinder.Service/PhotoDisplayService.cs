using MediaMinder.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediaMinder.Service
{
    /// <summary>
    /// 照片目录监控服务
    /// </summary>
    public class PhotoDisplayService : IDisposable
    {
        private readonly ILogger<PhotoDisplayService> _logger;
        private readonly ServiceSettings _settings;
        private readonly ICommunicationService _communicationService;
        private FileSystemWatcher? _fileWatcher;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isRunning;

        public PhotoDisplayService(
            ILogger<PhotoDisplayService> logger,
            IOptions<ServiceSettings> settings,
            ICommunicationService communicationService)
        {
            _logger = logger;
            _settings = settings.Value;
            _communicationService = communicationService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isRunning)
                return;

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _isRunning = true;

            _logger.LogInformation("启动照片展示服务");

            try
            {
                // 确保照片目录存在
                EnsurePhotosDirectoryExists();

                // 启动文件监控
                StartFileMonitoring();

                // 发送照片展示服务启动消息
                await _communicationService.SendMessageAsync(
                    IPCMessage.Create(MessageType.StatusUpdate, "照片展示服务已启动", "PhotoDisplayService"));

                _logger.LogInformation("照片展示服务启动成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动照片展示服务失败");
                _isRunning = false;
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_isRunning)
                return;

            _logger.LogInformation("停止照片展示服务");

            try
            {
                _isRunning = false;
                _cancellationTokenSource?.Cancel();

                // 停止文件监控
                StopFileMonitoring();

                // 发送照片展示服务停止消息
                await _communicationService.SendMessageAsync(
                    IPCMessage.Create(MessageType.StatusUpdate, "照片展示服务已停止", "PhotoDisplayService"));

                _logger.LogInformation("照片展示服务已停止");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止照片展示服务时发生错误");
            }
        }

        private void StartFileMonitoring()
        {
            try
            {
                _fileWatcher = new FileSystemWatcher(_settings.PhotoDisplayService.PhotosDirectory)
                {
                    NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };

                _fileWatcher.Created += OnFileCreated;
                _fileWatcher.Changed += OnFileChanged;
                _fileWatcher.Error += OnFileWatcherError;

                _logger.LogInformation("文件监控已启动，监控目录: {PhotosDirectory}", _settings.PhotoDisplayService.PhotosDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动文件监控失败");
                throw;
            }
        }

        private void StopFileMonitoring()
        {
            try
            {
                if (_fileWatcher != null)
                {
                    _fileWatcher.Created -= OnFileCreated;
                    _fileWatcher.Changed -= OnFileChanged;
                    _fileWatcher.Error -= OnFileWatcherError;
                    _fileWatcher.Dispose();
                    _fileWatcher = null;
                }

                _logger.LogInformation("文件监控已停止");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止文件监控时发生错误");
            }
        }

        private async void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            await HandleFileEvent(e, "文件创建");
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            await HandleFileEvent(e, "文件修改");
        }

        private async Task HandleFileEvent(FileSystemEventArgs e, string eventType)
        {
            try
            {
                if (e.FullPath == null)
                    return;

                var extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
                if (!_settings.PhotoDisplayService.SupportedExtensions.Contains(extension))
                    return;

                _logger.LogDebug("{EventType}: {FileName}", eventType, Path.GetFileName(e.FullPath));

                // 等待文件写入完成
                await Task.Delay(1000, _cancellationTokenSource?.Token ?? CancellationToken.None);

                // 检查文件是否可访问
                if (!IsFileAccessible(e.FullPath))
                    return;

                // 创建照片信息
                var photoInfo = await CreatePhotoInfo(e.FullPath);
                if (photoInfo != null)
                {
                    // 发送新照片信息
                    await _communicationService.SendMessageAsync(
                        IPCMessage.Create(MessageType.NewPhotosAvailable, photoInfo, "PhotoDisplayService"));

                    _logger.LogDebug("发送新照片信息: {FileName}", photoInfo.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理文件事件时发生错误: {EventType} - {FileName}", eventType, e.FullPath);
            }
        }

        private void OnFileWatcherError(object sender, ErrorEventArgs e)
        {
            _logger.LogError(e.GetException(), "文件监控发生错误");
        }

        private bool IsFileAccessible(string filePath)
        {
            try
            {
                using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return fileStream.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task<PhotoInfo?> CreatePhotoInfo(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                    return null;

                return new PhotoInfo
                {
                    FileName = fileInfo.Name,
                    FullPath = fileInfo.FullName,
                    FileSize = fileInfo.Length,
                    CreatedTime = fileInfo.CreationTime,
                    ModifiedTime = fileInfo.LastWriteTime,
                    Extension = fileInfo.Extension.ToLowerInvariant(),
                    IsNew = true,
                    SourceDevice = "Canon G16"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建照片信息失败: {FilePath}", filePath);
                return null;
            }
        }

        private void EnsurePhotosDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_settings.PhotoDisplayService.PhotosDirectory))
                {
                    Directory.CreateDirectory(_settings.PhotoDisplayService.PhotosDirectory);
                    _logger.LogInformation("创建照片目录: {PhotosDirectory}", _settings.PhotoDisplayService.PhotosDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建照片目录失败: {PhotosDirectory}", _settings.PhotoDisplayService.PhotosDirectory);
                throw;
            }
        }

        public void Dispose()
        {
            StopFileMonitoring();
            _cancellationTokenSource?.Dispose();
        }
    }
}
