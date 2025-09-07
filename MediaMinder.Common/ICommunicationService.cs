using System;
using System.Threading.Tasks;

namespace MediaMinder.Common
{
    /// <summary>
    /// 通信服务抽象接口
    /// </summary>
    public interface ICommunicationService : IDisposable
    {
        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">要发送的消息</param>
        /// <returns>异步任务</returns>
        Task SendMessageAsync(IPCMessage message);

        /// <summary>
        /// 开始监听消息
        /// </summary>
        /// <param name="messageHandler">消息处理回调</param>
        void StartListening(Action<IPCMessage> messageHandler);

        /// <summary>
        /// 停止监听
        /// </summary>
        void StopListening();

        /// <summary>
        /// 消息接收事件
        /// </summary>
        event EventHandler<IPCMessage> MessageReceived;

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        event EventHandler<bool> ConnectionStatusChanged;
    }
}
