using MediaMinder.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Management;

namespace MediaMinder.Service
{
    /// <summary>
    /// 相机检测和图片下载服务
    /// </summary>
    public class CameraService : IDisposable
    {
        private readonly ILogger<CameraService> _logger;
        private readonly ServiceSettings _settings;
        private readonly ICommunicationService _communicationService;
        private ManagementEventWatcher? _deviceWatcher;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isRunning;
        private DateTime _lastDownloadTime = DateTime.MinValue;

        public CameraService(
            ILogger<CameraService> logger,
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

            _logger.LogInformation("启动相机服务");

            try
            {
                // 确保目标目录存在
                EnsureTargetDirectoryExists();

                // 启动WMI设备监控
                StartDeviceMonitoring();

                // 发送相机服务启动消息
                await _communicationService.SendMessageAsync(
                    IPCMessage.Create(MessageType.StatusUpdate, "相机服务已启动", "CameraService"));

                _logger.LogInformation("相机服务启动成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动相机服务失败");
                _isRunning = false;
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_isRunning)
                return;

            _logger.LogInformation("停止相机服务");

            try
            {
                _isRunning = false;
                _cancellationTokenSource?.Cancel();

                // 停止设备监控
                StopDeviceMonitoring();

                // 发送相机服务停止消息
                await _communicationService.SendMessageAsync(
                    IPCMessage.Create(MessageType.StatusUpdate, "相机服务已停止", "CameraService"));

                _logger.LogInformation("相机服务已停止");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止相机服务时发生错误");
            }
        }

        private void StartDeviceMonitoring()
        {
            try
            {
                // 监控USB设备插入事件
                var query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
                _deviceWatcher = new ManagementEventWatcher(query);
                _deviceWatcher.EventArrived += OnDeviceInserted;
                _deviceWatcher.Start();

                _logger.LogInformation("设备监控已启动");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动设备监控失败");
                throw;
            }
        }

        private void StopDeviceMonitoring()
        {
            try
            {
                if (_deviceWatcher != null)
                {
                    _deviceWatcher.EventArrived -= OnDeviceInserted;
                    _deviceWatcher.Stop();
                    _deviceWatcher.Dispose();
                    _deviceWatcher = null;
                }

                _logger.LogInformation("设备监控已停止");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止设备监控时发生错误");
            }
        }

        private async void OnDeviceInserted(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var driveName = e.NewEvent["DriveName"]?.ToString();
                if (string.IsNullOrEmpty(driveName))
                    return;

                _logger.LogInformation("检测到设备插入: {DriveName}", driveName);

                // 检查是否是Canon相机
                if (IsCanonCamera(driveName))
                {
                    _logger.LogInformation("检测到Canon相机: {DriveName}", driveName);
                    
                    // 发送相机插入事件
                    await _communicationService.SendMessageAsync(
                        IPCMessage.Create(MessageType.CameraEvent, 
                            new { Type = CameraEventType.CameraInserted, DriveName = driveName }, 
                            "CameraService"));

                    // 等待设备稳定后开始下载
                    await Task.Delay(2000, _cancellationTokenSource?.Token ?? CancellationToken.None);
                    await DownloadPhotosFromCamera(driveName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理设备插入事件时发生错误");
            }
        }

        private bool IsCanonCamera(string driveName)
        {
            try
            {
                var drive = new DriveInfo(driveName);
                if (!drive.IsReady)
                    return false;

                var volumeLabel = drive.VolumeLabel;
                return !string.IsNullOrEmpty(volumeLabel) && 
                       volumeLabel.StartsWith(_settings.CameraService.CanonDrivePrefix, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查驱动器 {DriveName} 时发生错误", driveName);
                return false;
            }
        }

        private async Task DownloadPhotosFromCamera(string driveName)
        {
            try
            {
                // 检查冷却时间
                if (DateTime.Now - _lastDownloadTime < TimeSpan.FromSeconds(_settings.CameraService.CooldownSeconds))
                {
                    _logger.LogInformation("下载冷却中，跳过此次下载");
                    return;
                }

                _logger.LogInformation("开始从相机下载照片: {DriveName}", driveName);

                // 发送下载开始事件
                await _communicationService.SendMessageAsync(
                    IPCMessage.Create(MessageType.CameraEvent,
                        new { Type = CameraEventType.DownloadStarted, DriveName = driveName },
                        "CameraService"));

                var dcimPath = Path.Combine(driveName, _settings.CameraService.DcimPath);
                if (!Directory.Exists(dcimPath))
                {
                    _logger.LogWarning("DCIM目录不存在: {DcimPath}", dcimPath);
                    return;
                }

                var downloadedCount = 0;
                var photoFiles = Directory.GetFiles(dcimPath, "*.*", SearchOption.AllDirectories)
                    .Where(file => _settings.PhotoDisplayService.SupportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    .ToArray();

                foreach (var photoFile in photoFiles)
                {
                    if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                        break;

                    if (await DownloadPhoto(photoFile))
                    {
                        downloadedCount++;
                    }
                }

                _lastDownloadTime = DateTime.Now;

                // 发送下载完成事件
                await _communicationService.SendMessageAsync(
                    IPCMessage.Create(MessageType.CameraEvent,
                        new { Type = CameraEventType.DownloadCompleted, DriveName = driveName, Count = downloadedCount },
                        "CameraService"));

                // 发送新照片可用消息
                if (downloadedCount > 0)
                {
                    await _communicationService.SendMessageAsync(
                        IPCMessage.Create(MessageType.NewPhotosAvailable, downloadedCount.ToString(), "CameraService"));
                }

                _logger.LogInformation("照片下载完成，共下载 {Count} 张照片", downloadedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载照片时发生错误: {DriveName}", driveName);

                // 发送下载失败事件
                await _communicationService.SendMessageAsync(
                    IPCMessage.Create(MessageType.CameraEvent,
                        new { Type = CameraEventType.DownloadFailed, DriveName = driveName, Error = ex.Message },
                        "CameraService"));
            }
        }

        private async Task<bool> DownloadPhoto(string sourceFile)
        {
            try
            {
                var fileName = Path.GetFileName(sourceFile);
                var targetPath = Path.Combine(_settings.CameraService.TargetDirectory, fileName);

                // 如果文件已存在，添加时间戳
                if (File.Exists(targetPath))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var extension = Path.GetExtension(fileName);
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    fileName = $"{nameWithoutExt}_{timestamp}{extension}";
                    targetPath = Path.Combine(_settings.CameraService.TargetDirectory, fileName);
                }

                File.Copy(sourceFile, targetPath, true);
                _logger.LogDebug("照片下载成功: {FileName}", fileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载照片失败: {SourceFile}", sourceFile);
                return false;
            }
        }

        private void EnsureTargetDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_settings.CameraService.TargetDirectory))
                {
                    Directory.CreateDirectory(_settings.CameraService.TargetDirectory);
                    _logger.LogInformation("创建目标目录: {TargetDirectory}", _settings.CameraService.TargetDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建目标目录失败: {TargetDirectory}", _settings.CameraService.TargetDirectory);
                throw;
            }
        }

        public void Dispose()
        {
            StopDeviceMonitoring();
            _cancellationTokenSource?.Dispose();
        }
    }
}
