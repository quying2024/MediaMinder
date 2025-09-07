using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MediaMinder.Common
{
    /// <summary>
    /// 抽象通信协议基类
    /// </summary>
    public abstract class CommunicationProtocol
    {
        /// <summary>
        /// 写入消息到流
        /// </summary>
        /// <param name="stream">目标流</param>
        /// <param name="message">要写入的消息</param>
        /// <returns>异步任务</returns>
        public abstract Task WriteMessageAsync(Stream stream, IPCMessage message);

        /// <summary>
        /// 从流读取消息
        /// </summary>
        /// <param name="stream">源流</param>
        /// <returns>读取到的消息</returns>
        public abstract Task<IPCMessage> ReadMessageAsync(Stream stream);

        /// <summary>
        /// JSON序列化选项
        /// </summary>
        protected static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// 基于长度前缀的通信协议实现
    /// </summary>
    public class LengthPrefixProtocol : CommunicationProtocol
    {
        /// <summary>
        /// 写入消息到流（使用长度前缀）
        /// </summary>
        /// <param name="stream">目标流</param>
        /// <param name="message">要写入的消息</param>
        /// <returns>异步任务</returns>
        public override async Task WriteMessageAsync(Stream stream, IPCMessage message)
        {
            var json = JsonSerializer.Serialize(message, JsonOptions);
            var data = Encoding.UTF8.GetBytes(json);
            var length = BitConverter.GetBytes(data.Length);

            // 先写入长度
            await stream.WriteAsync(length, 0, length.Length);
            // 再写入数据
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }

        /// <summary>
        /// 从流读取消息（使用长度前缀）
        /// </summary>
        /// <param name="stream">源流</param>
        /// <returns>读取到的消息</returns>
        public override async Task<IPCMessage> ReadMessageAsync(Stream stream)
        {
            // 读取长度
            var lengthBytes = new byte[4];
            var bytesRead = await stream.ReadAsync(lengthBytes, 0, 4);
            if (bytesRead != 4)
                throw new InvalidOperationException("无法读取消息长度");

            var length = BitConverter.ToInt32(lengthBytes, 0);
            if (length <= 0 || length > 1024 * 1024) // 1MB限制
                throw new InvalidOperationException($"无效的消息长度: {length}");

            // 读取数据
            var data = new byte[length];
            var totalBytesRead = 0;
            while (totalBytesRead < length)
            {
                var bytesReadThisTime = await stream.ReadAsync(data, totalBytesRead, length - totalBytesRead);
                if (bytesReadThisTime == 0)
                    throw new InvalidOperationException("流意外结束");
                totalBytesRead += bytesReadThisTime;
            }

            var json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<IPCMessage>(json, JsonOptions) 
                ?? throw new InvalidOperationException("无法反序列化消息");
        }
    }
}
