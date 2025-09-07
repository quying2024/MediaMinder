using MediaMinder.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Pipes;

namespace MediaMinder.Service
{
    /// <summary>
    /// 命名管道通信服务实现
    /// </summary>
    public class NamedPipeCommunicationService : ICommunicationService
    {
        private readonly ILogger<NamedPipeCommunicationService> _logger;
        private readonly CommunicationSettings _settings;
        private readonly CommunicationProtocol _protocol;
        private NamedPipeServerStream? _pipeServer;
        private CancellationTokenSource? _cancellationTokenSource;
        private Action<IPCMessage> _messageHandler;
        private bool _isListening;

        public bool IsConnected => _pipeServer?.IsConnected ?? false;

        public event EventHandler<IPCMessage> MessageReceived;
        public event EventHandler<bool> ConnectionStatusChanged;

        public NamedPipeCommunicationService(
            ILogger<NamedPipeCommunicationService> logger,
            IOptions<ServiceSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value.Communication;
            _protocol = new LengthPrefixProtocol();
        }

        public async Task SendMessageAsync(IPCMessage message)
        {
            if (!IsConnected)
            {
                _logger.LogWarning("管道未连接，无法发送消息: {MessageType}", message.Type);
                return;
            }

            try
            {
                await _protocol.WriteMessageAsync(_pipeServer!, message);
                _logger.LogDebug("消息发送成功: {MessageType}", message.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送消息失败: {MessageType}", message.Type);
                throw;
            }
        }

        public void StartListening(Action<IPCMessage> messageHandler)
        {
            if (_isListening)
                return;

            _messageHandler = messageHandler;
            _cancellationTokenSource = new CancellationTokenSource();
            _isListening = true;

            _logger.LogInformation("开始监听命名管道: {PipeName}", _settings.PipeName);

            // 启动监听任务
            _ = Task.Run(async () => await ListenForConnectionsAsync(_cancellationTokenSource.Token));
        }

        public void StopListening()
        {
            if (!_isListening)
                return;

            _logger.LogInformation("停止监听命名管道");

            _isListening = false;
            _cancellationTokenSource?.Cancel();

            try
            {
                _pipeServer?.Dispose();
                _pipeServer = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "停止监听时发生错误");
            }
        }

        private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
        {
            while (_isListening && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 创建命名管道服务器
                    _pipeServer = new NamedPipeServerStream(
                        _settings.PipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    _logger.LogInformation("等待客户端连接...");

                    // 等待客户端连接
                    await _pipeServer.WaitForConnectionAsync(cancellationToken);

                    _logger.LogInformation("客户端已连接");
                    OnConnectionStatusChanged(true);

                    // 处理客户端消息
                    await HandleClientMessagesAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("监听已取消");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "监听连接时发生错误");
                    
                    // 等待一段时间后重试
                    try
                    {
                        await Task.Delay(5000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                finally
                {
                    try
                    {
                        _pipeServer?.Dispose();
                        _pipeServer = null;
                        OnConnectionStatusChanged(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "清理管道资源时发生错误");
                    }
                }
            }
        }

        private async Task HandleClientMessagesAsync(CancellationToken cancellationToken)
        {
            while (_isListening && IsConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 读取消息
                    var message = await _protocol.ReadMessageAsync(_pipeServer!);
                    
                    _logger.LogDebug("收到客户端消息: {MessageType}", message.Type);

                    // 处理消息
                    _messageHandler?.Invoke(message);
                    MessageReceived?.Invoke(this, message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理客户端消息时发生错误");
                    break;
                }
            }
        }

        private void OnConnectionStatusChanged(bool isConnected)
        {
            try
            {
                ConnectionStatusChanged?.Invoke(this, isConnected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "触发连接状态变化事件时发生错误");
            }
        }

        public void Dispose()
        {
            StopListening();
            _cancellationTokenSource?.Dispose();
        }
    }
}
