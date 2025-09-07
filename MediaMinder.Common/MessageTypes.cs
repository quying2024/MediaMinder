namespace MediaMinder.Common
{
    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// 新照片可用
        /// </summary>
        NewPhotosAvailable,

        /// <summary>
        /// 状态更新
        /// </summary>
        StatusUpdate,

        /// <summary>
        /// 相机事件
        /// </summary>
        CameraEvent,

        /// <summary>
        /// 服务已启动
        /// </summary>
        ServiceStarted,

        /// <summary>
        /// 服务已停止
        /// </summary>
        ServiceStopped,

        /// <summary>
        /// 打印请求（为未来打印功能预留）
        /// </summary>
        PrintRequest,

        /// <summary>
        /// 错误消息
        /// </summary>
        Error,

        /// <summary>
        /// 心跳消息
        /// </summary>
        Heartbeat
    }

    /// <summary>
    /// 相机事件类型
    /// </summary>
    public enum CameraEventType
    {
        /// <summary>
        /// 相机插入
        /// </summary>
        CameraInserted,

        /// <summary>
        /// 相机移除
        /// </summary>
        CameraRemoved,

        /// <summary>
        /// 开始下载
        /// </summary>
        DownloadStarted,

        /// <summary>
        /// 下载完成
        /// </summary>
        DownloadCompleted,

        /// <summary>
        /// 下载失败
        /// </summary>
        DownloadFailed
    }

    /// <summary>
    /// 服务状态
    /// </summary>
    public enum ServiceStatus
    {
        /// <summary>
        /// 未知状态
        /// </summary>
        Unknown,

        /// <summary>
        /// 正在启动
        /// </summary>
        Starting,

        /// <summary>
        /// 运行中
        /// </summary>
        Running,

        /// <summary>
        /// 正在停止
        /// </summary>
        Stopping,

        /// <summary>
        /// 已停止
        /// </summary>
        Stopped,

        /// <summary>
        /// 错误状态
        /// </summary>
        Error
    }
}
