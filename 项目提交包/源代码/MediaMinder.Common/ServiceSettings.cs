using System;
using System.ComponentModel.DataAnnotations;

namespace MediaMinder.Common
{
    /// <summary>
    /// 服务配置设置
    /// </summary>
    public class ServiceSettings
    {
        /// <summary>
        /// 相机服务配置
        /// </summary>
        public CameraServiceSettings CameraService { get; set; } = new CameraServiceSettings();

        /// <summary>
        /// 照片展示服务配置
        /// </summary>
        public PhotoDisplayServiceSettings PhotoDisplayService { get; set; } = new PhotoDisplayServiceSettings();

        /// <summary>
        /// 通信配置
        /// </summary>
        public CommunicationSettings Communication { get; set; } = new CommunicationSettings();
    }

    /// <summary>
    /// 相机服务配置
    /// </summary>
    public class CameraServiceSettings
    {
        /// <summary>
        /// 是否启用相机服务
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Canon相机驱动器前缀
        /// </summary>
        [Required]
        public string CanonDrivePrefix { get; set; } = "Canon G16";

        /// <summary>
        /// DCIM路径
        /// </summary>
        [Required]
        public string DcimPath { get; set; } = "DCIM";

        /// <summary>
        /// 目标下载目录
        /// </summary>
        [Required]
        public string TargetDirectory { get; set; } = @"C:\ProgramData\MediaMinder\G16";

        /// <summary>
        /// 冷却时间（秒）
        /// </summary>
        [Range(1, 300)]
        public int CooldownSeconds { get; set; } = 30;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        [Range(1, 10)]
        public int MaxRetryAttempts { get; set; } = 5;

        /// <summary>
        /// 重试延迟（毫秒）
        /// </summary>
        [Range(100, 10000)]
        public int RetryDelayMs { get; set; } = 1000;
    }

    /// <summary>
    /// 照片展示服务配置
    /// </summary>
    public class PhotoDisplayServiceSettings
    {
        /// <summary>
        /// 是否启用照片展示服务
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 照片目录
        /// </summary>
        [Required]
        public string PhotosDirectory { get; set; } = @"C:\ProgramData\MediaMinder\G16";

        /// <summary>
        /// 最大缩略图大小
        /// </summary>
        [Range(50, 500)]
        public int MaxThumbnailSize { get; set; } = 200;

        /// <summary>
        /// 刷新延迟（毫秒）
        /// </summary>
        [Range(100, 10000)]
        public int RefreshDelayMs { get; set; } = 1000;

        /// <summary>
        /// 最大显示照片数量
        /// </summary>
        [Range(1, 20)]
        public int MaxDisplayPhotos { get; set; } = 6;

        /// <summary>
        /// 支持的图片扩展名
        /// </summary>
        public string[] SupportedExtensions { get; set; } = 
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".raw", ".cr2"
        };
    }

    /// <summary>
    /// 通信配置
    /// </summary>
    public class CommunicationSettings
    {
        /// <summary>
        /// 命名管道名称
        /// </summary>
        [Required]
        public string PipeName { get; set; } = "MediaMinder_Pipe";

        /// <summary>
        /// 连接超时（毫秒）
        /// </summary>
        [Range(1000, 30000)]
        public int ConnectionTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 消息超时（毫秒）
        /// </summary>
        [Range(1000, 60000)]
        public int MessageTimeoutMs { get; set; } = 10000;
    }
}
