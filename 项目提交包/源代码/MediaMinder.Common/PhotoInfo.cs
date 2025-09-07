using System;

namespace MediaMinder.Common
{
    /// <summary>
    /// 照片信息数据模型
    /// </summary>
    public class PhotoInfo
    {
        /// <summary>
        /// 照片文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 完整文件路径
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime ModifiedTime { get; set; }

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// 缩略图路径（如果存在）
        /// </summary>
        public string ThumbnailPath { get; set; }

        /// <summary>
        /// 是否为新照片
        /// </summary>
        public bool IsNew { get; set; }

        /// <summary>
        /// 来源设备
        /// </summary>
        public string SourceDevice { get; set; } = string.Empty;
    }
}
