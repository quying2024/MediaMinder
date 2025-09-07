using MediaMinder.Common;
using System.Drawing.Imaging;

namespace MediaMinder.UI
{
    /// <summary>
    /// 照片展示窗体
    /// </summary>
    public partial class PhotoDisplayForm : Form
    {
        private readonly ICommunicationService _communicationService;
        private readonly CommunicationSettings _settings;
        private readonly List<PhotoInfo> _photos = new();
        private readonly List<PictureBox> _pictureBoxes = new();
        private readonly CancellationTokenSource _refreshCancellationTokenSource = new();
        private readonly string _photosDirectory;
        private bool _isDisposed;

        public PhotoDisplayForm()
        {
            InitializeComponent();
            
            // 初始化设置
            _settings = new CommunicationSettings();
            _communicationService = new NamedPipeClient(_settings);
            _photosDirectory = @"C:\ProgramData\MediaMinder\G16";

            // 设置窗体属性
            this.Text = "MediaMinder - 照片展示";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(600, 400);

            // 设置窗体图标（如果有的话）
            try
            {
                // this.Icon = new Icon("icon.ico");
            }
            catch
            {
                // 忽略图标加载错误
            }

            // 初始化UI
            InitializePhotoDisplay();
            SetupCommunication();
        }

        private void InitializePhotoDisplay()
        {
            // 创建主面板
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(10)
            };

            // 设置列和行的大小
            for (int i = 0; i < 3; i++)
            {
                mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            }
            for (int i = 0; i < 2; i++)
            {
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            }

            // 创建图片框
            for (int i = 0; i < 6; i++)
            {
                var pictureBox = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.LightGray,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(5)
                };

                // 添加加载标签
                var loadingLabel = new Label
                {
                    Text = "等待照片...",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    ForeColor = Color.Gray,
                    Font = new Font("Microsoft YaHei", 12, FontStyle.Regular)
                };

                pictureBox.Controls.Add(loadingLabel);
                _pictureBoxes.Add(pictureBox);
                mainPanel.Controls.Add(pictureBox);
            }

            this.Controls.Add(mainPanel);

            // 启动照片刷新任务
            _ = Task.Run(async () => await RefreshPhotosAsync(_refreshCancellationTokenSource.Token));
        }

        private void SetupCommunication()
        {
            _communicationService.MessageReceived += OnServiceMessageReceived;
            _communicationService.ConnectionStatusChanged += OnConnectionStatusChanged;
            _communicationService.StartListening(OnServiceMessageReceivedHandler);
        }

        private void OnServiceMessageReceived(object sender, IPCMessage message)
        {
            OnServiceMessageReceivedHandler(message);
        }

        private void OnServiceMessageReceivedHandler(IPCMessage message)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<IPCMessage>(OnServiceMessageReceivedHandler), message);
                return;
            }

            try
            {
                switch (message.Type)
                {
                    case MessageType.NewPhotosAvailable:
                        HandleNewPhotosMessage(message);
                        break;
                    case MessageType.CameraEvent:
                        HandleCameraEventMessage(message);
                        break;
                    case MessageType.StatusUpdate:
                        HandleStatusUpdateMessage(message);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理服务消息时发生错误: {ex.Message}");
            }
        }

        private void OnConnectionStatusChanged(object sender, bool isConnected)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<object, bool>(OnConnectionStatusChanged), sender, isConnected);
                return;
            }

            this.Text = isConnected ? "MediaMinder - 照片展示 (已连接)" : "MediaMinder - 照片展示 (未连接)";
        }

        private void HandleNewPhotosMessage(IPCMessage message)
        {
            try
            {
                // 如果是数字，表示新照片数量
                if (int.TryParse(message.Data, out int count))
                {
                    // 刷新照片显示
                    _ = Task.Run(async () => await RefreshPhotosAsync(_refreshCancellationTokenSource.Token));
                }
                // 如果是PhotoInfo对象
                else
                {
                    var photoInfo = message.GetData<PhotoInfo>();
                    if (photoInfo != null)
                    {
                        _photos.Add(photoInfo);
                        UpdatePhotoDisplay();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理新照片消息时发生错误: {ex.Message}");
            }
        }

        private void HandleCameraEventMessage(IPCMessage message)
        {
            try
            {
                var cameraEvent = message.GetData<dynamic>();
                if (cameraEvent != null)
                {
                    // 可以在这里处理相机事件，比如显示状态信息
                    System.Diagnostics.Debug.WriteLine($"相机事件: {cameraEvent}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理相机事件消息时发生错误: {ex.Message}");
            }
        }

        private void HandleStatusUpdateMessage(IPCMessage message)
        {
            try
            {
                // 可以在这里处理状态更新消息
                System.Diagnostics.Debug.WriteLine($"状态更新: {message.Data}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理状态更新消息时发生错误: {ex.Message}");
            }
        }

        private async Task RefreshPhotosAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!Directory.Exists(_photosDirectory))
                    return;

                var photoFiles = Directory.GetFiles(_photosDirectory, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(file => IsImageFile(file))
                    .OrderByDescending(file => File.GetCreationTime(file))
                    .Take(6)
                    .ToArray();

                var newPhotos = new List<PhotoInfo>();
                foreach (var file in photoFiles)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var fileInfo = new FileInfo(file);
                        newPhotos.Add(new PhotoInfo
                        {
                            FileName = fileInfo.Name,
                            FullPath = fileInfo.FullName,
                            FileSize = fileInfo.Length,
                            CreatedTime = fileInfo.CreationTime,
                            ModifiedTime = fileInfo.LastWriteTime,
                            Extension = fileInfo.Extension.ToLowerInvariant(),
                            IsNew = false,
                            SourceDevice = "Canon G16"
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"处理文件 {file} 时发生错误: {ex.Message}");
                    }
                }

                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        _photos.Clear();
                        _photos.AddRange(newPhotos);
                        UpdatePhotoDisplay();
                    }));
                }
                else
                {
                    _photos.Clear();
                    _photos.AddRange(newPhotos);
                    UpdatePhotoDisplay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新照片时发生错误: {ex.Message}");
            }
        }

        private void UpdatePhotoDisplay()
        {
            try
            {
                for (int i = 0; i < _pictureBoxes.Count; i++)
                {
                    var pictureBox = _pictureBoxes[i];
                    
                    // 清除现有图片
                    pictureBox.Image?.Dispose();
                    pictureBox.Image = null;

                    // 清除加载标签
                    foreach (Control control in pictureBox.Controls.OfType<Label>().ToList())
                    {
                        control.Dispose();
                    }

                    if (i < _photos.Count)
                    {
                        var photo = _photos[i];
                        LoadImageAsync(photo.FullPath, pictureBox);
                    }
                    else
                    {
                        // 显示空状态
                        var emptyLabel = new Label
                        {
                            Text = "无照片",
                            TextAlign = ContentAlignment.MiddleCenter,
                            Dock = DockStyle.Fill,
                            ForeColor = Color.Gray,
                            Font = new Font("Microsoft YaHei", 12, FontStyle.Regular)
                        };
                        pictureBox.Controls.Add(emptyLabel);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新照片显示时发生错误: {ex.Message}");
            }
        }

        private async void LoadImageAsync(string imagePath, PictureBox pictureBox)
        {
            try
            {
                // 在后台线程加载图片
                var image = await Task.Run(() =>
                {
                    try
                    {
                        using var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
                        return Image.FromStream(fs);
                    }
                    catch
                    {
                        return null;
                    }
                });

                // 在UI线程更新显示
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        if (image != null)
                        {
                            pictureBox.Image = image;
                        }
                        else
                        {
                            var errorLabel = new Label
                            {
                                Text = "加载失败",
                                TextAlign = ContentAlignment.MiddleCenter,
                                Dock = DockStyle.Fill,
                                ForeColor = Color.Red,
                                Font = new Font("Microsoft YaHei", 10, FontStyle.Regular)
                            };
                            pictureBox.Controls.Add(errorLabel);
                        }
                    }));
                }
                else
                {
                    if (image != null)
                    {
                        pictureBox.Image = image;
                    }
                    else
                    {
                        var errorLabel = new Label
                        {
                            Text = "加载失败",
                            TextAlign = ContentAlignment.MiddleCenter,
                            Dock = DockStyle.Fill,
                            ForeColor = Color.Red,
                            Font = new Font("Microsoft YaHei", 10, FontStyle.Regular)
                        };
                        pictureBox.Controls.Add(errorLabel);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载图片失败: {imagePath}, 错误: {ex.Message}");
            }
        }

        private static bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || 
                   extension == ".gif" || extension == ".bmp" || extension == ".tiff" || 
                   extension == ".raw" || extension == ".cr2";
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                _refreshCancellationTokenSource?.Cancel();
                _refreshCancellationTokenSource?.Dispose();

                // 释放所有图片资源
                foreach (var pictureBox in _pictureBoxes)
                {
                    pictureBox.Image?.Dispose();
                }

                // 释放通信服务
                _communicationService?.Dispose();

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
