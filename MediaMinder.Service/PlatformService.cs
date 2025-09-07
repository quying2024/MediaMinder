using MediaMinder.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediaMinder.Service
{
    /// <summary>
    /// 主服务协调器
    /// </summary>
    public class PlatformService : BackgroundService
    {
        private readonly ILogger<PlatformService> _logger;
        private readonly ServiceSettings _settings;
        private readonly CameraService _cameraService;
        private readonly PhotoDisplayService _photoDisplayService;
        private readonly ICommunicationService _communicationService;

        public PlatformService(
            ILogger<PlatformService> logger,
            IOptions<ServiceSettings> settings,
            CameraService cameraService,
            PhotoDisplayService photoDisplayService,
            ICommunicationService communicationService)
        {
            _logger = logger;
            _settings = settings.Value;
            _cameraService = cameraService;
            _photoDisplayService = photoDisplayService;
            _communicationService = communicationService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MediaMinder 平台服务启动");

            try
            {
                // 启动通信服务
                _communicationService.StartListening(OnMessageReceived);
                _logger.LogInformation("通信服务已启动");

                // 发送服务启动消息
                await _communicationService.SendMessageAsync(
                    IPCMessage.Create(MessageType.ServiceStarted, "MediaMinder 服务已启动", "Service"));

                // 启动相机服务
                if (_settings.CameraService.Enabled)
                {
                    await _cameraService.StartAsync(stoppingToken);
                    _logger.LogInformation("相机服务已启动");
                }

                // 启动照片展示服务
                if (_settings.PhotoDisplayService.Enabled)
                {
                    await _photoDisplayService.StartAsync(stoppingToken);
                    _logger.LogInformation("照片展示服务已启动");
                }

                // 等待停止信号
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("服务正在停止...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "服务运行时发生错误");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("正在停止 MediaMinder 平台服务");

            try
            {
                // 发送服务停止消息
                await _communicationService.SendMessageAsync(
                    IPCMessage.Create(MessageType.ServiceStopped, "MediaMinder 服务正在停止", "Service"));

                // 停止各个服务
                if (_settings.CameraService.Enabled)
                {
                    await _cameraService.StopAsync(cancellationToken);
                }

                if (_settings.PhotoDisplayService.Enabled)
                {
                    await _photoDisplayService.StopAsync(cancellationToken);
                }

                // 停止通信服务
                _communicationService.StopListening();
                _communicationService.Dispose();

                _logger.LogInformation("MediaMinder 平台服务已停止");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止服务时发生错误");
            }

            await base.StopAsync(cancellationToken);
        }

        private void OnMessageReceived(IPCMessage message)
        {
            try
            {
                _logger.LogDebug("收到消息: {MessageType} from {Sender}", message.Type, message.Sender);

                switch (message.Type)
                {
                    case MessageType.Heartbeat:
                        // 处理心跳消息
                        break;
                    case MessageType.PrintRequest:
                        // 处理打印请求（未来功能）
                        _logger.LogInformation("收到打印请求: {Data}", message.Data);
                        break;
                    default:
                        _logger.LogDebug("未处理的消息类型: {MessageType}", message.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理消息时发生错误: {MessageType}", message.Type);
            }
        }
    }
}
