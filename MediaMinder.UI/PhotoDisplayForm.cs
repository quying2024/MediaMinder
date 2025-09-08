using MediaMinder.Common;
using System.Drawing.Imaging;

namespace MediaMinder.UI
{
    /// <summary>
    /// ç…§ç‰‡å±•ç¤ºçª—ä½“
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
            
            // åˆå§‹åŒ–è®¾ç½®
            _settings = new CommunicationSettings();
            _communicationService = new NamedPipeClient(_settings);
            _photosDirectory = @"C:\ProgramData\MediaMinder\Photos";

            // è®¾ç½®çª—ä½“å±žæ€§
            this.Text = "MediaMinder - ç…§ç‰‡å±•ç¤º";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(600, 400);

            // è®¾ç½®çª—ä½“å›¾æ ‡ï¼ˆå¦‚æžœæœ‰çš„è¯ï¼‰
            try
            {
                // this.Icon = new Icon("icon.ico");
            }
            catch
            {
                // å¿½ç•¥å›¾æ ‡åŠ è½½é”™è¯¯
            }

            // åˆå§‹åŒ–UI
            InitializePhotoDisplay();
            SetupCommunication();
        }

        private void InitializePhotoDisplay()
        {
            // åˆ›å»ºä¸»é¢æ¿
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(10)
            };

            // è®¾ç½®åˆ—å’Œè¡Œçš„å¤§å°
            for (int i = 0; i < 3; i++)
            {
                mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            }
            for (int i = 0; i < 2; i++)
            {
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            }

            // åˆ›å»ºå›¾ç‰‡æ¡†
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

                // æ·»åŠ åŠ è½½æ ‡ç­¾
                var loadingLabel = new Label
                {
                    Text = "ç­‰å¾…ç…§ç‰‡...",
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

            // å¯åŠ¨ç…§ç‰‡åˆ·æ–°ä»»åŠ¡
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
                System.Diagnostics.Debug.WriteLine($"å¤„ç†æœåŠ¡æ¶ˆæ¯æ—¶å‘ç”Ÿé”™è¯¯: {ex.Message}");
            }
        }

        private void OnConnectionStatusChanged(object sender, bool isConnected)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<object, bool>(OnConnectionStatusChanged), sender, isConnected);
                return;
            }

            this.Text = isConnected ? "MediaMinder - ç…§ç‰‡å±•ç¤º (å·²è¿žæŽ¥)" : "MediaMinder - ç…§ç‰‡å±•ç¤º (æœªè¿žæŽ¥)";
        }

        private void HandleNewPhotosMessage(IPCMessage message)
        {
            try
            {
                // å¦‚æžœæ˜¯æ•°å­—ï¼Œè¡¨ç¤ºæ–°ç…§ç‰‡æ•°é‡
                if (int.TryParse(message.Data, out int count))
                {
                    // åˆ·æ–°ç…§ç‰‡æ˜¾ç¤º
                    _ = Task.Run(async () => await RefreshPhotosAsync(_refreshCancellationTokenSource.Token));
                }
                // å¦‚æžœæ˜¯PhotoInfoå¯¹è±¡
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
                System.Diagnostics.Debug.WriteLine($"å¤„ç†æ–°ç…§ç‰‡æ¶ˆæ¯æ—¶å‘ç”Ÿé”™è¯¯: {ex.Message}");
            }
        }

        private void HandleCameraEventMessage(IPCMessage message)
        {
            try
            {
                var cameraEvent = message.GetData<dynamic>();
                if (cameraEvent != null)
                {
                    // å¯ä»¥åœ¨è¿™é‡Œå¤„ç†ç›¸æœºäº‹ä»¶ï¼Œæ¯”å¦‚æ˜¾ç¤ºçŠ¶æ€ä¿¡æ¯
                    System.Diagnostics.Debug.WriteLine($"ç›¸æœºäº‹ä»¶: {cameraEvent}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"å¤„ç†ç›¸æœºäº‹ä»¶æ¶ˆæ¯æ—¶å‘ç”Ÿé”™è¯¯: {ex.Message}");
            }
        }

        private void HandleStatusUpdateMessage(IPCMessage message)
        {
            try
            {
                // å¯ä»¥åœ¨è¿™é‡Œå¤„ç†çŠ¶æ€æ›´æ–°æ¶ˆæ¯
                System.Diagnostics.Debug.WriteLine($"çŠ¶æ€æ›´æ–°: {message.Data}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"å¤„ç†çŠ¶æ€æ›´æ–°æ¶ˆæ¯æ—¶å‘ç”Ÿé”™è¯¯: {ex.Message}");
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
                        System.Diagnostics.Debug.WriteLine($"å¤„ç†æ–‡ä»¶ {file} æ—¶å‘ç”Ÿé”™è¯¯: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"åˆ·æ–°ç…§ç‰‡æ—¶å‘ç”Ÿé”™è¯¯: {ex.Message}");
            }
        }

        private void UpdatePhotoDisplay()
        {
            try
            {
                for (int i = 0; i < _pictureBoxes.Count; i++)
                {
                    var pictureBox = _pictureBoxes[i];
                    
                    // æ¸…é™¤çŽ°æœ‰å›¾ç‰‡
                    pictureBox.Image?.Dispose();
                    pictureBox.Image = null;

                    // æ¸…é™¤åŠ è½½æ ‡ç­¾
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
                        // æ˜¾ç¤ºç©ºçŠ¶æ€
                        var emptyLabel = new Label
                        {
                            Text = "æ— ç…§ç‰‡",
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
                System.Diagnostics.Debug.WriteLine($"æ›´æ–°ç…§ç‰‡æ˜¾ç¤ºæ—¶å‘ç”Ÿé”™è¯¯: {ex.Message}");
            }
        }

        private async void LoadImageAsync(string imagePath, PictureBox pictureBox)
        {
            try
            {
                // åœ¨åŽå°çº¿ç¨‹åŠ è½½å›¾ç‰‡
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

                // åœ¨UIçº¿ç¨‹æ›´æ–°æ˜¾ç¤º
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
                                Text = "åŠ è½½å¤±è´¥",
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
                            Text = "åŠ è½½å¤±è´¥",
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
                System.Diagnostics.Debug.WriteLine($"åŠ è½½å›¾ç‰‡å¤±è´¥: {imagePath}, é”™è¯¯: {ex.Message}");
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

                // é‡Šæ”¾æ‰€æœ‰å›¾ç‰‡èµ„æº
                foreach (var pictureBox in _pictureBoxes)
                {
                    pictureBox.Image?.Dispose();
                }

                // é‡Šæ”¾é€šä¿¡æœåŠ¡
                _communicationService?.Dispose();

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
