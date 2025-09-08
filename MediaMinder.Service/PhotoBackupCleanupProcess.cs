using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MediaMinder.Service
{
    /// <summary>
    /// 照片备份清理进程启动器
    /// 启动独立进程执行备份和清理操作，避免文件句柄占用问题
    /// </summary>
    public class PhotoBackupCleanupProcess
    {
        private readonly string _photosDirectory;
        private readonly ILogger<PhotoBackupCleanupProcess> _logger;

        public PhotoBackupCleanupProcess(string photosDirectory, ILogger<PhotoBackupCleanupProcess> logger)
        {
            _photosDirectory = photosDirectory;
            _logger = logger;
        }

        /// <summary>
        /// 启动独立的备份清理进程
        /// </summary>
        public async Task<bool> StartBackupCleanupProcessAsync()
        {
            try
            {
                _logger.LogInformation("启动独立的照片备份清理进程");

                // 创建临时批处理文件
                var batchFile = await CreateBackupCleanupBatchFileAsync();
                if (batchFile == null)
                {
                    _logger.LogError("创建备份清理批处理文件失败");
                    return false;
                }

                // 启动独立进程
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = batchFile,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };

                process.Start();
                _logger.LogInformation("备份清理进程已启动，PID: {ProcessId}", process.Id);

                // 等待进程完成（最多等待5分钟）
                var completed = process.WaitForExit(300000); // 5分钟超时

                if (completed)
                {
                    var exitCode = process.ExitCode;
                    _logger.LogInformation("备份清理进程完成，退出代码: {ExitCode}", exitCode);
                    
                    // 清理临时文件
                    try
                    {
                        File.Delete(batchFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除临时批处理文件失败: {BatchFile}", batchFile);
                    }

                    return exitCode == 0;
                }
                else
                {
                    _logger.LogError("备份清理进程超时，强制终止");
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "终止备份清理进程失败");
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动备份清理进程时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 创建备份清理批处理文件
        /// </summary>
        private async Task<string?> CreateBackupCleanupBatchFileAsync()
        {
            try
            {
                var tempDir = Path.GetTempPath();
                var batchFileName = $"MediaMinder_BackupCleanup_{DateTime.Now:yyyyMMdd_HHmmss}.bat";
                var batchFilePath = Path.Combine(tempDir, batchFileName);

                // 创建C#控制台程序代码
                var csharpCode = GenerateBackupCleanupCode();

                // 创建临时C#文件
                var csFileName = $"MediaMinder_BackupCleanup_{DateTime.Now:yyyyMMdd_HHmmss}.cs";
                var csFilePath = Path.Combine(tempDir, csFileName);

                await File.WriteAllTextAsync(csFilePath, csharpCode);

                // 创建批处理文件
                var batchContent = $@"
@echo off
echo 开始执行照片备份和清理操作...

REM 编译C#程序
csc /out:""{tempDir}MediaMinder_BackupCleanup.exe"" ""{csFilePath}""

if %ERRORLEVEL% neq 0 (
    echo 编译失败
    exit /b 1
)

REM 执行备份清理程序
""{tempDir}MediaMinder_BackupCleanup.exe"" ""{_photosDirectory}""

REM 保存退出代码
set EXIT_CODE=%ERRORLEVEL%

REM 清理临时文件
del ""{csFilePath}""
del ""{tempDir}MediaMinder_BackupCleanup.exe""
del ""%~f0""

exit /b %EXIT_CODE%
";

                await File.WriteAllTextAsync(batchFilePath, batchContent);
                _logger.LogInformation("创建备份清理批处理文件: {BatchFile}", batchFilePath);

                return batchFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建备份清理批处理文件时发生错误");
                return null;
            }
        }

        /// <summary>
        /// 生成备份清理C#代码
        /// </summary>
        private string GenerateBackupCleanupCode()
        {
            return @"
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                Console.WriteLine(""错误: 未指定照片目录路径"");
                return 1;
            }

            var photosDirectory = args[0];
            Console.WriteLine($""开始处理照片目录: {photosDirectory}"");

            // 检查目录是否存在
            if (!Directory.Exists(photosDirectory))
            {
                Console.WriteLine($""照片目录不存在: {photosDirectory}"");
                return 0;
            }

            // 获取根目录中的所有图片文件
            var imageExtensions = new[] { "".jpg"", "".jpeg"", "".png"", "".gif"", "".bmp"", "".tiff"", "".raw"", "".cr2"", "".arw"", "".nef"", "".dng"" };
            var imageFiles = Directory.GetFiles(photosDirectory, ""*.*"", SearchOption.TopDirectoryOnly)
                .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .ToList();

            if (imageFiles.Count == 0)
            {
                Console.WriteLine(""根目录中没有图片文件，跳过备份"");
            }
            else
            {
                Console.WriteLine($""找到 {imageFiles.Count} 个图片文件需要备份"");

                // 创建时间戳命名的子目录
                var timestamp = DateTime.Now.ToString(""yyyyMMdd_HHmmss"");
                var backupDirectory = Path.Combine(photosDirectory, $""Backup_{timestamp}"");
                Directory.CreateDirectory(backupDirectory);
                Console.WriteLine($""创建备份目录: {backupDirectory}"");

                // 复制文件到备份目录
                var successCount = 0;
                foreach (var sourceFile in imageFiles)
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
                            fileName = $""{nameWithoutExt}_{counter}{extension}"";
                            targetFile = Path.Combine(backupDirectory, fileName);
                            counter++;
                        }

                        File.Copy(sourceFile, targetFile, false);
                        successCount++;
                        Console.WriteLine($""复制文件: {Path.GetFileName(sourceFile)}"");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($""复制文件失败: {sourceFile}, 错误: {ex.Message}"");
                    }
                }

                Console.WriteLine($""文件复制完成: {successCount}/{imageFiles.Count}"");

                // 验证复制结果
                var verificationSuccess = true;
                foreach (var sourceFile in imageFiles)
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
                            
                            while (counter <= 100)
                            {
                                var numberedFileName = $""{nameWithoutExt}_{counter}{extension}"";
                                targetFile = Path.Combine(backupDirectory, numberedFileName);
                                
                                if (File.Exists(targetFile))
                                    break;
                                
                                counter++;
                            }
                        }

                        if (!File.Exists(targetFile))
                        {
                            Console.WriteLine($""备份文件不存在: {targetFile}"");
                            verificationSuccess = false;
                            continue;
                        }

                        // 验证文件大小
                        var sourceFileInfo = new FileInfo(sourceFile);
                        var targetFileInfo = new FileInfo(targetFile);

                        if (sourceFileInfo.Length != targetFileInfo.Length)
                        {
                            Console.WriteLine($""文件大小不匹配: {sourceFile} ({sourceFileInfo.Length}) vs {targetFile} ({targetFileInfo.Length})"");
                            verificationSuccess = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($""验证文件时发生错误: {sourceFile}, 错误: {ex.Message}"");
                        verificationSuccess = false;
                    }
                }

                if (!verificationSuccess)
                {
                    Console.WriteLine(""备份验证失败"");
                    return 1;
                }

                Console.WriteLine(""备份验证成功"");

                // 删除根目录中的图片文件
                var deleteSuccessCount = 0;
                foreach (var imageFile in imageFiles)
                {
                    try
                    {
                        File.Delete(imageFile);
                        deleteSuccessCount++;
                        Console.WriteLine($""删除文件: {Path.GetFileName(imageFile)}"");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($""删除文件失败，可能被其他程序占用: {imageFile}, 错误: {ex.Message}"");
                        // 按照要求，遇到被占用的文件直接跳过，不重试
                    }
                }

                Console.WriteLine($""根目录图片删除完成: {deleteSuccessCount}/{imageFiles.Count}"");
            }

            // 清理过期子目录（创建时间超过1周）
            var oneWeekAgo = DateTime.Now.AddDays(-7);
            var subdirectories = Directory.GetDirectories(photosDirectory);
            var deletedCount = 0;

            foreach (var subdirectory in subdirectories)
            {
                try
                {
                    var directoryInfo = new DirectoryInfo(subdirectory);
                    if (directoryInfo.CreationTime < oneWeekAgo)
                    {
                        Console.WriteLine($""删除过期目录: {subdirectory} (创建时间: {directoryInfo.CreationTime})"");
                        Directory.Delete(subdirectory, true);
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($""删除过期目录失败: {subdirectory}, 错误: {ex.Message}"");
                }
            }

            if (deletedCount > 0)
            {
                Console.WriteLine($""清理过期目录完成，删除了 {deletedCount} 个目录"");
            }
            else
            {
                Console.WriteLine(""没有找到需要清理的过期目录"");
            }

            Console.WriteLine(""照片备份和清理操作完成"");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($""执行备份和清理操作时发生错误: {ex.Message}"");
            return 1;
        }
    }
}";
        }
    }
}
