using MediaMinder.Common;
using System.IO.Pipes;

namespace MediaMinder.UI
{
    /// <summary>
    /// 命名管道客户端
    /// </summary>
    public class NamedPipeClient : ICommunicationService
    {
        private readonly CommunicationSettings _settings;
        private readonly CommunicationProtocol _protocol;
        private NamedPipeClientStream? _pipeClient;
        private CancellationTokenSource? _cancellationTokenSource;
        private Action<IPCMessage> _messageHandler;
        private bool _isConnected;
        private bool _isListening;

        public bool IsConnected => _isConnected && _pipeClient?.IsConnected == true;

        public event EventHandler<IPCMessage> MessageReceived;
        public event EventHandler<bool> ConnectionStatusChanged;

        public NamedPipeClient(CommunicationSettings settings)
        {
            _settings = settings;
            _protocol = new LengthPrefixProtocol();
        }

        public async Task SendMessageAsync(IPCMessage message)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("管道未连接");
            }

            try
            {
                await _protocol.WriteMessageAsync(_pipeClient!, message);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"发送消息失败: {ex.Message}", ex);
            }
        }

        public void StartListening(Action<IPCMessage> messageHandler)
        {
            if (_isListening)
                return;

            _messageHandler = messageHandler;
            _cancellationTokenSource = new CancellationTokenSource();
            _isListening = true;

            // 启动连接和监听任务
            _ = Task.Run(async () => await ConnectAndListenAsync(_cancellationTokenSource.Token));
        }

        public void StopListening()
        {
            if (!_isListening)
                return;

            _isListening = false;
            _cancellationTokenSource?.Cancel();

            try
            {
                _pipeClient?.Dispose();
                _pipeClient = null;
                _isConnected = false;
                OnConnectionStatusChanged(false);
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常
                System.Diagnostics.Debug.WriteLine($"停止监听时发生错误: {ex.Message}");
            }
        }

        private async Task ConnectAndListenAsync(CancellationToken cancellationToken)
        {
            while (_isListening && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 尝试连接
                    await ConnectAsync(cancellationToken);

                    if (_isConnected)
                    {
                        // 监听消息
                        await ListenForMessagesAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"连接或监听时发生错误: {ex.Message}");
                }

                // 如果连接断开，等待一段时间后重试
                if (_isListening && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(5000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                _pipeClient = new NamedPipeClientStream(".", _settings.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                
                // 设置连接超时
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(_settings.ConnectionTimeoutMs);

                await _pipeClient.ConnectAsync(timeoutCts.Token);
                _isConnected = true;
                OnConnectionStatusChanged(true);
            }
            catch (Exception ex)
            {
                _isConnected = false;
                OnConnectionStatusChanged(false);
                throw new InvalidOperationException($"连接失败: {ex.Message}", ex);
            }
        }

        private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
        {
            while (_isListening && IsConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 读取消息
                    var message = await _protocol.ReadMessageAsync(_pipeClient!);
                    
                    // 处理消息
                    _messageHandler?.Invoke(message);
                    MessageReceived?.Invoke(this, message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"处理消息时发生错误: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"触发连接状态变化事件时发生错误: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopListening();
            _cancellationTokenSource?.Dispose();
        }
    }
}
