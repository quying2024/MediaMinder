using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaMinder.Service
{
    /// <summary>
    /// 照片备份和清理服务
    /// 独立进程处理照片目录的备份和清理工作
    /// </summary>
    public class PhotoBackupCleanupService
    {
        private readonly string _photosDirectory;
        private readonly ILogger<PhotoBackupCleanupService> _logger;

        public PhotoBackupCleanupService(string photosDirectory, ILogger<PhotoBackupCleanupService> logger)
        {
            _photosDirectory = photosDirectory;
            _logger = logger;
        }

        /// <summary>
        /// 执行备份和清理操作
        /// </summary>
        public async Task<bool> ExecuteBackupAndCleanupAsync()
        {
            try
            {
                _logger.LogInformation("开始执行照片备份和清理操作");

                // 1. 检查下载目录是否存在
                if (!Directory.Exists(_photosDirectory))
                {
                    _logger.LogWarning("照片目录不存在: {PhotosDirectory}", _photosDirectory);
                    return true; // 目录不存在，无需处理
                }

                // 2. 获取根目录中的所有图片文件
                var imageFiles = GetImageFilesInRootDirectory();
                if (imageFiles.Count == 0)
                {
                    _logger.LogInformation("根目录中没有图片文件，跳过备份");
                }
                else
                {
                    _logger.LogInformation("找到 {Count} 个图片文件需要备份", imageFiles.Count);

                    // 3. 创建时间戳命名的子目录
                    var backupDirectory = await CreateTimestampedBackupDirectoryAsync();
                    if (backupDirectory == null)
                    {
                        _logger.LogError("创建备份目录失败");
                        return false;
                    }

                    // 4. 复制文件到备份目录
                    var copySuccess = await CopyFilesToBackupDirectoryAsync(imageFiles, backupDirectory);
                    if (!copySuccess)
                    {
                        _logger.LogError("文件复制失败");
                        return false;
                    }

                    // 5. 验证复制结果
                    var verificationSuccess = await VerifyBackupIntegrityAsync(imageFiles, backupDirectory);
                    if (!verificationSuccess)
                    {
                        _logger.LogError("备份验证失败");
                        return false;
                    }

                    // 6. 删除根目录中的图片文件
                    await DeleteRootDirectoryImagesAsync(imageFiles);
                }

                // 7. 清理过期子目录
                await CleanupExpiredDirectoriesAsync();

                _logger.LogInformation("照片备份和清理操作完成");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行备份和清理操作时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 获取根目录中的所有图片文件
        /// </summary>
        private List<string> GetImageFilesInRootDirectory()
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".raw", ".cr2", ".arw", ".nef", ".dng" };
            
            return Directory.GetFiles(_photosDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .ToList();
        }

        /// <summary>
        /// 创建时间戳命名的备份目录
        /// </summary>
        private async Task<string?> CreateTimestampedBackupDirectoryAsync()
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupDirectory = Path.Combine(_photosDirectory, $"Backup_{timestamp}");

                Directory.CreateDirectory(backupDirectory);
                _logger.LogInformation("创建备份目录: {BackupDirectory}", backupDirectory);

                return backupDirectory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建备份目录失败");
                return null;
            }
        }

        /// <summary>
        /// 复制文件到备份目录
        /// </summary>
        private async Task<bool> CopyFilesToBackupDirectoryAsync(List<string> sourceFiles, string backupDirectory)
        {
            try
            {
                var successCount = 0;
                var totalCount = sourceFiles.Count;

                foreach (var sourceFile in sourceFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(sourceFile);
                        var targetFile = Path.Combine(backupDirectory, fileName);

                        // 如果目标文件已存在，添加序号
                        var counter = 1;
                        while (File.Exists(targetFile))
                        {
                            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                            var extension = Path.GetExtension(fileName);
                            fileName = $"{nameWithoutExt}_{counter}{extension}";
                            targetFile = Path.Combine(backupDirectory, fileName);
                            counter++;
                        }

                        File.Copy(sourceFile, targetFile, false);
                        successCount++;

                        _logger.LogDebug("复制文件: {SourceFile} -> {TargetFile}", sourceFile, targetFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "复制文件失败: {SourceFile}", sourceFile);
                    }
                }

                _logger.LogInformation("文件复制完成: {SuccessCount}/{TotalCount}", successCount, totalCount);
                return successCount == totalCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制文件到备份目录时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 验证备份完整性
        /// </summary>
        private async Task<bool> VerifyBackupIntegrityAsync(List<string> sourceFiles, string backupDirectory)
        {
            try
            {
                var verificationSuccess = true;

                foreach (var sourceFile in sourceFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(sourceFile);
                        var targetFile = Path.Combine(backupDirectory, fileName);

                        // 如果目标文件不存在，尝试查找带序号的文件
                        if (!File.Exists(targetFile))
                        {
                            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                            var extension = Path.GetExtension(fileName);
                            var counter = 1;
                            
                            while (counter <= 100) // 最多查找100个序号
                            {
                                var numberedFileName = $"{nameWithoutExt}_{counter}{extension}";
                                targetFile = Path.Combine(backupDirectory, numberedFileName);
                                
                                if (File.Exists(targetFile))
                                    break;
                                
                                counter++;
                            }
                        }

                        if (!File.Exists(targetFile))
                        {
                            _logger.LogError("备份文件不存在: {TargetFile}", targetFile);
                            verificationSuccess = false;
                            continue;
                        }

                        // 验证文件大小
                        var sourceFileInfo = new FileInfo(sourceFile);
                        var targetFileInfo = new FileInfo(targetFile);

                        if (sourceFileInfo.Length != targetFileInfo.Length)
                        {
                            _logger.LogError("文件大小不匹配: {SourceFile} ({SourceSize}) vs {TargetFile} ({TargetSize})", 
                                sourceFile, sourceFileInfo.Length, targetFile, targetFileInfo.Length);
                            verificationSuccess = false;
                        }
                        else
                        {
                            _logger.LogDebug("文件验证通过: {FileName} ({FileSize} bytes)", fileName, sourceFileInfo.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "验证文件时发生错误: {SourceFile}", sourceFile);
                        verificationSuccess = false;
                    }
                }

                if (verificationSuccess)
                {
                    _logger.LogInformation("备份验证成功");
                }
                else
                {
                    _logger.LogError("备份验证失败");
                }

                return verificationSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证备份完整性时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 删除根目录中的图片文件
        /// </summary>
        private async Task DeleteRootDirectoryImagesAsync(List<string> imageFiles)
        {
            try
            {
                var successCount = 0;
                var totalCount = imageFiles.Count;

                foreach (var imageFile in imageFiles)
                {
                    try
                    {
                        File.Delete(imageFile);
                        successCount++;
                        _logger.LogDebug("删除文件: {ImageFile}", imageFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除文件失败，可能被其他程序占用: {ImageFile}", imageFile);
                        // 按照要求，遇到被占用的文件直接跳过，不重试
                    }
                }

                _logger.LogInformation("根目录图片删除完成: {SuccessCount}/{TotalCount}", successCount, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除根目录图片时发生错误");
            }
        }

        /// <summary>
        /// 清理过期子目录（创建时间超过1周）
        /// </summary>
        private async Task CleanupExpiredDirectoriesAsync()
        {
            try
            {
                var oneWeekAgo = DateTime.Now.AddDays(-7);
                var subdirectories = Directory.GetDirectories(_photosDirectory);

                var deletedCount = 0;

                foreach (var subdirectory in subdirectories)
                {
                    try
                    {
                        var directoryInfo = new DirectoryInfo(subdirectory);
                        if (directoryInfo.CreationTime < oneWeekAgo)
                        {
                            _logger.LogInformation("删除过期目录: {Directory} (创建时间: {CreationTime})", 
                                subdirectory, directoryInfo.CreationTime);

                            Directory.Delete(subdirectory, true); // 强制删除，包括所有文件
                            deletedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "删除过期目录失败: {Directory}", subdirectory);
                    }
                }

                if (deletedCount > 0)
                {
                    _logger.LogInformation("清理过期目录完成，删除了 {Count} 个目录", deletedCount);
                }
                else
                {
                    _logger.LogInformation("没有找到需要清理的过期目录");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期目录时发生错误");
            }
        }
    }
}
