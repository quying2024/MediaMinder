using System;
using System.Text.Json;

namespace MediaMinder.Common
{
    /// <summary>
    /// 统一IPC消息模型
    /// </summary>
    public class IPCMessage
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public MessageType Type { get; set; }

        /// <summary>
        /// 消息数据（JSON格式）
        /// </summary>
        public string Data { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 消息ID（用于跟踪）
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 发送者标识
        /// </summary>
        public string Sender { get; set; } = string.Empty;

        /// <summary>
        /// 接收者标识
        /// </summary>
        public string Receiver { get; set; } = string.Empty;

        /// <summary>
        /// 创建带数据的消息
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="type">消息类型</param>
        /// <param name="data">数据对象</param>
        /// <param name="sender">发送者</param>
        /// <param name="receiver">接收者</param>
        /// <returns>IPC消息</returns>
        public static IPCMessage Create<T>(MessageType type, T data, string sender = "", string receiver = "")
        {
            return new IPCMessage
            {
                Type = type,
                Data = JsonSerializer.Serialize(data, JsonOptions),
                Sender = sender,
                Receiver = receiver
            };
        }

        /// <summary>
        /// 创建简单消息
        /// </summary>
        /// <param name="type">消息类型</param>
        /// <param name="data">数据字符串</param>
        /// <param name="sender">发送者</param>
        /// <param name="receiver">接收者</param>
        /// <returns>IPC消息</returns>
        public static IPCMessage Create(MessageType type, string data = "", string sender = "", string receiver = "")
        {
            return new IPCMessage
            {
                Type = type,
                Data = data,
                Sender = sender,
                Receiver = receiver
            };
        }

        /// <summary>
        /// 反序列化数据
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <returns>反序列化后的对象</returns>
        public T GetData<T>()
        {
            if (string.IsNullOrEmpty(Data))
                return default;

            try
            {
                return JsonSerializer.Deserialize<T>(Data, JsonOptions);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// JSON序列化选项
        /// </summary>
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };
    }
}
