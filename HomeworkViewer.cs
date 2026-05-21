#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace HomeworkViewer
{
    public class HomeworkViewer : Form
    {
        // ====================== 常量 ======================
        private Timer _resizeTimer;
        private bool _isWin7;
        private float _clockAlpha = 0f; // 时钟透明度 0-255
        private bool _isEditingFinishing = false;
        private readonly Size VIRTUAL_SIZE = new Size(1200, 675);
        private const int BTN_SQUARE_SIZE = 46;
        private readonly Point FULLSCREEN_BTN_POS = new Point(1200 - BTN_SQUARE_SIZE - 10, 13);
        private readonly Point HISTORY_BTN_POS = new Point(1200 - BTN_SQUARE_SIZE - 10 - BTN_SQUARE_SIZE - 10, 13);
        private readonly Point EDIT_BTN_POS = new Point(1200 - BTN_SQUARE_SIZE - 10 - BTN_SQUARE_SIZE - 10 - BTN_SQUARE_SIZE - 10, 13);
        private readonly Point ROTATE_BTN_POS = new Point(1200 - BTN_SQUARE_SIZE - 10 - BTN_SQUARE_SIZE - 10 - BTN_SQUARE_SIZE - 10 - BTN_SQUARE_SIZE - 10, 13);
        private Point SETTINGS_BTN_POS;
        private readonly Rectangle GRID_AREA = new Rectangle(0, 47, 1200, 631);
        private const int GRID_PADDING = 20, ROUND_RADIUS = 5;

        private const int BAR_HEIGHT = 46, BAR_Y = 0, BAR_PADDING = 20;

        // 顶部下拉栏
        private Panel _drawerPanel;
        private bool _drawerOpen = false;
        private const int MAX_DRAWER_HEIGHT_RATIO = 3;

        // 自定义字体对象（用于卡片内容）
        private Font _customFontChinese, _customFontEnglish, _customFontNumber;
        private Font _customContentFontChinese, _customContentFontEnglish, _customContentFontNumber;

        // 动画
        private Timer _animationTimer;
        private List<Animation> _animations = new List<Animation>();
        private Dictionary<int, Animation> _cardAnimations = new Dictionary<int, Animation>();
        private Dictionary<int, float> _flyOutProgress = new Dictionary<int, float>();
        private Dictionary<int, float> _flyInOffsets = new Dictionary<int, float>();
        private int _flyInAnimationsRemaining = 0;
        private string _oldRotationText = "";

        // 虚线卡片独立动画
        private float _virtualCardOffset = 0f;
        private Animation _virtualCardAnim = null;
        private float _virtualCardFlyOutOffset = 0f;
        private Animation _virtualCardFlyOutAnim = null;
        private bool _showVirtualAddCardPage = false;

        // 淡入动画（用于添加卡片）
        private Dictionary<int, float> _fadeInProgress = new Dictionary<int, float>();

        // 淡出动画（添加卡片时虚线卡片消失）
        private Animation _addCardFadeOutAnim;
        private float _addCardFadeOutAlpha = 0f;
        private bool _lastAddCardHover = false;

        // 轮播滑动动画
        private bool _isSliding = false;
        private float _slideProgress = 0;
        private int _slidePhase = 0;
        private int _oldSlideIndex = -1;
        private int _newSlideIndex = -1;
        private string _oldSlideContent = "";
        private string _newSlideContent = "";

        // Mica/亚克力
        private bool _micaEnabled = false;
        private Image _backgroundImage = null;
        private Bitmap _cachedBackground;
        private TextureBrush _tileBrush;
        private ImageFillMode _cachedBackgroundMode;

        // 状态
        private bool fullscreen = false;
        private bool historyMode = false;
        private bool editMode = false;
        private bool rotationMode = false;
        private int rotationIndex = 0;
        private Timer rotationTimer;
        private const int rotationInterval = 10000;

        // 内联编辑
        private Control inlineEditControl;
        private int editingSubjectIndex = -1;
        private enum EditFieldType { None, Subject, DueTime }
        private EditFieldType currentEditType = EditFieldType.None;
        private bool _isCleaningUp = false;

        // 数据
        private HomeworkData homeworkData = new HomeworkData();
        private DateTime currentDate = DateTime.Now;
        private DateTime? historyDate = null;

        // 按钮矩形（全局，用于点击检测）
        private Rectangle rotateBtnRect, editBtnRect, historyBtnRect, fullscreenBtnRect, backBtnRect, leftArrowRect, rightArrowRect, settingsBtnRect, minimizeBtnRect, closeBtnRect, exportBtnRect;
        private Rectangle expandBtnRect, collapseBtnRect;
        private Rectangle manageBtnRect, addCardBtnRect;

        // 网格分页
        private List<Rectangle> gridRects = new List<Rectangle>();
        private int _currentPage = 0;
        private int TotalPages
        {
            get
            {
                int realPages = (int)Math.Ceiling(appConfig.CustomSubjects.Count / (double)CARDS_PER_PAGE);
                if (_isCardEditingMode && _showVirtualAddCardPage)
                    return realPages + 1;
                return realPages;
            }
        }
        private const int CARDS_PER_PAGE = 6;
        private Timer _autoPageTimer;
        private int _autoPageInterval = 10;
        private bool _isCardEditingMode = false;
        private Timer _shakeTimer;
        private float _shakeAngle = 0;
        private bool _shakeDirection = true;
        private float _shakeStep = 0.6f;
        private float _shakeMaxAngle = 0.5f;

        // 拖拽排序
        private bool _isDragging = false;
        private int _dragStartIndex = -1;
        private int _dragOverIndex = -1;
        private Point _dragStartPoint;
        private List<Rectangle> _deleteButtonRects = new List<Rectangle>();
        private Rectangle _virtualAddCardRect;

        // 字体
        private Font font12, font20, font24, font30, font22, font36, hintFont, buttonFont, fontSmall;

        // 颜色
        private Color TEXT_COLOR = Color.White;
        private readonly Brush RED_SEMI = new SolidBrush(Color.FromArgb(255, 255, 0, 0));
        private readonly Brush ORANGE_SEMI = new SolidBrush(Color.FromArgb(255, 255, 165, 0));
        private readonly Brush GREEN_SEMI = new SolidBrush(Color.FromArgb(255, 0, 255, 0));
        private readonly Brush BLUE_SEMI = new SolidBrush(Color.FromArgb(255, 0, 0, 255));
        private readonly Brush PURPLE_SEMI = new SolidBrush(Color.FromArgb(255, 128, 0, 128));
        private readonly Brush DARKORANGE_SEMI = new SolidBrush(Color.FromArgb(255, 255, 140, 0));

        // 缩放
        private float scaleFactor = 1.0f;
        private Point offset = Point.Empty;
        private bool _resizing = false;
        private bool _inSizeMove = false;

        // 图片资源
        private Image buttonImage, historyBtnImage, backBtnImage, leftArrowImage, rightArrowImage, manageCardImage, addCardImage;
        private Dictionary<string, Image> buttonIcons = new Dictionary<string, Image>();

        // 配置
        private AppConfig appConfig;
        private float[] fontScales = { 0.8f, 1.0f, 1.2f };
        private string[] currentSubjects;

        // 窗口状态保存
        private Size _savedClientSize;
        private FormWindowState _savedWindowState;

        // 按钮悬停/按下
        private Dictionary<string, bool> _buttonHover = new Dictionary<string, bool>();
        private Dictionary<string, bool> _buttonPressed = new Dictionary<string, bool>();
        private Dictionary<string, float> _buttonHoverProgress = new Dictionary<string, float>();
        private string _currentHoverKey = null;

        // 按钮按下标志
        private bool _settingsPressed = false, _rotatePressed = false, _editPressed = false, _historyPressed = false, _fullscreenPressed = false, _backPressed = false, _minimizePressed = false, _closePressed = false, _expandPressed = false, _collapsePressed = false, _exportPressed = false;
        private bool _managePressed = false, _addCardPressed = false;

        // 按钮栏动画
        private bool _isButtonBarAnimating = false;
        private float _buttonBarAnimProgress = 0f;
        private int _buttonBarAnimDirection = 0;
        private Animation _buttonBarAnimation = null;
        private List<Rectangle> _currentButtonRects = new List<Rectangle>();
        private List<string> _currentButtonKeys = new List<string>();

        // 收纳
        private bool _buttonsExpanded = false;

        // 下拉栏拖拽手势
        private bool _isDraggingDrawer = false;
        private int _dragDrawerStartY = 0;
        private int _dragDrawerStartHeight = 0;
        private const int DRAWER_DRAG_THRESHOLD = 30;

        // 轮播滑动翻页（触摸/鼠标拖拽）
        private bool _isSwiping = false;
        private int _swipeStartX = 0;
        private float _swipeOffset = 0f;
        private float _swipeStartOffset = 0f;

        // 模式切换下拉框（废弃）
        private ComboBox modeComboBox;

        // 时间提醒
        private Timer timeCheckTimer;
        private List<string> _activeEvenings = new List<string>();
        private List<string> _flashingEvenings = new List<string>();
        private List<string> _grayEvenings = new List<string>();
        private bool _previousFlashingState = false;

        // 闪烁
        private Timer flashTimer;
        private int flashStep = 0;
        private bool _debugFlashing = false;
        private DateTime _debugFlashStartTime;
        private DateTime flashStartTime;
        private const int FLASH_DURATION = 300;
        private const int FLASH_INTERVAL = 100;
        private float _laserOffset = 0f;

        // 滚动
        private Timer scrollTimer;
        private Dictionary<int, float> scrollOffsets = new Dictionary<int, float>();
        private Dictionary<int, bool> scrollPaused = new Dictionary<int, bool>();
        private Dictionary<int, DateTime> pauseStartTime = new Dictionary<int, DateTime>();
        private const int SCROLL_PAUSE_SECONDS = 3;

        // 背景效果
        private string _currentBackgroundEffect = "Mica";
        private bool _isWin10OrAbove = false;
        private bool _sizing = false;
        private int _slideDirection = 0; // 1=向右（左箭头），-1=向左（右箭头）

        // 行列调整
        private int[] _rowHeights = new int[2];
        private int[] _colWidths = new int[3];
        private bool _isResizing = false;
        private int _resizeTargetRow = -1, _resizeTargetCol = -1, _resizeStartX, _resizeStartY, _originalHeight, _originalWidth;

        private int _lastFlyDirection = 1;
        private bool _isFlyingIn = false;

        // 周末延续
        private DateTime GetEffectiveDate(DateTime date)
        {
            if (!appConfig.ExtendFridayHomeworkToWeekend) return date;
            if (date.DayOfWeek == DayOfWeek.Saturday) return date.AddDays(-1);
            if (date.DayOfWeek == DayOfWeek.Sunday) return date.AddDays(-2);
            return date;
        }

        public HomeworkViewer()
        {
            SETTINGS_BTN_POS = new Point(ROTATE_BTN_POS.X - BTN_SQUARE_SIZE - 10, 13);

            Text = "作业展板";
            this.Size = new Size(1200, 700);
            ClientSize = VIRTUAL_SIZE;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = true;
            MaximizeBox = true;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Color.Black;
            ResizeRedraw = false;

            appConfig = AppConfig.Load();
            if (appConfig.CustomSubjects == null || appConfig.CustomSubjects.Count == 0)
                appConfig.CustomSubjects = new List<string> { "语文", "数学", "英语", "物理", "化学", "生物" };
            ApplyFontSettings();
            UpdateSubjectsByMode();
            CalculateGridRects();
            LoadHomeworkData(currentDate);
            LoadImages();
            LoadBackgroundImage();

            rotationTimer = new Timer { Interval = rotationInterval };
            rotationTimer.Tick += (s, e) => { if (rotationMode) RotateNext(); };

            timeCheckTimer = new Timer { Interval = 1000 };
            timeCheckTimer.Tick += (s, e) => CheckEveningClassStates();
            timeCheckTimer.Start();

            flashTimer = new Timer { Interval = FLASH_INTERVAL };
            flashTimer.Tick += FlashTimer_Tick;

            scrollTimer = new Timer { Interval = 50 };
            scrollTimer.Tick += ScrollTimer_Tick;
            scrollTimer.Start();

            _animationTimer = new Timer { Interval = 16 };
            _animationTimer.Tick += (s, e) => {
                bool needRedraw = false;
                foreach (var anim in _animations.ToList())
                {
                    anim.Update();
                    needRedraw = true;
                }
                if (needRedraw) Invalidate();
                _animations.RemoveAll(a => !a.IsRunning);
            };
            _animationTimer.Start();

            InitAutoPageTimer();

            this.MouseClick += new MouseEventHandler(OnMouseClick);
            this.MouseDoubleClick += OnMouseDoubleClick;
            this.MouseDown += OnMouseDown;
            this.MouseUp += OnMouseUp;
            this.MouseLeave += OnMouseLeave;
            this.MouseMove += OnMouseMove;
            this.Resize += OnResize;
            this.Load += OnLoad;
            this.Activated += OnActivated;
            this.FormClosing += OnFormClosing;
            this.KeyDown += OnKeyDown;

            InitializeModeComboBox();

            // ** 在此处添加双缓冲和样式设置 **
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, false);

            CheckWindowsVersion();
            ApplyBackgroundEffect(appConfig.BackgroundEffect);
            CheckEveningClassStates();

            _rowHeights[0] = _rowHeights[1] = 0;
            _colWidths[0] = _colWidths[1] = _colWidths[2] = 0;

            ManagementHelper.CheckForUpdatesAsync(appConfig, (updated) => { if (updated) Invalidate(); });

            InitializeDrawerPanel();
        }

        private class BufferedPanel : Panel
        {
            public BufferedPanel()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.I)
            {
                using (var settingsForm = new SettingsForm(this))
                {
                    settingsForm.ShowDialog();
                }
                e.Handled = true;
            }
            else if ((e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down) &&
                     !_isCardEditingMode && !rotationMode && TotalPages > 1 && inlineEditControl == null)
            {
                int newPage;
                if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Up)
                    newPage = (_currentPage - 1 + TotalPages) % TotalPages;
                else
                    newPage = (_currentPage + 1) % TotalPages;
                int direction = (e.KeyCode == Keys.Left || e.KeyCode == Keys.Up) ? 1 : -1;
                SwitchToPage(newPage, direction);
                e.Handled = true;
            }
        }

        private void CheckWindowsVersion()
        {
            var os = Environment.OSVersion;
            _isWin10OrAbove = os.Version.Major >= 10;
        }

        private void LoadBackgroundImage()
        {
            if (_cachedBackground != null)
            {
                _cachedBackground.Dispose();
                _cachedBackground = null;
            }
            if (_tileBrush != null)
            {
                _tileBrush.Dispose();
                _tileBrush = null;
            }

            if (appConfig.UseBackgroundImage && !string.IsNullOrEmpty(appConfig.BackgroundImagePath))
            {
                string fullPath = appConfig.BackgroundImagePath;
                if (!Path.IsPathRooted(fullPath))
                {
                    fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fullPath);
                }
                if (File.Exists(fullPath))
                {
                    try
                    {
                        using (var originalImage = Image.FromFile(fullPath))
                        {
                            _cachedBackgroundMode = appConfig.BackgroundImageMode;
                            if (appConfig.BackgroundImageMode == ImageFillMode.Tile)
                            {
                                _tileBrush = new TextureBrush(originalImage, WrapMode.Tile);
                            }
                            else
                            {
                                _cachedBackground = GenerateBackgroundCache(originalImage);
                            }
                        }
                    }
                    catch { _cachedBackground = null; _tileBrush = null; }
                }
                else
                {
                    _cachedBackground = null;
                }
            }
        }

        private Bitmap GenerateBackgroundCache(Image original)
        {
            Size targetSize = VIRTUAL_SIZE;
            Bitmap cache = new Bitmap(targetSize.Width, targetSize.Height);
            using (Graphics g = Graphics.FromImage(cache))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.None;
                g.CompositingQuality = CompositingQuality.HighSpeed;

                switch (_cachedBackgroundMode)
                {
                    case ImageFillMode.Fill:
                        g.DrawImage(original, 0, 0, targetSize.Width, targetSize.Height);
                        break;
                    case ImageFillMode.Uniform:
                        float scaleU = Math.Min((float)targetSize.Width / original.Width, (float)targetSize.Height / original.Height);
                        int drawWU = (int)(original.Width * scaleU);
                        int drawHU = (int)(original.Height * scaleU);
                        int xU = (targetSize.Width - drawWU) / 2;
                        int yU = (targetSize.Height - drawHU) / 2;
                        g.DrawImage(original, xU, yU, drawWU, drawHU);
                        break;
                    case ImageFillMode.UniformToFill:
                        float scaleF = Math.Max((float)targetSize.Width / original.Width, (float)targetSize.Height / original.Height);
                        int drawWF = (int)(original.Width * scaleF);
                        int drawHF = (int)(original.Height * scaleF);
                        int xF = (targetSize.Width - drawWF) / 2;
                        int yF = (targetSize.Height - drawHF) / 2;
                        g.DrawImage(original, xF, yF, drawWF, drawHF);
                        break;
                    case ImageFillMode.Stretch:
                        g.DrawImage(original, 0, 0, targetSize.Width, targetSize.Height);
                        break;
                    case ImageFillMode.Center:
                        g.DrawImage(original, (targetSize.Width - original.Width) / 2, (targetSize.Height - original.Height) / 2, original.Width, original.Height);
                        break;
                    default:
                        g.DrawImage(original, 0, 0, targetSize.Width, targetSize.Height);
                        break;
                }
            }
            return cache;
        }

        public void ApplyBackgroundEffect(string effect)
        {
            _isWin7 = Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1;
            if (_isWin7)
            {
                EnableAeroGlass();
                return;
            }

            if (appConfig.UseBackgroundImage)
            {
                _micaEnabled = false;
                Invalidate();
                return;
            }
            try
            {
                switch (effect)
                {
                    case "Mica":
                        EnableMica();
                        break;
                    case "Acrylic":
                        EnableAcrylic();
                        break;
                    case "Aero":
                        EnableAero();
                        break;
                    default:
                        EnableMica();
                        break;
                }
            }
            catch { }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        private void EnableAeroGlass()
        {
            try
            {
                var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                DwmExtendFrameIntoClientArea(this.Handle, ref margins);
                _micaEnabled = false;
                
                // 根据设置调整背景色以增强玻璃反光
                if (appConfig.EnableGlassReflection)
                {
                    // 极低不透明度的黑色，让玻璃效果完全透出并显示高光
                    this.BackColor = Color.FromArgb(10, 0, 0, 0);
                }
                else
                {
                    this.BackColor = Color.Black;
                }
                Invalidate();
            }
            catch { }
        }

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_MICA = 1029;
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public uint AccentFlags;
            public uint GradientColor;
            public uint AnimationId;
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private void EnableMica()
        {
            try
            {
                if (Environment.OSVersion.Version.Build >= 22000)
                {
                    int micaValue = 1;
                    int result = DwmSetWindowAttribute(this.Handle, DWMWA_MICA, ref micaValue, sizeof(int));
                    if (result == 0)
                    {
                        _micaEnabled = true;
                        int darkMode = 0;
                        DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                    }
                    else EnableAcrylicFallback();
                }
                else EnableAcrylicFallback();
            }
            catch { }
        }

        private void EnableAcrylic()
        {
            try
            {
                var accent = new AccentPolicy
                {
                    AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    GradientColor = 0x99000000
                };
                int accentStructSize = Marshal.SizeOf(accent);
                IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };
                SetWindowCompositionAttribute(this.Handle, ref data);
                Marshal.FreeHGlobal(accentPtr);
                _micaEnabled = true;
            }
            catch { }
        }

        private void EnableAero()
        {
            try
            {
                var accent = new AccentPolicy
                {
                    AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND,
                    GradientColor = 0x99000000
                };
                int accentStructSize = Marshal.SizeOf(accent);
                IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };
                SetWindowCompositionAttribute(this.Handle, ref data);
                Marshal.FreeHGlobal(accentPtr);
                _micaEnabled = true;
            }
            catch { }
        }

        private void EnableAcrylicFallback() => EnableAcrylic();

        protected override void WndProc(ref Message m)
        {
            const int WM_ENTERSIZEMOVE = 0x0231;
            const int WM_EXITSIZEMOVE = 0x0232;
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MAXIMIZE = 0xF030;
            const int WM_SIZE = 0x0005;
            const int SIZE_RESTORED = 0;

            if (_isWin10OrAbove)
            {
                if (m.Msg == WM_ENTERSIZEMOVE)
                {
                    _sizing = true;
                    _inSizeMove = true;
                    DestroyAllDynamicControls();
                    Invalidate();
                }
                else if (m.Msg == WM_EXITSIZEMOVE)
                {
                    _sizing = false;
                    _inSizeMove = false;
                    UpdateScale();
                    CalculateGridRects();
                    RecreateDynamicControls();
                    Invalidate(true);
                }
            }

            if (m.Msg == WM_SYSCOMMAND && (int)m.WParam == SC_MAXIMIZE)
            {
                ToggleFullscreen();
                return;
            }

            if (m.Msg == WM_SIZE && m.WParam.ToInt32() == SIZE_RESTORED)
            {
                if (_savedClientSize != Size.Empty)
                    this.ClientSize = _savedClientSize;
                UpdateScale();
                if (!_sizing)
                    Invalidate();
                if (!_micaEnabled)
                    ApplyBackgroundEffect(_currentBackgroundEffect);
            }

            base.WndProc(ref m);
        }

        private void OnResize(object sender, EventArgs e)
        {
            // 同步全屏状态：如果窗口处于最大化状态，设置 fullscreen = true；否则 false
            fullscreen = (WindowState == FormWindowState.Maximized);

            if (WindowState == FormWindowState.Minimized)
            {
                _savedClientSize = ClientSize;
                _savedWindowState = WindowState;
                return;
            }

            if (_resizing || fullscreen || WindowState == FormWindowState.Maximized) return;

            _resizing = true;
            float targetRatio = (float)VIRTUAL_SIZE.Width / VIRTUAL_SIZE.Height;
            int newWidth = ClientSize.Width;
            int newHeight = (int)(newWidth / targetRatio);
            ClientSize = new Size(newWidth, newHeight);
            _resizing = false;

            UpdateScale();
            if (EditMode) CreateTimeComboBoxes();
            if (!_isWin10OrAbove || !_sizing)
                Invalidate();

            if (_drawerPanel != null)
            {
                _drawerPanel.Width = this.ClientSize.Width;
                if (_drawerOpen)
                {
                    int targetHeight = this.ClientSize.Height / MAX_DRAWER_HEIGHT_RATIO;
                    SetDrawerHeight(targetHeight);
                }
            }
        }

        public void ApplySettings(AppConfig newConfig)
        {
            // 忽略 ColumnWidth 和 RowHeight 的设置，卡片大小始终自动
            // 但如果需要更新其他设置，保留如下：
            appConfig = newConfig;
            ApplyFontSettings();
            UpdateSubjectsByMode();
            CheckEveningClassStates();
            if (EditMode) CreateTimeComboBoxes();
            LoadBackgroundImage();
            ApplyBackgroundEffect(appConfig.BackgroundEffect);
            CalculateGridRects();      // 重新计算网格（自动）
            LoadImages();

            _autoPageInterval = appConfig.AutoPageInterval;
            if (_autoPageTimer != null)
            {
                _autoPageTimer.Interval = _autoPageInterval * 1000;
                if (_autoPageTimer.Enabled && !_isCardEditingMode && !rotationMode)
                {
                    _autoPageTimer.Stop();
                    _autoPageTimer.Start();
                }
            }
            Invalidate();
        }

        private void ApplyFontSettings()
        {
            font12?.Dispose(); font20?.Dispose(); font24?.Dispose(); font30?.Dispose();
            font22?.Dispose(); font36?.Dispose(); hintFont?.Dispose(); buttonFont?.Dispose(); fontSmall?.Dispose();

            float scale = fontScales[appConfig.FontSizeLevel];
            string fontName = appConfig.UseCustomFont ? appConfig.CustomFontName : "微软雅黑";

            font12 = new Font(fontName, 10 * scale);
            font20 = new Font(fontName, 15 * scale);
            font24 = new Font(fontName, 14 * scale);
            font30 = new Font(fontName, 20 * scale);
            font22 = new Font(fontName, 21 * scale, FontStyle.Bold);
            font36 = new Font(fontName, 35 * scale);
            hintFont = new Font(fontName, 15 * scale);
            buttonFont = new Font(fontName, 10 * scale);
            fontSmall = new Font(fontName, 8 * scale);

            TEXT_COLOR = appConfig.FontColorWhite ? Color.White : Color.Black;
        }

        private void UpdateSubjectsByMode()
        {
            currentSubjects = appConfig.CustomSubjects.ToArray();
            CalculateGridRects();
        }

        private void CalculateGridRects()
        {
            gridRects.Clear();
            int cols = 3;
            int rows = 2;
            float areaWidth = GRID_AREA.Width;
            float areaHeight = GRID_AREA.Height;

            // 始终自动计算，不使用用户配置的固定宽高
            float rectWidth = (areaWidth - (cols + 1) * GRID_PADDING) / cols;
            float rectHeight = (areaHeight - (rows + 1) * GRID_PADDING) / rows;

            for (int row = 0; row < rows; row++)
                for (int col = 0; col < cols; col++)
                {
                    int x = GRID_AREA.Left + GRID_PADDING + col * (int)(rectWidth + GRID_PADDING);
                    int y = GRID_AREA.Top + GRID_PADDING + row * (int)(rectHeight + GRID_PADDING);
                    gridRects.Add(new Rectangle(x, y, (int)rectWidth, (int)rectHeight));
                }
        }

        private void LoadHomeworkData(DateTime date)
        {
            DateTime effectiveDate = (historyMode && historyDate.HasValue) ? date : GetEffectiveDate(date);
            HomeworkData loadedData = null;
            if (appConfig.MgmtEnabled && appConfig.MgmtForceRemote)
            {
                loadedData = ManagementHelper.LoadHomeworkDataFromRemote(effectiveDate);
                if (loadedData != null)
                {
                    CheckAndPromptMissingSubjects(loadedData);
                    homeworkData = loadedData;
                    if (EditMode) CreateTimeComboBoxes();
                    return;
                }
            }

            loadedData = HomeworkData.Load(effectiveDate);
            CheckAndPromptMissingSubjects(loadedData);
            homeworkData = loadedData;
            foreach (string subject in appConfig.CustomSubjects)
            {
                if (!homeworkData.Subjects.ContainsKey(subject))
                    homeworkData.Subjects[subject] = "";
                if (!homeworkData.DueTimes.ContainsKey(subject))
                    homeworkData.DueTimes[subject] = appConfig.EveningClassCount >= 3 ? "晚修3" : "无";
            }
            if (EditMode) CreateTimeComboBoxes();
        }

        private void CheckAndPromptMissingSubjects(HomeworkData loadedData)
        {
            if (loadedData == null) return;
            var missingSubjects = new List<string>();
            foreach (var subject in loadedData.Subjects.Keys)
            {
                if (!appConfig.CustomSubjects.Contains(subject))
                    missingSubjects.Add(subject);
            }
            if (missingSubjects.Count > 0)
            {
                string subjectList = string.Join("、", missingSubjects);
                DialogResult result = MessageBox.Show(
                    $"当前状态下加载该作业存在缺少的科目：{subjectList}\n是否添加这些卡片？",
                    "缺失科目",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    foreach (var sub in missingSubjects)
                    {
                        if (!appConfig.CustomSubjects.Contains(sub))
                        {
                            appConfig.CustomSubjects.Add(sub);
                            if (!homeworkData.Subjects.ContainsKey(sub))
                                homeworkData.Subjects[sub] = loadedData.Subjects.ContainsKey(sub) ? loadedData.Subjects[sub] : "";
                            if (!homeworkData.DueTimes.ContainsKey(sub))
                                homeworkData.DueTimes[sub] = loadedData.DueTimes.ContainsKey(sub) ? loadedData.DueTimes[sub] : "无";
                        }
                    }
                    appConfig.Save();
                    SaveHomeworkData();
                    UpdateSubjectsByMode();
                    Invalidate();
                }
                else
                {
                    foreach (var sub in missingSubjects)
                    {
                        loadedData.Subjects.Remove(sub);
                        loadedData.DueTimes.Remove(sub);
                    }
                }
            }
        }

        private void SaveHomeworkData()
        {
            if (appConfig.MgmtEnabled && appConfig.MgmtForceRemote)
            {
                MessageBox.Show("当前已由管理端控制，无法保存本地修改。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DateTime saveDate = historyMode && historyDate.HasValue ? historyDate.Value : currentDate;
            DateTime effectiveSaveDate = (historyMode && historyDate.HasValue) ? saveDate : GetEffectiveDate(saveDate);
            homeworkData.Save(effectiveSaveDate);
        }

        private void ToggleFullscreen()
        {
            bool isWin7 = Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1;
            fullscreen = !fullscreen;
            if (fullscreen)
            {
                if (isWin7)
                {
                    // Win7 传统全屏：最大化但保留边框，任务栏可见
                    FormBorderStyle = FormBorderStyle.Sizable;
                    WindowState = FormWindowState.Maximized;
                }
                else
                {
                    // 现代全屏：无边框最大化（沉浸式）
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                }
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = FormWindowState.Normal;
                ClientSize = VIRTUAL_SIZE;
            }
            UpdateScale();
            Invalidate();
        }

        private void UpdateScale()
        {
            Size clientSize = ClientSize;
            if (clientSize.Width == 0 || clientSize.Height == 0) return;
            float scaleW = (float)clientSize.Width / VIRTUAL_SIZE.Width;
            float scaleH = (float)clientSize.Height / VIRTUAL_SIZE.Height;
            scaleFactor = Math.Min(scaleW, scaleH);
            Size scaledVirtual = new Size((int)(VIRTUAL_SIZE.Width * scaleFactor), (int)(VIRTUAL_SIZE.Height * scaleFactor));
            offset = new Point((clientSize.Width - scaledVirtual.Width) / 2, (clientSize.Height - scaledVirtual.Height) / 2);
        }

        private Point MapToVirtual(Point screenPt) => new Point((int)((screenPt.X - offset.X) / scaleFactor), (int)((screenPt.Y - offset.Y) / scaleFactor));
        private Point MapToScreen(Point virtualPt) => new Point((int)(virtualPt.X * scaleFactor + offset.X), (int)(virtualPt.Y * scaleFactor + offset.Y));

        // ------------------------------ 自动轮播与卡片管理模式 ------------------------------
        private void InitAutoPageTimer()
        {
            _autoPageInterval = appConfig.AutoPageInterval;
            _autoPageTimer = new Timer { Interval = _autoPageInterval * 1000 };
            _autoPageTimer.Tick += (s, e) =>
            {
                if (!_isCardEditingMode && !rotationMode && !editMode && TotalPages > 1)
                {
                    int nextPage = (_currentPage + 1) % TotalPages;
                    SwitchToPage(nextPage, -1);
                }
            };
            _autoPageTimer.Start();
        }

        private void ResetAutoPageTimer()
        {
            if (_autoPageTimer != null)
            {
                _autoPageTimer.Stop();
                _autoPageTimer.Start();
            }
        }

        private void SwitchToPage(int newPage, int direction)
        {
            if (newPage < 0 || newPage >= TotalPages) return;
            if (newPage == _currentPage) return;
            ResetAutoPageTimer();

            if (EditMode)
            {
                DestroyTimeComboBoxes();
            }

            FlyOutCurrentPage(() =>
            {
                _currentPage = newPage;
                scrollOffsets.Clear();
                scrollPaused.Clear();
                pauseStartTime.Clear();
                StartCardFlyInAnimation(-direction);
            }, direction);
        }

        private void FlyOutCurrentPage(Action onComplete, int direction)
        {
            _cardAnimations.Clear();
            _flyOutProgress.Clear();
            int cols = 3;
            double duration = 0.3;
            double totalDelay = 0.1;
            double colDelay = 0.05;
            double rowDelay = 0.03;
            int remaining = 0;
            float screenWidth = VIRTUAL_SIZE.Width;
            float outOffset = direction == 1 ? screenWidth : -screenWidth;

            for (int i = 0; i < appConfig.CustomSubjects.Count; i++)
            {
                int row = i / cols;
                if (row / 2 != _currentPage) continue;
                int col = i % cols;
                double delay = totalDelay + col * colDelay + (row % 2) * rowDelay;

                remaining++;
                int index = i;
                var anim = new Animation
                {
                    Duration = TimeSpan.FromSeconds(duration),
                    StartDelay = TimeSpan.FromSeconds(delay),
                    Tag = index
                };
                anim.OnUpdate = (p) =>
                {
                    float offset = outOffset * (float)p;
                    _flyOutProgress[index] = offset;
                    Invalidate(GetPageCardRect(index));
                };
                anim.OnComplete = () =>
                {
                    remaining--;
                    if (remaining == 0 && _virtualCardFlyOutAnim == null)
                        onComplete?.Invoke();
                };
                anim.Start();
                _animations.Add(anim);
            }

            int visibleCount = Math.Min(CARDS_PER_PAGE, appConfig.CustomSubjects.Count - _currentPage * CARDS_PER_PAGE);
            if (visibleCount < CARDS_PER_PAGE)
            {
                int emptyLocalIndex = visibleCount;
                int row = emptyLocalIndex / 3;
                int col = emptyLocalIndex % 3;
                double delay = totalDelay + col * colDelay + row * rowDelay;
                _virtualCardFlyOutOffset = 0;
                if (_virtualCardFlyOutAnim != null) _animations.Remove(_virtualCardFlyOutAnim);
                _virtualCardFlyOutAnim = new Animation
                {
                    Duration = TimeSpan.FromSeconds(duration),
                    StartDelay = TimeSpan.FromSeconds(delay),
                    Tag = "virtualOut"
                };
                _virtualCardFlyOutAnim.OnUpdate = (p) =>
                {
                    _virtualCardFlyOutOffset = outOffset * (float)p;
                    Invalidate();
                };
                _virtualCardFlyOutAnim.OnComplete = () =>
                {
                    _virtualCardFlyOutOffset = 0;
                    _virtualCardFlyOutAnim = null;
                    if (remaining == 0)
                        onComplete?.Invoke();
                };
                _virtualCardFlyOutAnim.Start();
                _animations.Add(_virtualCardFlyOutAnim);
            }

            if (remaining == 0 && _virtualCardFlyOutAnim == null)
                onComplete?.Invoke();
        }

        private void StartCardFlyInAnimation(int direction)
        {
            _isFlyingIn = true;
            _cardAnimations.Clear();
            _flyInOffsets.Clear();
            _flyInAnimationsRemaining = 0;

            int cols = 3;
            double totalDelay = 0.3;
            double colDelay = 0.08;
            double rowDelay = 0.05;
            double duration = 0.4;
            float screenWidth = VIRTUAL_SIZE.Width;
            float startOffset = direction == 1 ? screenWidth : -screenWidth;
            _lastFlyDirection = direction;

            for (int i = 0; i < appConfig.CustomSubjects.Count; i++)
            {
                int row = i / cols;
                if (row / 2 != _currentPage) continue;
                _flyInOffsets[i] = startOffset;
            }

            for (int i = 0; i < appConfig.CustomSubjects.Count; i++)
            {
                int row = i / cols;
                if (row / 2 != _currentPage) continue;
                int col = i % cols;
                double delay = totalDelay + col * colDelay + (row % 2) * rowDelay;

                var anim = new Animation
                {
                    Duration = TimeSpan.FromSeconds(duration),
                    StartDelay = TimeSpan.FromSeconds(delay),
                    Tag = i
                };
                int index = i;
                anim.OnUpdate = (p) =>
                {
                    double t = 1 - Math.Pow(1 - p, 2);
                    float offset = startOffset * (float)(1 - t);
                    _flyInOffsets[index] = offset;
                    Invalidate(GetPageCardRect(index));
                };
                anim.OnComplete = () =>
                {
                    _flyInAnimationsRemaining--;
                    if (_flyInAnimationsRemaining == 0 && _virtualCardAnim == null)
                    {
                        _isFlyingIn = false;
                        _cardAnimations.Clear();
                        _flyInOffsets.Clear();
                        if (EditMode)
                        {
                            CreateTimeComboBoxes();
                        }
                        Invalidate();
                    }
                };
                anim.Start();
                _cardAnimations[index] = anim;
                _animations.Add(anim);
                _flyInAnimationsRemaining++;
            }

            int visibleCount = Math.Min(CARDS_PER_PAGE, appConfig.CustomSubjects.Count - _currentPage * CARDS_PER_PAGE);
            if (visibleCount < CARDS_PER_PAGE)
            {
                int emptyLocalIndex = visibleCount;
                int row = emptyLocalIndex / 3;
                int col = emptyLocalIndex % 3;
                double delay = totalDelay + col * colDelay + row * rowDelay;
                _virtualCardOffset = startOffset;
                if (_virtualCardAnim != null) _animations.Remove(_virtualCardAnim);
                _virtualCardAnim = new Animation
                {
                    Duration = TimeSpan.FromSeconds(duration),
                    StartDelay = TimeSpan.FromSeconds(delay),
                    Tag = "virtualIn"
                };
                _virtualCardAnim.OnUpdate = (p) =>
                {
                    double t = 1 - Math.Pow(1 - p, 2);
                    _virtualCardOffset = startOffset * (float)(1 - t);
                    Invalidate();
                };
                _virtualCardAnim.OnComplete = () =>
                {
                    _virtualCardOffset = 0;
                    _virtualCardAnim = null;
                    if (_flyInAnimationsRemaining == 0)
                    {
                        _isFlyingIn = false;
                        if (EditMode)
                        {
                            CreateTimeComboBoxes();
                        }
                        Invalidate();
                    }
                };
                _virtualCardAnim.Start();
                _animations.Add(_virtualCardAnim);
            }

            if (_flyInAnimationsRemaining == 0 && _virtualCardAnim == null)
            {
                _isFlyingIn = false;
                if (EditMode)
                {
                    CreateTimeComboBoxes();
                }
            }
        }

        private void RotateNext()
        {
            // 检查当前科目是否有作业，如果没有则跳到下一个
            if (rotationIndex >= 0 && rotationIndex < appConfig.CustomSubjects.Count)
            {
                string subject = appConfig.CustomSubjects[rotationIndex];
                string content = homeworkData.Subjects.ContainsKey(subject) ? homeworkData.Subjects[subject] : "";
                if (string.IsNullOrWhiteSpace(content))
                {
                    RotateManual(1);
                    return;
                }
            }
            RotateManual(1);
        }

        private void RotateManual(int direction)
        {
            var nonEmpty = new List<int>();
            for (int i = 0; i < appConfig.CustomSubjects.Count; i++)
                if (homeworkData.Subjects.ContainsKey(appConfig.CustomSubjects[i]) && !string.IsNullOrWhiteSpace(homeworkData.Subjects[appConfig.CustomSubjects[i]]))
                    nonEmpty.Add(i);
            if (nonEmpty.Count == 0)
            {
                rotationMode = false;
                rotationTimer.Stop();
                CloseDrawer();
                MessageBox.Show("所有科目都没有作业内容，已退出轮播模式", "提示");
                return;
            }

            int curIdx = nonEmpty.IndexOf(rotationIndex);
            if (curIdx < 0) curIdx = 0;
            int newIdx = (curIdx + direction + nonEmpty.Count) % nonEmpty.Count;
            int newIndex = nonEmpty[newIdx];
            if (newIndex == rotationIndex) return;

            _oldSlideIndex = rotationIndex;
            _newSlideIndex = newIndex;
            _oldSlideContent = homeworkData.Subjects.ContainsKey(appConfig.CustomSubjects[_oldSlideIndex]) ? homeworkData.Subjects[appConfig.CustomSubjects[_oldSlideIndex]] : "";
            _newSlideContent = homeworkData.Subjects.ContainsKey(appConfig.CustomSubjects[_newSlideIndex]) ? homeworkData.Subjects[appConfig.CustomSubjects[_newSlideIndex]] : "";
            _slideProgress = 0;
            _isSliding = true;
            _slidePhase = 0;

            // 直接使用 direction 作为滑动方向：1=右箭头，-1=左箭头
            _slideDirection = direction;

            var slideAnim = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(400),
                IsLooping = false,
                Tag = "slide"
            };
            slideAnim.OnUpdate = (p) =>
            {
                _slideProgress = (float)p;
                _drawerPanel?.Invalidate();
            };
            slideAnim.OnComplete = () =>
            {
                if (_slidePhase == 0)
                {
                    _slidePhase = 1;
                    _slideProgress = 0;
                    var newAnim = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(400),
                        IsLooping = false,
                        Tag = "slide"
                    };
                    newAnim.OnUpdate = (p) =>
                    {
                        _slideProgress = (float)p;
                        _drawerPanel?.Invalidate();
                    };
                    newAnim.OnComplete = () =>
                    {
                        _isSliding = false;
                        rotationIndex = _newSlideIndex;
                        _drawerPanel?.Invalidate();
                    };
                    newAnim.Start();
                    _animations.Add(newAnim);
                }
                else
                {
                    _isSliding = false;
                    rotationIndex = _newSlideIndex;
                    _drawerPanel?.Invalidate();
                }
            };
            slideAnim.Start();
            _animations.Add(slideAnim);
        }

        private void EnterCardEditMode()
        {
            if (!_buttonsExpanded)
            {
                _buttonsExpanded = true;
                Invalidate();
            }
            _isCardEditingMode = true;
            _autoPageTimer.Stop();
            StartShakeAnimation();

            int realTotalPages = (int)Math.Ceiling(appConfig.CustomSubjects.Count / (double)CARDS_PER_PAGE);
            if (realTotalPages > 0 && appConfig.CustomSubjects.Count % CARDS_PER_PAGE == 0)
            {
                _showVirtualAddCardPage = true;
            }
            else
            {
                _showVirtualAddCardPage = false;
            }

            Invalidate();
        }

        private void ExitCardEditMode()
        {
            _isCardEditingMode = false;
            _autoPageTimer.Start();
            StopShakeAnimation();
            _showVirtualAddCardPage = false;

            int realTotalPages = (int)Math.Ceiling(appConfig.CustomSubjects.Count / (double)CARDS_PER_PAGE);
            if (_currentPage >= realTotalPages)
                _currentPage = realTotalPages - 1;
            if (_currentPage < 0) _currentPage = 0;

            _virtualCardOffset = 0;
            if (_virtualCardAnim != null)
            {
                _animations.Remove(_virtualCardAnim);
                _virtualCardAnim = null;
            }
            Invalidate();
        }

        private void StartShakeAnimation()
        {
            if (_shakeTimer == null)
            {
                _shakeTimer = new Timer { Interval = 70 };
                _shakeTimer.Tick += (s, e) =>
                {
                    _shakeAngle += _shakeDirection ? _shakeStep : -_shakeStep;
                    if (_shakeAngle > _shakeMaxAngle) { _shakeAngle = _shakeMaxAngle; _shakeDirection = false; }
                    else if (_shakeAngle < -_shakeMaxAngle) { _shakeAngle = -_shakeMaxAngle; _shakeDirection = true; }
                    Invalidate();
                };
            }
            _shakeTimer.Start();
        }

        private void StopShakeAnimation()
        {
            _shakeTimer?.Stop();
            _shakeAngle = 0;
        }

        private void AddNewCard()
        {
            string baseName = "科目";
            string newName = baseName;
            int counter = 1;
            while (appConfig.CustomSubjects.Contains(newName))
            {
                counter++;
                newName = $"{baseName}{counter}";
            }

            string inputName = Microsoft.VisualBasic.Interaction.InputBox("请输入科目名称：", "添加卡片", newName);
            if (string.IsNullOrWhiteSpace(inputName)) return;

            if (appConfig.CustomSubjects.Contains(inputName))
            {
                MessageBox.Show("科目已存在！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool hadPlaceholder = _virtualAddCardRect != Rectangle.Empty;

            int newIndex = appConfig.CustomSubjects.Count;
            appConfig.CustomSubjects.Add(inputName);
            if (!homeworkData.Subjects.ContainsKey(inputName))
                homeworkData.Subjects[inputName] = "";
            if (!homeworkData.DueTimes.ContainsKey(inputName))
                homeworkData.DueTimes[inputName] = "无";
            SaveHomeworkData();
            appConfig.Save();
            UpdateSubjectsByMode();

            if (_showVirtualAddCardPage)
            {
                _showVirtualAddCardPage = false;
            }

            int cardsOnCurrentPageAfter = appConfig.CustomSubjects.Count - _currentPage * CARDS_PER_PAGE;
            bool placeHolderWillDisappear = hadPlaceholder && (cardsOnCurrentPageAfter >= CARDS_PER_PAGE || _showVirtualAddCardPage == false);

            if (placeHolderWillDisappear)
            {
                _addCardFadeOutAlpha = 0f;
                if (_addCardFadeOutAnim != null) _animations.Remove(_addCardFadeOutAnim);
                _addCardFadeOutAnim = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(200),
                    Tag = "addCardFadeOut"
                };
                _addCardFadeOutAnim.OnUpdate = (p) =>
                {
                    _addCardFadeOutAlpha = (float)p;
                    Invalidate(_virtualAddCardRect);
                };
                _addCardFadeOutAnim.OnComplete = () =>
                {
                    _addCardFadeOutAlpha = 0f;
                    _addCardFadeOutAnim = null;
                    _virtualAddCardRect = Rectangle.Empty;
                    FinalizeAddNewCard(newIndex);
                };
                _addCardFadeOutAnim.Start();
                _animations.Add(_addCardFadeOutAnim);
            }
            else
            {
                FinalizeAddNewCard(newIndex);
            }
        }

        private void FinalizeAddNewCard(int newIndex)
        {
            int targetPage = newIndex / CARDS_PER_PAGE;
            if (_currentPage != targetPage)
            {
                _currentPage = targetPage;
                StartCardFlyInAnimation(1);
            }
            else
            {
                var fadeAnim = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(800),
                    Tag = newIndex
                };
                fadeAnim.OnUpdate = (p) =>
                {
                    _fadeInProgress[newIndex] = (float)p;
                    Invalidate(GetPageCardRect(newIndex));
                };
                fadeAnim.OnComplete = () =>
                {
                    _fadeInProgress.Remove(newIndex);
                    Invalidate();
                };
                fadeAnim.Start();
                _animations.Add(fadeAnim);
                Invalidate();
            }
        }

        private void DeleteCard(int index)
        {
            string subject = appConfig.CustomSubjects[index];
            DialogResult result = MessageBox.Show($"确定要删除卡片“{subject}”吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                appConfig.CustomSubjects.RemoveAt(index);
                homeworkData.Subjects.Remove(subject);
                homeworkData.DueTimes.Remove(subject);
                SaveHomeworkData();
                appConfig.Save();
                UpdateSubjectsByMode();

                if (appConfig.CustomSubjects.Count == 0)
                {
                    ExitCardEditMode();
                    appConfig.CustomSubjects.Add("科目");
                    homeworkData.Subjects["科目"] = "";
                    homeworkData.DueTimes["科目"] = "无";
                    SaveHomeworkData();
                    appConfig.Save();
                    UpdateSubjectsByMode();
                    MessageBox.Show("所有卡片已删除，已自动创建一个空白卡片。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    int cardsOnCurrentPage = appConfig.CustomSubjects.Count - _currentPage * CARDS_PER_PAGE;
                    if (cardsOnCurrentPage <= 0 && _currentPage > 0)
                        _currentPage--;
                    int newTotalPages = (int)Math.Ceiling(appConfig.CustomSubjects.Count / (double)CARDS_PER_PAGE);
                    if (_currentPage >= newTotalPages && newTotalPages > 0)
                        _currentPage = newTotalPages - 1;
                    if (_currentPage < 0) _currentPage = 0;
                }

                int realTotalPages = (int)Math.Ceiling(appConfig.CustomSubjects.Count / (double)CARDS_PER_PAGE);
                if (realTotalPages > 0 && appConfig.CustomSubjects.Count % CARDS_PER_PAGE == 0)
                    _showVirtualAddCardPage = true;
                else
                    _showVirtualAddCardPage = false;

                Invalidate();
            }
        }

        private void RenameCard(int index, string newName)
        {
            if (index < 0 || index >= appConfig.CustomSubjects.Count) return;
            string oldName = appConfig.CustomSubjects[index];
            if (string.IsNullOrEmpty(newName) || oldName == newName) return;
            if (appConfig.CustomSubjects.Contains(newName))
            {
                MessageBox.Show("科目已存在！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. 更新科目列表
            appConfig.CustomSubjects[index] = newName;

            // 2. 更新作业内容字典
            if (homeworkData.Subjects.ContainsKey(oldName))
            {
                string content = homeworkData.Subjects[oldName];
                homeworkData.Subjects.Remove(oldName);
                homeworkData.Subjects[newName] = content;
            }

            // 3. 更新提交时间字典
            if (homeworkData.DueTimes.ContainsKey(oldName))
            {
                string due = homeworkData.DueTimes[oldName];
                homeworkData.DueTimes.Remove(oldName);
                homeworkData.DueTimes[newName] = due;
            }

            // 4. 保存到文件
            SaveHomeworkData();
            appConfig.Save();

            // 5. 更新界面
            UpdateSubjectsByMode();  // 重新计算网格布局
            Invalidate();           // 刷新显示
        }

        private void SwapCards(int indexA, int indexB)
        {
            if (indexA == indexB) return;
            if (indexA < 0 || indexA >= appConfig.CustomSubjects.Count || indexB < 0 || indexB >= appConfig.CustomSubjects.Count)
                return;
            var temp = appConfig.CustomSubjects[indexA];
            appConfig.CustomSubjects[indexA] = appConfig.CustomSubjects[indexB];
            appConfig.CustomSubjects[indexB] = temp;
            appConfig.Save();
            UpdateSubjectsByMode();
            Invalidate();
        }

        // ------------------------------ 下拉栏方法 ------------------------------
        private void InitializeDrawerPanel()
        {
            _drawerPanel = new BufferedPanel
            {
                BackColor = Color.FromArgb(200, 30, 30, 30),
                Location = new Point(0, 0),
                Size = new Size(ClientSize.Width, 0),
                Visible = true
            };
            _drawerPanel.Paint += DrawerPanel_Paint;
            _drawerPanel.MouseDown += DrawerPanel_MouseDown;
            _drawerPanel.MouseMove += DrawerPanel_MouseMove;
            _drawerPanel.MouseUp += DrawerPanel_MouseUp;
            _drawerPanel.MouseClick += DrawerPanel_MouseClick;
            this.Controls.Add(_drawerPanel);
            this.Controls.SetChildIndex(_drawerPanel, 0);
        }

        private void DrawerPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (!rotationMode) return;
            int arrowSize = 46;
            int arrowY = (_drawerPanel.Height - arrowSize) / 2;
            Rectangle leftArrowDrawer = new Rectangle(10, arrowY, arrowSize, arrowSize);
            Rectangle rightArrowDrawer = new Rectangle(_drawerPanel.Width - 10 - arrowSize, arrowY, arrowSize, arrowSize);
            if (leftArrowDrawer.Contains(e.Location))
            {
                RotateManual(-1);
            }
            else if (rightArrowDrawer.Contains(e.Location))
            {
                RotateManual(1);
            }
        }

        private void DrawerPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (rotationMode)
            {
                _isSwiping = true;
                _swipeStartX = e.X;
                _swipeStartOffset = _swipeOffset;
            }
            else
            {
                _isDraggingDrawer = true;
                _dragDrawerStartY = e.Y;
                _dragDrawerStartHeight = _drawerPanel.Height;
            }
        }

        private void DrawerPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (rotationMode && _isSwiping)
            {
                int deltaX = e.X - _swipeStartX;
                _swipeOffset = _swipeStartOffset + deltaX;
                float maxSwipe = _drawerPanel.Width * 0.8f;
                if (_swipeOffset > maxSwipe) _swipeOffset = maxSwipe;
                if (_swipeOffset < -maxSwipe) _swipeOffset = -maxSwipe;
                _drawerPanel.Invalidate();
            }
            else if (!rotationMode && _isDraggingDrawer)
            {
                int deltaY = e.Y - _dragDrawerStartY;
                int newHeight = _dragDrawerStartHeight + deltaY;
                if (newHeight < 0) newHeight = 0;
                if (newHeight > this.ClientSize.Height - BAR_HEIGHT)
                    newHeight = this.ClientSize.Height - BAR_HEIGHT;
                SetDrawerHeight(newHeight);
            }
        }

        private void DrawerPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (rotationMode && _isSwiping)
            {
                _isSwiping = false;
                if (Math.Abs(_swipeOffset) > _drawerPanel.Width * 0.3f)
                {
                    if (_swipeOffset > 0)
                        RotateManual(-1);
                    else
                        RotateManual(1);
                }
                _swipeOffset = 0;
                _drawerPanel.Invalidate();
            }
            else if (!rotationMode && _isDraggingDrawer)
            {
                _isDraggingDrawer = false;
                int halfHeight = this.ClientSize.Height / 4;
                if (_drawerPanel.Height > halfHeight)
                    OpenDrawer();
                else
                    CloseDrawer();
            }
        }

        private void DrawerPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            if (rotationMode)
            {
                if (rotationIndex < 0 || rotationIndex >= appConfig.CustomSubjects.Count) return;
                string subject = appConfig.CustomSubjects[rotationIndex];
                string content = homeworkData.Subjects.ContainsKey(subject) ? homeworkData.Subjects[subject] : "";

                g.TranslateTransform(_swipeOffset, 0);

                if (_isSliding)
                {
                    if (_slidePhase == 0)
                    {
                        float offsetX = (_slideDirection == 1) ? -_slideProgress * _drawerPanel.Width : _slideProgress * _drawerPanel.Width;
                        g.TranslateTransform(offsetX, 0);
                        string oldSubject = _oldSlideIndex >= 0 ? appConfig.CustomSubjects[_oldSlideIndex] : subject;
                        string oldContent = _oldSlideContent;
                        DrawRotationContent(g, oldSubject, oldContent);
                        g.ResetTransform();
                    }
                    else
                    {
                        float offsetX = (_slideDirection == 1) ? (1 - _slideProgress) * _drawerPanel.Width : -(1 - _slideProgress) * _drawerPanel.Width;
                        g.TranslateTransform(offsetX, 0);
                        string newSubject = _newSlideIndex >= 0 ? appConfig.CustomSubjects[_newSlideIndex] : subject;
                        string newContent = _newSlideContent;
                        DrawRotationContent(g, newSubject, newContent);
                        g.ResetTransform();
                    }
                }
                else
                {
                    DrawRotationContent(g, subject, content);
                }

                g.ResetTransform();

                if (!_isSwiping)
                {
                    int arrowSize = 46;
                    int arrowY = (_drawerPanel.Height - arrowSize) / 2;
                    Rectangle leftArrowDrawer = new Rectangle(10, arrowY, arrowSize, arrowSize);
                    Rectangle rightArrowDrawer = new Rectangle(_drawerPanel.Width - 10 - arrowSize, arrowY, arrowSize, arrowSize);
                    if (leftArrowImage != null)
                        g.DrawImage(leftArrowImage, leftArrowDrawer);
                    else
                        DrawDefaultArrow(g, leftArrowDrawer, true);
                    if (rightArrowImage != null)
                        g.DrawImage(rightArrowImage, rightArrowDrawer);
                    else
                        DrawDefaultArrow(g, rightArrowDrawer, false);
                }
            }
            else
            {
                DateTime now = DateTime.Now;
                string timeStr = now.ToString("HH:mm");
                string dateStr = now.ToString("yyyy年MM月dd日 dddd");

                using (Brush whiteBrush = new SolidBrush(Color.White))
                using (Font timeFont = new Font("微软雅黑", 36, FontStyle.Bold))
                using (Font dateFont = new Font("微软雅黑", 16))
                {
                    SizeF timeSize = g.MeasureString(timeStr, timeFont);
                    SizeF dateSize = g.MeasureString(dateStr, dateFont);
                    float totalHeight = timeSize.Height + dateSize.Height + 10;
                    float startY = (_drawerPanel.Height - totalHeight) / 2;
                    float timeX = (_drawerPanel.Width - timeSize.Width) / 2;
                    float dateX = (_drawerPanel.Width - dateSize.Width) / 2;
                    g.DrawString(timeStr, timeFont, whiteBrush, timeX, startY);
                    g.DrawString(dateStr, dateFont, whiteBrush, dateX, startY + timeSize.Height + 10);
                }
            }
        }

        private void OpenDrawer(int targetHeight = -1)
        {
            if (_drawerOpen) return;
            _drawerOpen = true;
            if (targetHeight <= 0)
                targetHeight = this.ClientSize.Height / MAX_DRAWER_HEIGHT_RATIO;
            var startHeight = _drawerPanel.Height;
            var anim = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(150),  // 原来是 300
                Tag = "drawerOpen"
            };
            anim.OnUpdate = (p) =>
            {
                int newHeight = (int)(startHeight + (targetHeight - startHeight) * p);
                SetDrawerHeight(newHeight);
            };
            anim.OnComplete = () => SetDrawerHeight(targetHeight);
            anim.Start();
            _animations.Add(anim);
        }

        private void CloseDrawer()
        {
            if (!_drawerOpen) return;
            _drawerOpen = false;
            var startHeight = _drawerPanel.Height;
            var anim = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(150),  // 原来是 300
                Tag = "drawerClose"
            };
            anim.OnUpdate = (p) =>
            {
                int newHeight = (int)(startHeight * (1 - p));
                SetDrawerHeight(newHeight);
            };
            anim.OnComplete = () =>
            {
                SetDrawerHeight(0);
                if (rotationMode)
                {
                    rotationMode = false;
                    rotationTimer.Stop();
                    Invalidate();
                }
            };
            anim.Start();
            _animations.Add(anim);
        }

        private void SetDrawerHeight(int height)
        {
            if (_drawerPanel == null) return;
            _drawerPanel.Size = new Size(this.ClientSize.Width, height);
            _drawerPanel.Location = new Point(0, 0);
            _drawerPanel.Invalidate();
        }

        // ------------------------------ 鼠标事件 ------------------------------
        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!rotationMode && e.Y <= BAR_HEIGHT)
            {
                OpenDrawer();
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (_isFlyingIn) return;
            Point v = MapToVirtual(e.Location);
            int x = v.X, y = v.Y;

            foreach (var key in _buttonPressed.Keys.ToList()) _buttonPressed[key] = false;
            _settingsPressed = _rotatePressed = _editPressed = _historyPressed = _fullscreenPressed = _backPressed = _minimizePressed = _closePressed = _expandPressed = _collapsePressed = _exportPressed = _managePressed = _addCardPressed = false;

            if (!_buttonsExpanded)
            {
                if (expandBtnRect.Contains(x, y)) { _expandPressed = true; _buttonPressed["expand"] = true; }
                else if (editBtnRect.Contains(x, y)) { _editPressed = true; _buttonPressed["edit"] = true; }
                else if (fullscreenBtnRect.Contains(x, y)) { _fullscreenPressed = true; _buttonPressed["fullscreen"] = true; }
                else if (minimizeBtnRect.Contains(x, y)) { _minimizePressed = true; _buttonPressed["minimize"] = true; }
            }
            else
            {
                if (_isCardEditingMode)
                {
                    if (manageBtnRect.Contains(x, y)) { _managePressed = true; _buttonPressed["manage"] = true; }
                    else if (addCardBtnRect.Contains(x, y)) { _addCardPressed = true; _buttonPressed["addCard"] = true; }
                }
                else
                {
                    if (collapseBtnRect.Contains(x, y)) { _collapsePressed = true; _buttonPressed["collapse"] = true; }
                    else if (settingsBtnRect.Contains(x, y)) { _settingsPressed = true; _buttonPressed["settings"] = true; }
                    else if (rotateBtnRect.Contains(x, y)) { _rotatePressed = true; _buttonPressed["rotate"] = true; }
                    else if (manageBtnRect.Contains(x, y)) { _managePressed = true; _buttonPressed["manage"] = true; }
                    else if (exportBtnRect.Contains(x, y)) { _exportPressed = true; _buttonPressed["export"] = true; }
                    else if (editBtnRect.Contains(x, y)) { _editPressed = true; _buttonPressed["edit"] = true; }
                    else if (historyBtnRect.Contains(x, y)) { _historyPressed = true; _buttonPressed["history"] = true; }
                    else if (fullscreenBtnRect.Contains(x, y)) { _fullscreenPressed = true; _buttonPressed["fullscreen"] = true; }
                    else if (minimizeBtnRect.Contains(x, y)) { _minimizePressed = true; _buttonPressed["minimize"] = true; }
                    else if (closeBtnRect.Contains(x, y)) { _closePressed = true; _buttonPressed["close"] = true; }
                }
            }

            if (_isCardEditingMode && !rotationMode)
            {
                bool hitDelete = false;
                foreach (var delRect in _deleteButtonRects)
                {
                    if (delRect.Contains(x, y)) { hitDelete = true; break; }
                }
                bool hitArrow = (TotalPages > 1 && (leftArrowRect.Contains(x, y) || rightArrowRect.Contains(x, y)));
                if (!hitDelete && !hitArrow && inlineEditControl == null)
                {
                    List<Rectangle> pageRects = GetPageGridRects();
                    int startIndex = _currentPage * CARDS_PER_PAGE;
                    for (int i = 0; i < pageRects.Count; i++)
                    {
                        int cardIndex = startIndex + i;
                        if (cardIndex >= appConfig.CustomSubjects.Count) break;
                        if (pageRects[i].Contains(x, y))
                        {
                            _isDragging = true;
                            _dragStartIndex = cardIndex;
                            _dragStartPoint = new Point(x, y);
                            _dragOverIndex = cardIndex;
                            break;
                        }
                    }
                }
            }

            if (!rotationMode && !editMode && !_isCardEditingMode)
            {
                for (int i = 0; i < gridRects.Count; i++)
                {
                    var rect = gridRects[i];
                    int col = i % 3;
                    int row = i / 3;
                    if (Math.Abs(x - rect.Right) < 5 && col < 2)
                    {
                        _isResizing = true;
                        _resizeTargetCol = col;
                        _resizeStartX = x;
                        _originalWidth = rect.Width;
                        break;
                    }
                    if (Math.Abs(y - rect.Bottom) < 5 && row < 1)
                    {
                        _isResizing = true;
                        _resizeTargetRow = row;
                        _resizeStartY = y;
                        _originalHeight = rect.Height;
                        break;
                    }
                }
            }

            Invalidate();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isFlyingIn) return;
            Point v = MapToVirtual(e.Location);
            int x = v.X, y = v.Y;

            if (_isDragging && _isCardEditingMode && !rotationMode)
            {
                List<Rectangle> pageRects = GetPageGridRects();
                int startIndex = _currentPage * CARDS_PER_PAGE;
                int newOver = -1;
                for (int i = 0; i < pageRects.Count; i++)
                {
                    int cardIndex = startIndex + i;
                    if (cardIndex >= appConfig.CustomSubjects.Count) break;
                    if (pageRects[i].Contains(x, y))
                    {
                        newOver = cardIndex;
                        break;
                    }
                }
                if (newOver != _dragOverIndex)
                {
                    _dragOverIndex = newOver;
                    Invalidate();
                }
            }

            string newHoverKey = null;
            if (_isButtonBarAnimating)
            {
                for (int i = 0; i < _currentButtonRects.Count; i++)
                {
                    if (_currentButtonRects[i].Contains(x, y))
                    {
                        newHoverKey = _currentButtonKeys[i];
                        break;
                    }
                }
            }
            else
            {
                if (!_buttonsExpanded)
                {
                    if (expandBtnRect.Contains(x, y)) newHoverKey = "expand";
                    else if (editBtnRect.Contains(x, y)) newHoverKey = "edit";
                    else if (fullscreenBtnRect.Contains(x, y)) newHoverKey = "fullscreen";
                    else if (minimizeBtnRect.Contains(x, y)) newHoverKey = "minimize";
                }
                else
                {
                    if (_isCardEditingMode)
                    {
                        if (manageBtnRect.Contains(x, y)) newHoverKey = "manage";
                        else if (addCardBtnRect.Contains(x, y)) newHoverKey = "addCard";
                    }
                    else
                    {
                        if (collapseBtnRect.Contains(x, y)) newHoverKey = "collapse";
                        else if (settingsBtnRect.Contains(x, y)) newHoverKey = "settings";
                        else if (rotateBtnRect.Contains(x, y)) newHoverKey = "rotate";
                        else if (manageBtnRect.Contains(x, y)) newHoverKey = "manage";
                        else if (exportBtnRect.Contains(x, y)) newHoverKey = "export";
                        else if (editBtnRect.Contains(x, y)) newHoverKey = "edit";
                        else if (historyBtnRect.Contains(x, y)) newHoverKey = "history";
                        else if (fullscreenBtnRect.Contains(x, y)) newHoverKey = "fullscreen";
                        else if (minimizeBtnRect.Contains(x, y)) newHoverKey = "minimize";
                        else if (closeBtnRect.Contains(x, y)) newHoverKey = "close";
                    }
                }
            }

            if (_isCardEditingMode && _virtualAddCardRect != Rectangle.Empty)
            {
                bool nowHover = _virtualAddCardRect.Contains(x, y);
                if (nowHover != _lastAddCardHover)
                {
                    _lastAddCardHover = nowHover;
                    Invalidate(_virtualAddCardRect);
                }
            }
            else
            {
                if (_lastAddCardHover)
                {
                    _lastAddCardHover = false;
                    if (_virtualAddCardRect != Rectangle.Empty)
                        Invalidate(_virtualAddCardRect);
                }
            }

            if (newHoverKey == _currentHoverKey) return;
            if (_currentHoverKey != null)
                StartButtonHoverAnimation(_currentHoverKey, false);
            if (newHoverKey != null)
            {
                if (!_buttonHoverProgress.ContainsKey(newHoverKey))
                    _buttonHoverProgress[newHoverKey] = 0;
                StartButtonHoverAnimation(newHoverKey, true);
            }
            _currentHoverKey = newHoverKey;
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (_isFlyingIn) return;

            if (_isResizing)
            {
                _isResizing = false;
                if (_resizeTargetCol >= 0)
                {
                    int newWidth = _originalWidth + (e.X - _resizeStartX);
                    if (newWidth >= 150 && newWidth <= 800)
                        appConfig.ColumnWidth = newWidth;
                    else
                        appConfig.ColumnWidth = 0;
                }
                if (_resizeTargetRow >= 0)
                {
                    int newHeight = _originalHeight + (e.Y - _resizeStartY);
                    if (newHeight >= 120 && newHeight <= 600)
                        appConfig.RowHeight = newHeight;
                    else
                        appConfig.RowHeight = 0;
                }
                appConfig.Save();
                CalculateGridRects();
                Invalidate();
                _resizeTargetCol = -1;
                _resizeTargetRow = -1;
            }

            if (_isDragging && _isCardEditingMode && !rotationMode)
            {
                if (_dragOverIndex != -1 && _dragOverIndex != _dragStartIndex)
                {
                    SwapCards(_dragStartIndex, _dragOverIndex);
                }
                _isDragging = false;
                _dragStartIndex = -1;
                _dragOverIndex = -1;
                Invalidate();
            }

            foreach (var key in _buttonPressed.Keys.ToList()) _buttonPressed[key] = false;
            _settingsPressed = _rotatePressed = _editPressed = _historyPressed = _fullscreenPressed = _backPressed = _minimizePressed = _closePressed = _expandPressed = _collapsePressed = _exportPressed = _managePressed = _addCardPressed = false;
            Invalidate();
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (_isFlyingIn) return;

            if (_currentHoverKey != null)
            {
                StartButtonHoverAnimation(_currentHoverKey, false);
                _currentHoverKey = null;
            }
            foreach (var key in _buttonPressed.Keys.ToList())
                _buttonPressed[key] = false;
            Invalidate();

            if (_lastAddCardHover && _virtualAddCardRect != Rectangle.Empty)
            {
                _lastAddCardHover = false;
                Invalidate(_virtualAddCardRect);
            }
        }

        private void OnActivated(object sender, EventArgs e) { UpdateScale(); Invalidate(); }

        private void StartButtonHoverAnimation(string key, bool fadeIn)
        {
            var existing = _animations.FirstOrDefault(a => a.Tag?.ToString() == key);
            if (existing != null) _animations.Remove(existing);

            float start = _buttonHoverProgress.GetValueOrDefault(key, 0);
            float target = fadeIn ? 1 : 0;

            var anim = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(150),
                Tag = key
            };
            anim.OnUpdate = (p) =>
            {
                _buttonHoverProgress[key] = start + (target - start) * (float)p;
                Invalidate();
            };
            anim.OnComplete = () =>
            {
                _buttonHoverProgress[key] = target;
                Invalidate();
            };
            anim.Start();
            _animations.Add(anim);
        }

        // ------------------------------ 辅助绘图方法 ------------------------------
        private GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius, bool bottomOnly = false)
        {
            var path = new GraphicsPath();
            if (radius <= 0) { path.AddRectangle(rect); return path; }

            if (bottomOnly)
            {
                int x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height, d = radius * 2;
                path.AddLine(x, y, x + w, y);
                path.AddLine(x + w, y, x + w, y + h - radius);
                path.AddArc(new Rectangle(x + w - d, y + h - d, d, d), 0, 90);
                path.AddLine(x + w - radius, y + h, x + radius, y + h);
                path.AddArc(new Rectangle(x, y + h - d, d, d), 90, 90);
                path.AddLine(x, y + h - radius, x, y);
            }
            else
            {
                int d = radius * 2;
                Rectangle arcRect = new Rectangle(rect.Location, new Size(d, d));
                path.AddArc(arcRect, 180, 90);
                arcRect.X = rect.Right - d;
                path.AddArc(arcRect, 270, 90);
                arcRect.Y = rect.Bottom - d;
                path.AddArc(arcRect, 0, 90);
                arcRect.X = rect.Left;
                path.AddArc(arcRect, 90, 90);
                path.CloseFigure();
            }
            return path;
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            Rectangle arcRect = new Rectangle(rect.Location, new Size(d, d));
            path.AddArc(arcRect, 180, 90);
            arcRect.X = rect.Right - d;
            path.AddArc(arcRect, 270, 90);
            arcRect.Y = rect.Bottom - d;
            path.AddArc(arcRect, 0, 90);
            arcRect.X = rect.Left;
            path.AddArc(arcRect, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Rectangle GetDueTimeRect(int subjectIndex)
        {
            Rectangle rect = GetPageCardRect(subjectIndex);
            if (rect.IsEmpty) return Rectangle.Empty;
            string subject = appConfig.CustomSubjects[subjectIndex];
            string dueTime = homeworkData.DueTimes.ContainsKey(subject) ? homeworkData.DueTimes[subject] : "";
            string prefix = "提交时间：";
            string displayTime = string.IsNullOrEmpty(dueTime) ? "无" : dueTime;

            float prefixWidth, timeWidth;
            using (Graphics g = CreateGraphics())
            {
                prefixWidth = g.MeasureString(prefix, fontSmall).Width;
                timeWidth = g.MeasureString(displayTime, font22).Width;
            }
            float totalWidth = prefixWidth + timeWidth;
            int rightX = rect.Right - 10;
            float startX = rightX - totalWidth;
            int lineY = rect.Top + 50;
            int timeY = lineY - font22.Height;
            int prefixY = lineY - fontSmall.Height;
            int minY = Math.Min(prefixY, timeY);
            int maxY = lineY;
            int height = maxY - minY + 2;
            if (height < 25) { minY = maxY - 25; height = 25; }
            return new Rectangle((int)startX, minY, (int)totalWidth + 5, height);
        }

        private Rectangle GetPageCardRect(int globalIndex)
        {
            int page = globalIndex / CARDS_PER_PAGE;
            int idx = globalIndex % CARDS_PER_PAGE;
            if (page == _currentPage && idx < gridRects.Count)
                return gridRects[idx];
            else
                return Rectangle.Empty;
        }

        // ------------------------------ 模式切换下拉框（废弃） ------------------------------
        private void InitializeModeComboBox()
        {
            modeComboBox = new ComboBox { Visible = false };
            Controls.Add(modeComboBox);
        }

        // ------------------------------ 图片加载 ------------------------------
        private void LoadImages()
        {
            string theme = appConfig.CardOpacity < 50 ? "Dark" : "Light";
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", theme);
            buttonImage = LoadImage(Path.Combine(imagePath, "按钮.png"), new Size(BTN_SQUARE_SIZE, BTN_SQUARE_SIZE));
            historyBtnImage = LoadImage(Path.Combine(imagePath, "更多.png"), new Size(BTN_SQUARE_SIZE, BTN_SQUARE_SIZE));

            Image originalBack = LoadImage(Path.Combine(imagePath, "返回.png"), true);
            if (originalBack != null)
            {
                int targetHeight = BTN_SQUARE_SIZE;
                int targetWidth = (int)((float)originalBack.Width / originalBack.Height * targetHeight);
                backBtnImage = new Bitmap(originalBack, new Size(targetWidth, targetHeight));
            }

            leftArrowImage = LoadImage(Path.Combine(imagePath, "左箭头.png"), true);
            if (leftArrowImage == null) leftArrowImage = LoadImage(Path.Combine(imagePath, "箭头图片.png"), true);
            if (leftArrowImage != null)
            {
                rightArrowImage = (Image)new Bitmap(leftArrowImage);
                rightArrowImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }

            manageCardImage = LoadImage(Path.Combine(imagePath, "管理卡片.png"), true);
            addCardImage = LoadImage(Path.Combine(imagePath, "添加卡片.png"), true);

            string[] iconNames = { "编辑", "返回", "历史", "轮换", "全屏", "设置", "缩小", "完成", "关闭", "最小化", "展开", "收起", "导出" };
            int iconSize = (int)(BTN_SQUARE_SIZE * 0.6);
            foreach (string name in iconNames)
            {
                string filePath = Path.Combine(imagePath, name + ".png");
                if (File.Exists(filePath))
                {
                    using (var img = Image.FromFile(filePath))
                    {
                        var targetBitmap = new Bitmap(iconSize, iconSize);
                        using (var g = Graphics.FromImage(targetBitmap))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(img, 0, 0, iconSize, iconSize);
                        }
                        buttonIcons[name] = targetBitmap;
                    }
                }
            }
        }

        private Image LoadImage(string path, bool originalSize) { try { return File.Exists(path) ? Image.FromFile(path) : null; } catch { return null; } }
        private Image LoadImage(string path, Size size) { try { return File.Exists(path) ? new Bitmap(Image.FromFile(path), size) : null; } catch { return null; } }

        // ------------------------------ 颜色辅助 ------------------------------
        private Color ParseColor(string rgbString, Color defaultColor)
        {
            try
            {
                var parts = rgbString.Split(',');
                if (parts.Length == 3) return Color.FromArgb(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
            }
            catch { }
            return defaultColor;
        }

        private Brush GetDueTimeBrush(string value)
        {
            if (value.StartsWith("晚修") && int.TryParse(value.Substring(2), out int idx))
            {
                idx--;
                return idx switch
                {
                    0 => RED_SEMI,
                    1 => ORANGE_SEMI,
                    2 => GREEN_SEMI,
                    3 => BLUE_SEMI,
                    4 => PURPLE_SEMI,
                    5 => DARKORANGE_SEMI,
                    _ => new SolidBrush(TEXT_COLOR)
                };
            }
            return new SolidBrush(TEXT_COLOR);
        }

        // ------------------------------ 主绘制 ------------------------------
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.TranslateTransform(offset.X, offset.Y);
            g.ScaleTransform(scaleFactor, scaleFactor);

            if (_inSizeMove)
            {
                using (var bgBrush = new SolidBrush(Color.FromArgb(0, 0, 0, 0)))
                {
                    g.FillRectangle(bgBrush, new Rectangle(0, 0, VIRTUAL_SIZE.Width, VIRTUAL_SIZE.Height));
                }
                g.ResetTransform();
                return;
            }

            // 背景绘制
            if (appConfig.UseBackgroundImage)
            {
                if (appConfig.BackgroundImageMode == ImageFillMode.Tile && _tileBrush != null)
                {
                    g.FillRectangle(_tileBrush, new Rectangle(0, 0, VIRTUAL_SIZE.Width, VIRTUAL_SIZE.Height));
                }
                else if (_cachedBackground != null)
                {
                    g.DrawImage(_cachedBackground, 0, 0);
                }
                else if (_backgroundImage != null)
                {
                    _cachedBackground = GenerateBackgroundCache(_backgroundImage);
                    g.DrawImage(_cachedBackground, 0, 0);
                }
            }
            else if (!_micaEnabled)
            {
                using (var bgBrush = new SolidBrush(Color.FromArgb(32, 32, 32, 32)))
                {
                    g.FillRectangle(bgBrush, new Rectangle(0, 0, VIRTUAL_SIZE.Width, VIRTUAL_SIZE.Height));
                }
            }

            // 顶部栏背景区域
            Rectangle barRect = new Rectangle(0, BAR_Y, VIRTUAL_SIZE.Width, BAR_HEIGHT);
            if (gridRects.Count >= 3)
            {
                int barLeft = gridRects[0].Left;
                int barRight = gridRects[2].Right;
                barRect = new Rectangle(barLeft, BAR_Y, barRight - barLeft, BAR_HEIGHT);
            }

            float opacityFactor = appConfig.CardOpacity / 100f;
            int barAlpha = (int)(255 * opacityFactor);
            Color barColor = ParseColor(appConfig.BarColor, Color.Yellow);
            using (var barBrush = new SolidBrush(Color.FromArgb(barAlpha, barColor.R, barColor.G, barColor.B)))
            using (var barPath = CreateRoundedRectPath(barRect, ROUND_RADIUS, bottomOnly: true))
            {
                g.FillPath(barBrush, barPath);
                using (var borderPen = new Pen(Color.FromArgb(120, 255, 255, 255), 1))
                {
                    g.DrawPath(borderPen, barPath);
                }
            }

            // 绘制晚修进度条（在日期和按钮之前）
            DrawEveningProgress(g, barRect);

            DrawDateInfo(g, barRect);

            // 绘制网格或轮播
            if (rotationMode)
            {
                // 轮播模式下不绘制卡片网格
            }
            else
            {
                DrawGrid(g);
            }

            DrawButtons(g, barRect);

            if (editMode && !rotationMode && !_isCardEditingMode)
            {
                using (var hintFont = new Font("微软雅黑", 11, FontStyle.Bold | FontStyle.Italic))
                using (var hintBrush = new SolidBrush(Color.FromArgb(255, 255, 200, 80)))
                {
                    string hint = "提示1.点击卡片区域编辑作业内容\n      2.提交时间可通过下拉框修改\n      3.语音输入请在编辑时按下 ⊞+H (Win+H)";
                    SizeF size = g.MeasureString(hint, hintFont);
                    float x = (VIRTUAL_SIZE.Width - size.Width) / 2;
                    float y = VIRTUAL_SIZE.Height - size.Height - 35;
                    RectangleF textRect = new RectangleF(x - 5, y - 5, size.Width + 10, size.Height + 10);
                    using (var bgBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                        g.FillRectangle(bgBrush, textRect);
                    using (var borderPen = new Pen(Color.FromArgb(255, 255, 200, 80), 2))
                        g.DrawRectangle(borderPen, textRect.X, textRect.Y, textRect.Width, textRect.Height);
                    g.DrawString(hint, hintFont, hintBrush, x, y);
                }
            }

            g.ResetTransform();
        }

        private void DrawDateInfo(Graphics g, Rectangle barRect)
        {
            DateTime now = historyMode && historyDate.HasValue ? historyDate.Value : currentDate;
            string[] weekdays = { "星期一", "星期二", "星期三", "星期四", "星期五", "星期六", "星期日" };
            string weekday = weekdays[(int)now.DayOfWeek == 0 ? 6 : (int)now.DayOfWeek - 1];
            string date = now.ToString("yyyy年MM月dd日") + (historyMode ? " (历史作业)" : "");

            // 获取当前晚修节次（仅非历史模式且非轮播模式）
            string currentEvening = "";
            if (!rotationMode && !historyMode && appConfig.ShowDueTime && _activeEvenings.Count > 0)
            {
                currentEvening = " · " + _activeEvenings[0];  // 例如 " · 晚修一"
            }

            using (Brush textBrush = new SolidBrush(TEXT_COLOR))
            {
                if (rotationMode)
                {
                    int totalHeight = font20.Height + font12.Height;
                    int y = barRect.Top + (barRect.Height - totalHeight) / 2;
                    SizeF wSize = g.MeasureString(weekday, font20);
                    g.DrawString(weekday, font20, textBrush, (VIRTUAL_SIZE.Width - wSize.Width) / 2, y);
                    string fullDate = date + currentEvening;
                    SizeF dSize = g.MeasureString(fullDate, font12);
                    g.DrawString(fullDate, font12, textBrush, (VIRTUAL_SIZE.Width - dSize.Width) / 2, y + font20.Height);
                }
                else
                {
                    int totalHeight = font20.Height + font12.Height;
                    int y = barRect.Top + (barRect.Height - totalHeight) / 2;
                    int x = barRect.Left + BAR_PADDING;
                    g.DrawString(weekday, font20, textBrush, x, y);
                    string fullDate = date + currentEvening;
                    g.DrawString(fullDate, font12, textBrush, x, y + font20.Height);
                }
            }
        }
        
        // ------------------------------ 晚修进度条绘制 ------------------------------
        private void DrawEveningProgress(Graphics g, Rectangle barRect)
        {
            // 仅在非轮播、非历史模式且显示提交时间时生效
            if (rotationMode || historyMode || !appConfig.ShowDueTime) return;
            if (_activeEvenings.Count == 0) return;

            string eveningName = _activeEvenings[0];
            int index = -1;
            if (eveningName.Length >= 3 && int.TryParse(eveningName.Substring(2), out int num))
                index = num - 1;
            if (index < 0 || index >= appConfig.EveningClassTimes.Count) return;

            var time = appConfig.EveningClassTimes[index];
            if (!DateTime.TryParse(time.Start, out DateTime start) || !DateTime.TryParse(time.End, out DateTime end)) return;

            DateTime now = DateTime.Now;
            DateTime startToday = DateTime.Today.Add(start.TimeOfDay);
            DateTime endToday = DateTime.Today.Add(end.TimeOfDay);

            if (now < startToday || now > endToday) return;

            double total = (endToday - startToday).TotalSeconds;
            double elapsed = (now - startToday).TotalSeconds;
            double ratio = elapsed / total;
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;

            // 计算进度条宽度（像素）
            int progressWidth = (int)(barRect.Width * ratio);
            if (progressWidth <= 0) return;

            // 获取顶部栏颜色
            Color barColor = ParseColor(appConfig.BarColor, Color.Yellow);
            
            // 原始顶部栏的透明度 (由 appConfig.CardOpacity 决定)
            float opacityFactor = appConfig.CardOpacity / 100f;
            int normalAlpha = (int)(255 * opacityFactor);          // 例如 20% -> 51
            int highlightAlpha = (int)(255 * (opacityFactor * 0.8)); // 例如 20% -> 41，更实（不透明度更高）

            // 确保 alpha 在合法范围
            normalAlpha = Math.Clamp(normalAlpha, 0, 255);
            highlightAlpha = Math.Clamp(highlightAlpha, 0, 255);

            // 创建圆角矩形路径（与顶部栏一致）
            using (var clipPath = CreateRoundedRectPath(barRect, ROUND_RADIUS, bottomOnly: true))
            {
                g.SetClip(clipPath, CombineMode.Replace);  // 限制绘制区域为顶部栏圆角内
                // 先绘制已过部分（更不透明）
                using (var progressBrush = new SolidBrush(Color.FromArgb(highlightAlpha, barColor)))
                {
                    g.FillRectangle(progressBrush, barRect.X, barRect.Y, progressWidth, barRect.Height);
                }
                // 可选：未过部分不需要额外绘制，因为背景就是原始顶部栏（已经画好了）
                // 但是为了更明显的对比，可以绘制未过部分保持正常透明度，但背景已有，无需重复。
                g.ResetClip();
            }
        }

        // ------------------------------ 按钮绘制方法（含缓动动画） ------------------------------
        private void DrawSingleButton(Graphics g, Rectangle rect, string text, Image icon, bool pressed, string key, float alpha = 1f)
        {
            bool isPressed = _buttonPressed.GetValueOrDefault(key) || pressed;
            int alphaInt = (int)(255 * Math.Max(0, Math.Min(1, alpha)));

            float hoverProgress = _buttonHoverProgress.GetValueOrDefault(key, 0);
            int centerX = rect.X + rect.Width / 2;
            int centerY = rect.Y + rect.Height / 2;
            int maxRadius = rect.Width / 2;

            if (hoverProgress > 0)
            {
                float radiusGrowth = (float)Math.Pow(hoverProgress, 0.8);
                for (int i = 0; i < 4; i++)
                {
                    float radiusFactor = 0.6f + i * 0.15f;
                    float radius = maxRadius * radiusFactor * radiusGrowth;
                    int baseAlpha = 25 - i * 10;
                    if (baseAlpha < 0) baseAlpha = 0;
                    int hoverAlpha = (int)(baseAlpha * hoverProgress * alpha);
                    if (hoverAlpha > 0)
                    {
                        using (var brush = new SolidBrush(Color.FromArgb(hoverAlpha, 255, 255, 255)))
                        {
                            g.FillEllipse(brush, centerX - radius, centerY - radius, radius * 2, radius * 2);
                        }
                    }
                }
            }

            if (icon != null)
            {
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix { Matrix33 = alpha };
                var imgAttr = new System.Drawing.Imaging.ImageAttributes();
                imgAttr.SetColorMatrix(colorMatrix);
                int targetHeight = (int)(rect.Height * 0.5);
                int targetWidth = (int)((float)icon.Width / icon.Height * targetHeight);
                int iconX = rect.Left + (rect.Width - targetWidth) / 2;
                int iconY = rect.Top + (rect.Height - targetHeight) / 2 - 7;
                g.DrawImage(icon, new Rectangle(iconX, iconY, targetWidth, targetHeight), 0, 0, icon.Width, icon.Height, GraphicsUnit.Pixel, imgAttr);
                imgAttr.Dispose();
            }

            // 按钮文字绘制：底部对齐后上移 6 像素
            using (var textBrush = new SolidBrush(Color.FromArgb(alphaInt, TEXT_COLOR)))
            {
                SizeF textSize = g.MeasureString(text, buttonFont);
                float textX = rect.Left + (rect.Width - textSize.Width) / 2;
                float textY = rect.Bottom - textSize.Height - 1;   // 底部上移 5px
                g.DrawString(text, buttonFont, textBrush, textX, textY);
            }
        }

        private void DrawWin11Button(Graphics g, Rectangle rect, string text, Image icon, ref Rectangle targetRect, Point pos, bool pressed, string key)
        {
            targetRect = new Rectangle(pos, new Size(BTN_SQUARE_SIZE, BTN_SQUARE_SIZE));
            DrawSingleButton(g, targetRect, text, icon, pressed, key);
        }

        private void DrawDefaultArrow(Graphics g, Rectangle rect, bool left)
        {
            using (var brush = new SolidBrush(Color.DarkGray))
            {
                Point[] points;
                if (left)
                    points = new Point[] { new Point(rect.Right, rect.Top), new Point(rect.Right, rect.Bottom), new Point(rect.Left, rect.Top + rect.Height / 2) };
                else
                    points = new Point[] { new Point(rect.Left, rect.Top), new Point(rect.Left, rect.Bottom), new Point(rect.Right, rect.Top + rect.Height / 2) };
                g.FillPolygon(brush, points);
            }
        }

        private void DrawImageCentered(Graphics g, Image img, Rectangle container)
        {
            float scale = Math.Min((float)container.Width / img.Width, (float)container.Height / img.Height);
            int w = (int)(img.Width * scale);
            int h = (int)(img.Height * scale);
            int x = container.X + (container.Width - w) / 2;
            int y = container.Y + (container.Height - h) / 2;
            g.DrawImage(img, x, y, w, h);
        }

        // ------------------------------ 按钮栏动画触发和绘制 ------------------------------
        private void StartButtonBarAnimation(bool expanding)
        {
            if (_isButtonBarAnimating) return;
            _isButtonBarAnimating = true;
            _buttonBarAnimDirection = expanding ? 1 : -1;
            _buttonBarAnimProgress = 0f;
            _buttonBarAnimation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(800),
                IsLooping = false,
                Tag = "ButtonBarAnim"
            };
            _buttonBarAnimation.OnUpdate = (p) =>
            {
                _buttonBarAnimProgress = (float)p;
                // 时钟透明度动画：收起时显示（alpha 从 0->255 或 255->0 取决于方向）
                if (expanding)  // 展开：时钟隐藏（255 -> 0）
                    _clockAlpha = 255f * (1 - (float)p);
                else            // 收起：时钟显示（0 -> 255）
                    _clockAlpha = 255f * (float)p;
                Invalidate();
            };
            _buttonBarAnimation.OnComplete = () =>
            {
                _isButtonBarAnimating = false;
                _buttonsExpanded = expanding;
                _buttonBarAnimation = null;
                // 确保透明度到达终点
                _clockAlpha = expanding ? 0f : 255f;
                Invalidate();
            };
            _buttonBarAnimation.Start();
            _animations.Add(_buttonBarAnimation);
        }

        private void DrawButtons(Graphics g, Rectangle barRect)
        {
            if (rotationMode)
            {
                int backY = barRect.Top + (barRect.Height - BTN_SQUARE_SIZE) / 2;
                backBtnRect = new Rectangle(35, backY, BTN_SQUARE_SIZE, BTN_SQUARE_SIZE);
                DrawSingleButton(g, backBtnRect, "返回", buttonIcons.ContainsKey("返回") ? buttonIcons["返回"] : null, _backPressed, "back");

                int btnY = barRect.Top + (barRect.Height - BTN_SQUARE_SIZE) / 2;
                int totalButtonsWidth = 3 * BTN_SQUARE_SIZE + 2 * 10;
                int startX = barRect.Right - totalButtonsWidth - 20;

                Point editPos = new Point(startX, btnY);
                Point historyPos = new Point(editPos.X + BTN_SQUARE_SIZE + 10, btnY);
                Point fullscreenPos = new Point(historyPos.X + BTN_SQUARE_SIZE + 10, btnY);
                editBtnRect = new Rectangle(editPos, new Size(BTN_SQUARE_SIZE, BTN_SQUARE_SIZE));
                historyBtnRect = new Rectangle(historyPos, new Size(BTN_SQUARE_SIZE, BTN_SQUARE_SIZE));
                fullscreenBtnRect = new Rectangle(fullscreenPos, new Size(BTN_SQUARE_SIZE, BTN_SQUARE_SIZE));

                DrawSingleButton(g, editBtnRect, editMode ? "完成" : "编辑", buttonIcons.ContainsKey(editMode ? "完成" : "编辑") ? buttonIcons[editMode ? "完成" : "编辑"] : null, _editPressed, "edit");
                DrawSingleButton(g, historyBtnRect, historyMode ? "返回" : "历史", buttonIcons.ContainsKey(historyMode ? "返回" : "历史") ? buttonIcons[historyMode ? "返回" : "历史"] : null, _historyPressed, "history");
                DrawSingleButton(g, fullscreenBtnRect, fullscreen ? "缩小" : "全屏", buttonIcons.ContainsKey(fullscreen ? "缩小" : "全屏") ? buttonIcons[fullscreen ? "缩小" : "全屏"] : null, _fullscreenPressed, "fullscreen");
                return;
            }

            int btnYNormal = barRect.Top + (barRect.Height - BTN_SQUARE_SIZE) / 2;
            int btnSpace = 10;

            var collapseButtons = new List<(string text, string key, Image icon)>
            {
                ("展开", "expand", buttonIcons.ContainsKey("展开") ? buttonIcons["展开"] : null),
                (editMode ? "完成" : "编辑", "edit", buttonIcons.ContainsKey(editMode ? "完成" : "编辑") ? buttonIcons[editMode ? "完成" : "编辑"] : null),
                (fullscreen ? "缩小" : "全屏", "fullscreen", buttonIcons.ContainsKey(fullscreen ? "缩小" : "全屏") ? buttonIcons[fullscreen ? "缩小" : "全屏"] : null),
                ("最小化", "minimize", buttonIcons.ContainsKey("最小化") ? buttonIcons["最小化"] : null)
            };
            var expandButtons = new List<(string text, string key, Image icon)>
            {
                ("收起", "collapse", buttonIcons.ContainsKey("收起") ? buttonIcons["收起"] : null),
                ("设置", "settings", buttonIcons.ContainsKey("设置") ? buttonIcons["设置"] : null),
                ("轮换", "rotate", buttonIcons.ContainsKey("轮换") ? buttonIcons["轮换"] : null),
                ("卡片管理", "manage", manageCardImage),
                ("导出", "export", buttonIcons.ContainsKey("导出") ? buttonIcons["导出"] : null),
                (editMode ? "完成" : "编辑", "edit", buttonIcons.ContainsKey(editMode ? "完成" : "编辑") ? buttonIcons[editMode ? "完成" : "编辑"] : null),
                (historyMode ? "返回" : "历史", "history", buttonIcons.ContainsKey(historyMode ? "返回" : "历史") ? buttonIcons[historyMode ? "返回" : "历史"] : null),
                (fullscreen ? "缩小" : "全屏", "fullscreen", buttonIcons.ContainsKey(fullscreen ? "缩小" : "全屏") ? buttonIcons[fullscreen ? "缩小" : "全屏"] : null),
                ("最小化", "minimize", buttonIcons.ContainsKey("最小化") ? buttonIcons["最小化"] : null),
                ("关闭", "close", buttonIcons.ContainsKey("关闭") ? buttonIcons["关闭"] : null)
            };

            int collapseTotalWidth = collapseButtons.Count * BTN_SQUARE_SIZE + (collapseButtons.Count - 1) * btnSpace;
            int collapseStartX = barRect.Right - collapseTotalWidth - 20;
            List<Rectangle> collapseRects = new List<Rectangle>();
            for (int i = 0; i < collapseButtons.Count; i++)
            {
                int x = collapseStartX + i * (BTN_SQUARE_SIZE + btnSpace);
                collapseRects.Add(new Rectangle(x, btnYNormal, BTN_SQUARE_SIZE, BTN_SQUARE_SIZE));
            }

            int expandTotalWidth = expandButtons.Count * BTN_SQUARE_SIZE + (expandButtons.Count - 1) * btnSpace;
            int expandStartX = barRect.Right - expandTotalWidth - 20;
            List<Rectangle> expandRects = new List<Rectangle>();
            for (int i = 0; i < expandButtons.Count; i++)
            {
                int x = expandStartX + i * (BTN_SQUARE_SIZE + btnSpace);
                expandRects.Add(new Rectangle(x, btnYNormal, BTN_SQUARE_SIZE, BTN_SQUARE_SIZE));
            }

            Action drawClock = () =>
            {
                if (!appConfig.ShowClock) return;
                if (rotationMode || editMode || _isCardEditingMode || historyMode) return;
                int clockAlphaInt = (int)_clockAlpha;
                if (clockAlphaInt <= 0) return;
                string hour = DateTime.Now.ToString("HH");
                string minute = DateTime.Now.ToString("mm");
                string second = DateTime.Now.ToString("ss");
                int colonAlpha;
                if (appConfig.ClockColonFlash)
                    colonAlpha = (DateTime.Now.Second % 2) == 0 ? 255 : 80;
                else
                    colonAlpha = 255;
                string fontName = appConfig.UseCustomFont ? appConfig.CustomFontName : "微软雅黑";
                using (Font hourFont = FontManager.GetFont(fontName, 28, FontStyle.Bold))
                using (Font secondFont = FontManager.GetFont(fontName, 14, FontStyle.Bold))
                using (Font colonFont = FontManager.GetFont(fontName, 28, FontStyle.Bold))
                {
                    SizeF hourSize = g.MeasureString(hour, hourFont);
                    SizeF colon1Size = g.MeasureString(":", colonFont);
                    SizeF minuteSize = g.MeasureString(minute, hourFont);
                    SizeF colon2Size = g.MeasureString(":", secondFont);
                    SizeF secondSize = g.MeasureString(second, secondFont);
                    int offset = 4;
                    float totalWidth = hourSize.Width + (colon1Size.Width - offset) + minuteSize.Width + (colon2Size.Width - offset) + secondSize.Width;
                    float x = (VIRTUAL_SIZE.Width - totalWidth) / 2;
                    float baseY = barRect.Y + (barRect.Height - hourFont.Height) / 2 + 2;
                    float secondY = baseY + hourFont.Height - secondFont.Height - 3;

                    using (Brush brush = new SolidBrush(Color.FromArgb(clockAlphaInt, TEXT_COLOR)))
                        g.DrawString(hour, hourFont, brush, x, baseY);
                    x += hourSize.Width;

                    int finalAlpha = colonAlpha * clockAlphaInt / 255;
                    using (Brush colonBrush = new SolidBrush(Color.FromArgb(finalAlpha, TEXT_COLOR)))
                        g.DrawString(":", colonFont, colonBrush, x - offset, baseY); // 第一个冒号（大字体）
                    x += colon1Size.Width - offset;

                    using (Brush brush = new SolidBrush(Color.FromArgb(clockAlphaInt, TEXT_COLOR)))
                        g.DrawString(minute, hourFont, brush, x, baseY);
                    x += minuteSize.Width;

                    using (Brush colonBrush = new SolidBrush(Color.FromArgb(finalAlpha, TEXT_COLOR)))
                        g.DrawString(":", secondFont, colonBrush, x - offset, secondY); // 第二个冒号（小字体，与秒钟底部对齐）
                    x += colon2Size.Width - offset;

                    using (Brush secondBrush = new SolidBrush(Color.FromArgb(clockAlphaInt, TEXT_COLOR)))
                        g.DrawString(second, secondFont, secondBrush, x, secondY);
                }
            };

            if (!_isButtonBarAnimating)
            {
                if (!_buttonsExpanded)
                {
                    for (int i = 0; i < collapseButtons.Count; i++)
                    {
                        var btn = collapseButtons[i];
                        DrawSingleButton(g, collapseRects[i], btn.text, btn.icon, GetPressedState(btn.key), btn.key);
                    }
                    expandBtnRect = collapseRects[0];
                    editBtnRect = collapseRects[1];
                    fullscreenBtnRect = collapseRects[2];
                    minimizeBtnRect = collapseRects[3];
                    drawClock();
                }
                else
                {
                    for (int i = 0; i < expandButtons.Count; i++)
                    {
                        var btn = expandButtons[i];
                        DrawSingleButton(g, expandRects[i], btn.text, btn.icon, GetPressedState(btn.key), btn.key);
                    }
                    collapseBtnRect = expandRects[0];
                    settingsBtnRect = expandRects[1];
                    rotateBtnRect = expandRects[2];
                    manageBtnRect = expandRects[3];
                    exportBtnRect = expandRects[4];
                    editBtnRect = expandRects[5];
                    historyBtnRect = expandRects[6];
                    fullscreenBtnRect = expandRects[7];
                    minimizeBtnRect = expandRects[8];
                    closeBtnRect = expandRects[9];
                }
                return;
            }

            _currentButtonRects.Clear();
            _currentButtonKeys.Clear();

            float t = _buttonBarAnimProgress;
            float easeT = t < 0.5f ? 4 * t * t * t : 1 - (float)Math.Pow(-2 * t + 2, 3) / 2;
            bool isExpanding = _buttonBarAnimDirection == 1;
            var movingKeys = new[] { "edit", "fullscreen", "minimize", "expand", "collapse" };

            foreach (string key in movingKeys)
            {
                int oldIndex = collapseButtons.FindIndex(b => b.key == key);
                int newIndex = expandButtons.FindIndex(b => b.key == key);
                Rectangle oldRect, newRect;
                if (oldIndex != -1 && newIndex != -1)
                {
                    oldRect = collapseRects[oldIndex];
                    newRect = expandRects[newIndex];
                }
                else if (oldIndex != -1 && key == "expand")
                {
                    oldRect = collapseRects[oldIndex];
                    int collapseIdx = expandButtons.FindIndex(b => b.key == "collapse");
                    newRect = expandRects[collapseIdx];
                }
                else if (newIndex != -1 && key == "collapse")
                {
                    newRect = expandRects[newIndex];
                    int expandIdx = collapseButtons.FindIndex(b => b.key == "expand");
                    oldRect = collapseRects[expandIdx];
                }
                else
                    continue;

                Rectangle currentRect;
                if (isExpanding)
                {
                    currentRect = new Rectangle(
                        (int)(oldRect.X + (newRect.X - oldRect.X) * easeT),
                        (int)(oldRect.Y + (newRect.Y - oldRect.Y) * easeT),
                        BTN_SQUARE_SIZE, BTN_SQUARE_SIZE);
                }
                else
                {
                    currentRect = new Rectangle(
                        (int)(newRect.X + (oldRect.X - newRect.X) * easeT),
                        (int)(newRect.Y + (oldRect.Y - newRect.Y) * easeT),
                        BTN_SQUARE_SIZE, BTN_SQUARE_SIZE);
                }
                var btnData = isExpanding ? expandButtons.FirstOrDefault(b => b.key == key) : collapseButtons.FirstOrDefault(b => b.key == key);
                if (btnData.text == null) continue;
                DrawSingleButton(g, currentRect, btnData.text, btnData.icon, GetPressedState(btnData.key), btnData.key);
                _currentButtonRects.Add(currentRect);
                _currentButtonKeys.Add(btnData.key);
            }

            if (isExpanding)
            {
                var onlyCollapse = collapseButtons.Where(b => !movingKeys.Contains(b.key)).ToList();
                foreach (var btn in onlyCollapse)
                {
                    int idx = collapseButtons.FindIndex(b => b.key == btn.key);
                    Rectangle rect = collapseRects[idx];
                    float alpha = 1f - easeT;
                    DrawSingleButton(g, rect, btn.text, btn.icon, GetPressedState(btn.key), btn.key, alpha);
                    _currentButtonRects.Add(rect);
                    _currentButtonKeys.Add(btn.key);
                }
                var onlyExpand = expandButtons.Where(b => !movingKeys.Contains(b.key)).ToList();
                foreach (var btn in onlyExpand)
                {
                    int idx = expandButtons.FindIndex(b => b.key == btn.key);
                    Rectangle rect = expandRects[idx];
                    float alpha = easeT;
                    DrawSingleButton(g, rect, btn.text, btn.icon, GetPressedState(btn.key), btn.key, alpha);
                    _currentButtonRects.Add(rect);
                    _currentButtonKeys.Add(btn.key);
                }
            }
            else
            {
                var onlyExpand = expandButtons.Where(b => !movingKeys.Contains(b.key)).ToList();
                foreach (var btn in onlyExpand)
                {
                    int idx = expandButtons.FindIndex(b => b.key == btn.key);
                    Rectangle rect = expandRects[idx];
                    float alpha = 1f - easeT;
                    DrawSingleButton(g, rect, btn.text, btn.icon, GetPressedState(btn.key), btn.key, alpha);
                    _currentButtonRects.Add(rect);
                    _currentButtonKeys.Add(btn.key);
                }
                var onlyCollapse = collapseButtons.Where(b => !movingKeys.Contains(b.key)).ToList();
                foreach (var btn in onlyCollapse)
                {
                    int idx = collapseButtons.FindIndex(b => b.key == btn.key);
                    Rectangle rect = collapseRects[idx];
                    float alpha = easeT;
                    DrawSingleButton(g, rect, btn.text, btn.icon, GetPressedState(btn.key), btn.key, alpha);
                    _currentButtonRects.Add(rect);
                    _currentButtonKeys.Add(btn.key);
                }
            }

            drawClock();
        }

        private bool GetPressedState(string key)
        {
            return key switch
            {
                "settings" => _settingsPressed,
                "rotate" => _rotatePressed,
                "edit" => _editPressed,
                "history" => _historyPressed,
                "fullscreen" => _fullscreenPressed,
                "minimize" => _minimizePressed,
                "close" => _closePressed,
                "expand" => _expandPressed,
                "collapse" => _collapsePressed,
                "export" => _exportPressed,
                "manage" => _managePressed,
                "addCard" => _addCardPressed,
                "back" => _backPressed,
                _ => false
            };
        }

        // ------------------------------ 网格绘制 ------------------------------
        private List<Rectangle> GetPageGridRects()
        {
            return gridRects;
        }

        private void DrawGrid(Graphics g)
        {
            float opacityFactor = appConfig.CardOpacity / 100f;
            List<Rectangle> pageRects = GetPageGridRects();
            int startIndex = _currentPage * CARDS_PER_PAGE;
            int visibleCardCount = Math.Min(CARDS_PER_PAGE, appConfig.CustomSubjects.Count - startIndex);
            _deleteButtonRects.Clear();

            // 预创建粗体字体（避免循环内重复创建）
            using (Font subjectBoldFont = new Font(font22.FontFamily, font22.Size, FontStyle.Bold))
            using (Font dueBoldFont = new Font(font22.FontFamily, font22.Size, FontStyle.Bold))
            {
                for (int i = 0; i < visibleCardCount; i++)
                {
                    int cardIndex = startIndex + i;
                    string subject = appConfig.CustomSubjects[cardIndex];
                    Rectangle rect = pageRects[i];

                    float offsetX = 0;
                    if (_flyOutProgress.TryGetValue(cardIndex, out float flyOutOffset))
                        offsetX = flyOutOffset;
                    else if (_flyInOffsets.TryGetValue(cardIndex, out float flyInOffset))
                        offsetX = flyInOffset;

                    Rectangle drawRect = new Rectangle(rect.X + (int)offsetX, rect.Y, rect.Width, rect.Height);
                    float fadeAlpha = _fadeInProgress.ContainsKey(cardIndex) ? _fadeInProgress[cardIndex] : 1f;

                    Matrix originalTransform = g.Transform;
                    if (_isCardEditingMode && _shakeAngle != 0 && !_isDragging)
                    {
                        g.TranslateTransform(drawRect.X + drawRect.Width / 2, drawRect.Y + drawRect.Height / 2);
                        g.RotateTransform(_shakeAngle);
                        g.TranslateTransform(-(drawRect.X + drawRect.Width / 2), -(drawRect.Y + drawRect.Height / 2));
                    }

                    // 阴影
                    using (var shadowPath = CreateRoundedRectPath(new Rectangle(drawRect.X + 3, drawRect.Y + 3, drawRect.Width, drawRect.Height), ROUND_RADIUS))
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                        g.FillPath(shadowBrush, shadowPath);

                    int topAlpha = (int)(220 * opacityFactor * fadeAlpha);
                    int bottomAlpha = (int)(160 * opacityFactor * fadeAlpha);
                    using (var path = CreateRoundedRectPath(drawRect, ROUND_RADIUS))
                    using (var bgBrush = new LinearGradientBrush(drawRect, Color.FromArgb(topAlpha, 255, 255, 255), Color.FromArgb(bottomAlpha, 240, 240, 255), LinearGradientMode.Vertical))
                    {
                        if (_isDragging && _isCardEditingMode)
                        {
                            using (var grayBrush = new SolidBrush(Color.FromArgb(100, 40, 40, 40)))
                                g.FillPath(grayBrush, path);
                            if (cardIndex == _dragStartIndex)
                            {
                                using (var pen = new Pen(Color.Blue, 3))
                                    g.DrawPath(pen, path);
                            }
                            else
                            {
                                using (var pen = new Pen(Color.FromArgb(80, 80, 80), 1))
                                    g.DrawPath(pen, path);
                            }
                        }
                        else
                        {
                            // 卡片背景
                            g.FillPath(bgBrush, path);

                            // 边框（晚修高亮/闪烁）
                            string dueTime = homeworkData.DueTimes.ContainsKey(subject) ? homeworkData.DueTimes[subject] : "";
                            bool highlightActive = _activeEvenings.Contains(dueTime);
                            bool highlightFlash = _flashingEvenings.Contains(dueTime);
                            Color borderColor;
                            float borderWidth = 1;
                            if (_debugFlashing || highlightFlash)
                            {
                                using (var redBrush = new SolidBrush(Color.FromArgb(flashStep, 255, 0, 0)))
                                    g.FillPath(redBrush, path);
                                DrawLaserBorder(g, path, drawRect);
                                borderColor = Color.FromArgb(200, 255, 0, 0);
                            }
                            else if (highlightActive)
                            {
                                borderColor = SystemColors.Highlight;
                                borderWidth = 2;
                            }
                            else
                            {
                                borderColor = Color.FromArgb(100, 128, 128, 128);
                            }
                            using (var pen = new Pen(borderColor, borderWidth))
                                g.DrawPath(pen, path);

                            // 科目名称（加粗）
                            int topOffset = 50;
                            int subjectY = drawRect.Top + topOffset - subjectBoldFont.Height;
                            g.DrawString(subject, subjectBoldFont, new SolidBrush(TEXT_COLOR), drawRect.Left + 10, subjectY);
                            int lineY = drawRect.Top + topOffset;
                            g.DrawLine(Pens.Gray, drawRect.Left + 10, lineY, drawRect.Right - 10, lineY);

                            // 提交时间（加粗）
                            if (appConfig.ShowDueTime && !EditMode)
                            {
                                string dueTime2 = homeworkData.DueTimes.ContainsKey(subject) ? homeworkData.DueTimes[subject] : "";
                                string displayTime = string.IsNullOrEmpty(dueTime2) ? "无" : dueTime2;
                                string prefix = "提交时间：";
                                float prefixWidth = g.MeasureString(prefix, fontSmall).Width;
                                float timeWidth = g.MeasureString(displayTime, dueBoldFont).Width;
                                float totalWidth = prefixWidth + timeWidth;
                                int rightX = drawRect.Right - 10;
                                float startX = rightX - totalWidth;
                                int timeY = lineY - dueBoldFont.Height;
                                int prefixY = lineY - fontSmall.Height;
                                g.DrawString(prefix, fontSmall, new SolidBrush(TEXT_COLOR), startX, prefixY);
                                Brush timeBrush = GetDueTimeBrush(displayTime);
                                g.DrawString(displayTime, dueBoldFont, timeBrush, startX + prefixWidth, timeY);
                            }

                            // 作业内容区域
                            Rectangle textArea = new Rectangle(drawRect.Left + 10, drawRect.Top + topOffset + 10, drawRect.Width - 20, drawRect.Height - (topOffset + 20));
                            if (editingSubjectIndex == cardIndex && currentEditType == EditFieldType.Subject)
                            {
                                // 正在编辑科目名称
                            }
                            else if (homeworkData.Subjects.ContainsKey(subject) && !string.IsNullOrWhiteSpace(homeworkData.Subjects[subject]))
                            {
                                float scrollOffset = scrollOffsets.ContainsKey(cardIndex) ? scrollOffsets[cardIndex] : 0f;
                                DrawPlainTextInArea(g, homeworkData.Subjects[subject], textArea, font30, false, scrollOffset);
                            }
                            else
                            {
                                using (var textPath = CreateRoundedRectPath(textArea, ROUND_RADIUS))
                                using (var lightBrush = new SolidBrush(Color.FromArgb((int)(255 * opacityFactor * fadeAlpha), 255, 255, 255)))
                                    g.FillPath(lightBrush, textPath);
                                string hint = editMode ? "点我编辑作业" : "今天暂时没有此项作业";
                                g.DrawString(hint, hintFont, RED_SEMI, textArea, CenterStringFormat());
                            }
                        }
                    }

                    if (_isCardEditingMode && _shakeAngle != 0 && !_isDragging)
                        g.Transform = originalTransform;

                    // 删除按钮
                    if (_isCardEditingMode && !_isDragging)
                    {
                        int radius = ROUND_RADIUS;
                        int centerX = drawRect.Right - radius;
                        int centerY = drawRect.Top + radius;
                        int delRadius = 12;
                        Rectangle deleteRect = new Rectangle(centerX - delRadius, centerY - delRadius, delRadius * 2, delRadius * 2);
                        _deleteButtonRects.Add(deleteRect);
                        using (SolidBrush redBrush = new SolidBrush(Color.Red))
                            g.FillEllipse(redBrush, deleteRect);
                        using (Font font = new Font("微软雅黑", 12, FontStyle.Bold))
                        using (Brush whiteBrush = new SolidBrush(Color.White))
                        {
                            SizeF xSize = g.MeasureString("×", font);
                            float xOffset = (deleteRect.Width - xSize.Width) / 2;
                            float yOffset = (deleteRect.Height - xSize.Height) / 2;
                            g.DrawString("×", font, whiteBrush, deleteRect.X + xOffset, deleteRect.Y + yOffset);
                        }
                    }
                }
            }

            // 拖拽排序时的目标虚线框
            if (_isDragging && _isCardEditingMode && _dragOverIndex != -1 && _dragOverIndex != _dragStartIndex)
            {
                int startIdx = _currentPage * CARDS_PER_PAGE;
                int localIdx = _dragOverIndex - startIdx;
                if (localIdx >= 0 && localIdx < gridRects.Count)
                {
                    Rectangle rect = gridRects[localIdx];
                    float offsetX = 0;
                    if (_flyOutProgress.TryGetValue(_dragOverIndex, out float flyOff))
                        offsetX = flyOff;
                    else if (_flyInOffsets.TryGetValue(_dragOverIndex, out float flyInOff))
                        offsetX = flyInOff;
                    Rectangle drawRect = new Rectangle(rect.X + (int)offsetX, rect.Y, rect.Width, rect.Height);
                    using (var dashPen = new Pen(Color.Blue, 2) { DashStyle = DashStyle.Dash })
                    using (var outlinePath = CreateRoundedRectPath(drawRect, ROUND_RADIUS))
                        g.DrawPath(dashPen, outlinePath);
                }
            }

            // 卡片管理模式下绘制虚线卡片占位符（添加卡片）
            if (_isCardEditingMode && !_isDragging)
            {
                int realTotalPages = (int)Math.Ceiling(appConfig.CustomSubjects.Count / (double)CARDS_PER_PAGE);
                bool isVirtualPage = (_showVirtualAddCardPage && _currentPage == realTotalPages);
                if (isVirtualPage)
                {
                    if (gridRects.Count > 0)
                    {
                        Rectangle emptyRect = gridRects[0];
                        float offsetX = _virtualCardOffset + _virtualCardFlyOutOffset;
                        Rectangle drawRect = new Rectangle(emptyRect.X + (int)offsetX, emptyRect.Y, emptyRect.Width, emptyRect.Height);
                        using (var path = CreateRoundedRectPath(drawRect, ROUND_RADIUS))
                        {
                            using (var dashPen = new Pen(Color.Blue, 3) { DashStyle = DashStyle.Dash })
                                g.DrawPath(dashPen, path);
                            if (addCardImage != null)
                            {
                                int iconSize = 80;
                                int iconX = drawRect.X + (drawRect.Width - iconSize) / 2;
                                int iconY = drawRect.Y + (drawRect.Height - iconSize - 30) / 2;
                                g.DrawImage(addCardImage, iconX, iconY, iconSize, iconSize);
                            }
                            string addText = "添加卡片";
                            using (Font textFont = new Font("微软雅黑", 12, FontStyle.Bold))
                            using (Brush textBrush = new SolidBrush(Color.White))
                            {
                                SizeF textSize = g.MeasureString(addText, textFont);
                                float textX = drawRect.X + (drawRect.Width - textSize.Width) / 2;
                                float textY = drawRect.Y + drawRect.Height - 35;
                                g.DrawString(addText, textFont, textBrush, textX, textY);
                            }
                        }
                        _virtualAddCardRect = drawRect;
                    }
                }
                else
                {
                    int cardsOnThisPage = visibleCardCount;
                    if (cardsOnThisPage < CARDS_PER_PAGE)
                    {
                        int emptyLocalIndex = cardsOnThisPage;
                        if (emptyLocalIndex < pageRects.Count)
                        {
                            Rectangle emptyRect = pageRects[emptyLocalIndex];
                            float offsetX = _virtualCardOffset + _virtualCardFlyOutOffset;
                            Rectangle drawRect = new Rectangle(emptyRect.X + (int)offsetX, emptyRect.Y, emptyRect.Width, emptyRect.Height);
                            using (var path = CreateRoundedRectPath(drawRect, ROUND_RADIUS))
                            {
                                using (var dashPen = new Pen(Color.Blue, 3) { DashStyle = DashStyle.Dash })
                                    g.DrawPath(dashPen, path);
                                if (addCardImage != null)
                                {
                                    int iconSize = 80;
                                    int iconX = drawRect.X + (drawRect.Width - iconSize) / 2;
                                    int iconY = drawRect.Y + (drawRect.Height - iconSize - 30) / 2;
                                    g.DrawImage(addCardImage, iconX, iconY, iconSize, iconSize);
                                }
                                string addText = "添加卡片";
                                using (Font textFont = new Font("微软雅黑", 12, FontStyle.Bold))
                                using (Brush textBrush = new SolidBrush(Color.White))
                                {
                                    SizeF textSize = g.MeasureString(addText, textFont);
                                    float textX = drawRect.X + (drawRect.Width - textSize.Width) / 2;
                                    float textY = drawRect.Y + drawRect.Height - 35;
                                    g.DrawString(addText, textFont, textBrush, textX, textY);
                                }
                            }
                            _virtualAddCardRect = drawRect;
                        }
                        else
                        {
                            _virtualAddCardRect = Rectangle.Empty;
                        }
                    }
                    else
                    {
                        _virtualAddCardRect = Rectangle.Empty;
                    }
                }
            }

            // 分页箭头
            if (TotalPages > 1 && !rotationMode && leftArrowImage != null && rightArrowImage != null)
            {
                int arrowSize = 40;
                leftArrowRect = new Rectangle(20, VIRTUAL_SIZE.Height / 2 - arrowSize / 2, arrowSize, arrowSize);
                rightArrowRect = new Rectangle(VIRTUAL_SIZE.Width - 20 - arrowSize, VIRTUAL_SIZE.Height / 2 - arrowSize / 2, arrowSize, arrowSize);
                DrawImageCentered(g, leftArrowImage, leftArrowRect);
                DrawImageCentered(g, rightArrowImage, rightArrowRect);
            }
        }

        // ------------------------------ 滚动文本 ------------------------------
        private void DrawPlainTextInArea(Graphics g, string text, Rectangle area, Font font, bool center = false, float scrollOffset = 0f)
        {
            if (string.IsNullOrEmpty(text)) return;

            var lines = new List<string>();
            foreach (string para in text.Split('\n'))
            {
                if (string.IsNullOrEmpty(para)) continue;
                string[] words = para.Split(' ');
                string line = "";
                foreach (string word in words)
                {
                    string test = line + (line == "" ? "" : " ") + word;
                    if (g.MeasureString(test, font).Width <= area.Width)
                        line = test;
                    else
                    {
                        if (!string.IsNullOrEmpty(line)) lines.Add(line);
                        if (g.MeasureString(word, font).Width > area.Width)
                        {
                            string remaining = word;
                            while (remaining.Length > 0)
                            {
                                int take = 1;
                                while (take < remaining.Length && g.MeasureString(remaining.Substring(0, take), font).Width <= area.Width) take++;
                                while (take > 0 && g.MeasureString(remaining.Substring(0, take), font).Width > area.Width) take--;
                                if (take == 0) take = 1;
                                lines.Add(remaining.Substring(0, take));
                                remaining = remaining.Substring(take);
                            }
                            line = "";
                        }
                        else
                            line = word;
                    }
                }
                if (!string.IsNullOrEmpty(line)) lines.Add(line);
            }

            float lineHeight = font.GetHeight(g);
            var state = g.Save();
            g.SetClip(area);
            using (var brush = new SolidBrush(TEXT_COLOR))
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    float y = area.Top + i * lineHeight - scrollOffset;
                    if (center)
                    {
                        SizeF sz = g.MeasureString(lines[i], font);
                        g.DrawString(lines[i], font, brush, area.Left + (area.Width - sz.Width) / 2, y);
                    }
                    else
                    {
                        g.DrawString(lines[i], font, brush, area.Left + 5, y);
                    }
                }
            }
            g.Restore(state);
        }

        private void DrawLaserBorder(Graphics g, GraphicsPath path, Rectangle rect)
        {
            using (var pen = new Pen(Color.Red, 3) { DashStyle = DashStyle.Custom, DashPattern = new float[] { 8, 8 }, DashOffset = _laserOffset * 16 })
                g.DrawPath(pen, path);
        }

        private StringFormat CenterStringFormat() => new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        // ------------------------------ 时间提醒与闪烁 ------------------------------
        private void CheckEveningClassStates()
        {
            if (!appConfig.ShowDueTime || appConfig.EveningClassTimes == null || appConfig.EveningClassTimes.Count == 0)
            {
                if (_activeEvenings.Count > 0 || _flashingEvenings.Count > 0 || _grayEvenings.Count > 0)
                {
                    _activeEvenings.Clear();
                    _flashingEvenings.Clear();
                    _grayEvenings.Clear();
                    StopFlashingIfNeeded();
                    Invalidate();
                }
                return;
            }

            DateTime now = DateTime.Now;
            var newActive = new List<string>();
            var newFlashing = new List<string>();
            var newGray = new List<string>();
            bool newFlashingExists = false;

            for (int i = 0; i < appConfig.EveningClassTimes.Count; i++)
            {
                var time = appConfig.EveningClassTimes[i];
                string eveningName = $"晚修{i + 1}";
                if (DateTime.TryParse(time.Start, out DateTime start) && DateTime.TryParse(time.End, out DateTime end))
                {
                    DateTime startToday = DateTime.Today.Add(start.TimeOfDay);
                    DateTime endToday = DateTime.Today.Add(end.TimeOfDay);
                    if (now >= startToday && now < endToday) newActive.Add(eveningName);
                    else if (now >= endToday.AddMinutes(-2) && now <= endToday.AddMinutes(2)) { newFlashing.Add(eveningName); newFlashingExists = true; }
                    else if (now > endToday.AddMinutes(2)) newGray.Add(eveningName);
                }
            }

            bool changed = !_activeEvenings.SequenceEqual(newActive) || !_flashingEvenings.SequenceEqual(newFlashing) || !_grayEvenings.SequenceEqual(newGray);
            if (changed)
            {
                _activeEvenings = newActive;
                _flashingEvenings = newFlashing;
                _grayEvenings = newGray;
                if (_flashingEvenings.Count > 0 && !_debugFlashing) StartFlashing();
                else if (_flashingEvenings.Count == 0 && !_debugFlashing) StopFlashingIfNeeded();
            }

            // 每秒刷新界面，使进度条实时更新（即使晚修状态未变化，时间也在流逝）
            Invalidate();
        }

        private void StartFlashing() { if (!flashTimer.Enabled) { flashStartTime = DateTime.Now; flashTimer.Start(); } }
        private void StopFlashingIfNeeded() { if (_flashingEvenings.Count == 0 && !_debugFlashing) { flashTimer.Stop(); Invalidate(); } }
        private void FlashTimer_Tick(object sender, EventArgs e)
        {
            if (_debugFlashing && (DateTime.Now - _debugFlashStartTime).TotalSeconds > FLASH_DURATION) { _debugFlashing = false; StopFlashingIfNeeded(); }
            double angle = (DateTime.Now - flashStartTime).TotalMilliseconds / 500.0;
            flashStep = (int)((Math.Sin(angle) + 1) * 60);
            _laserOffset += 0.1f;
            if (_laserOffset >= 1f) _laserOffset -= 1f;
            Invalidate();
        }
        public void StartDebugFlashing() { _debugFlashing = true; _debugFlashStartTime = DateTime.Now; StartFlashing(); Invalidate(); }
        public void StopDebugFlashing() { _debugFlashing = false; StopFlashingIfNeeded(); }

        // ------------------------------ 滚动定时器 ------------------------------
        private void ScrollTimer_Tick(object sender, EventArgs e)
        {
            if (editMode || _isCardEditingMode) return;

            bool needRedraw = false;
            float speed = appConfig.ScrollSpeed * 0.05f;
            float epsilon = 0.01f;
            var indices = rotationMode ? new List<int> { rotationIndex } : Enumerable.Range(0, appConfig.CustomSubjects.Count).ToList();

            foreach (int i in indices)
            {
                string subject = appConfig.CustomSubjects[i];
                string content = homeworkData.Subjects.ContainsKey(subject) ? homeworkData.Subjects[subject] : "";
                if (string.IsNullOrWhiteSpace(content)) continue;

                Rectangle textArea = GetContentArea(i);
                float contentHeight = MeasureTextHeight(content, font30, textArea.Width);
                if (contentHeight <= textArea.Height) continue;

                if (!scrollOffsets.ContainsKey(i)) scrollOffsets[i] = 0f;
                if (!scrollPaused.ContainsKey(i)) scrollPaused[i] = false;
                if (!pauseStartTime.ContainsKey(i)) pauseStartTime[i] = DateTime.Now;
                if (speed <= 0) continue;

                if (scrollPaused[i])
                {
                    if ((DateTime.Now - pauseStartTime[i]).TotalSeconds >= SCROLL_PAUSE_SECONDS)
                    {
                        if (scrollOffsets[i] >= contentHeight - textArea.Height - epsilon)
                        {
                            scrollOffsets[i] = 0f;
                            pauseStartTime[i] = DateTime.Now;
                        }
                        else { scrollPaused[i] = false; scrollOffsets[i] = speed; }
                    }
                }
                else
                {
                    if (scrollOffsets[i] <= epsilon) { scrollPaused[i] = true; pauseStartTime[i] = DateTime.Now; }
                    else
                    {
                        scrollOffsets[i] += speed;
                        if (scrollOffsets[i] >= contentHeight - textArea.Height - epsilon)
                        {
                            scrollOffsets[i] = contentHeight - textArea.Height;
                            scrollPaused[i] = true;
                            pauseStartTime[i] = DateTime.Now;
                        }
                    }
                }
                needRedraw = true;
            }
            if (needRedraw) Invalidate();
        }

        private float MeasureTextHeight(string text, Font font, int maxWidth) { using (var g = CreateGraphics()) return g.MeasureString(text, font, maxWidth).Height; }
        private Rectangle GetContentArea(int subjectIndex)
        {
            if (rotationMode)
            {
                return Rectangle.Empty;
            }
            Rectangle rect = GetPageCardRect(subjectIndex);
            return new Rectangle(rect.Left + 10, rect.Top + 60, rect.Width - 20, rect.Height - 70);
        }

        // ------------------------------ 编辑模式属性 ------------------------------
        private bool EditMode
        {
            get => editMode;
            set
            {
                if (editMode != value)
                {
                    editMode = value;
                    if (editMode)
                    {
                        CreateTimeComboBoxes();
                        scrollOffsets.Clear();
                        scrollPaused.Clear();
                        pauseStartTime.Clear();
                    }
                    else
                    {
                        DestroyTimeComboBoxes();
                        if (editingSubjectIndex != -1) FinishInlineEdit();
                        scrollOffsets.Clear();
                        scrollPaused.Clear();
                        pauseStartTime.Clear();
                    }
                    Invalidate();
                }
            }
        }

        private void FinishInlineEdit()
        {
            if (_isEditingFinishing) return;
            if (inlineEditControl == null) return;
            _isEditingFinishing = true;
            try
            {
                if (editingSubjectIndex != -1 && inlineEditControl is TextBox textBox &&
                    editingSubjectIndex >= 0 && editingSubjectIndex < appConfig.CustomSubjects.Count)
                {
                    // 移除事件，防止再次触发
                    textBox.LostFocus -= InlineEdit_LostFocus;
                    textBox.KeyDown -= InlineEdit_KeyDown;
                    // 只处理作业内容编辑
                    if (currentEditType == EditFieldType.None)
                    {
                        string subject = appConfig.CustomSubjects[editingSubjectIndex];
                        if (homeworkData.Subjects.ContainsKey(subject))
                            homeworkData.Subjects[subject] = textBox.Text;
                        SaveHomeworkData();
                    }
                }
                // 安全释放控件
                if (inlineEditControl != null && !inlineEditControl.IsDisposed)
                {
                    Controls.Remove(inlineEditControl);
                    inlineEditControl.Dispose();
                }
                inlineEditControl = null;
                editingSubjectIndex = -1;
                currentEditType = EditFieldType.None;
                Invalidate();
            }
            finally
            {
                _isEditingFinishing = false;
            }
        }

        private void CancelInlineEdit()
        {
            if (editingSubjectIndex == -1 || inlineEditControl == null) return;
            Controls.Remove(inlineEditControl);
            inlineEditControl.Dispose();
            inlineEditControl = null;
            editingSubjectIndex = -1;
            currentEditType = EditFieldType.None;
            Invalidate();
        }

        // ------------------------------ 提交时间下拉框管理 ------------------------------
        private void CreateTimeComboBoxes()
        {
            DestroyTimeComboBoxes();
            for (int i = 0; i < appConfig.CustomSubjects.Count; i++)
            {
                string subject = appConfig.CustomSubjects[i];
                Rectangle virtualRect = GetDueTimeRect(i);
                if (virtualRect.IsEmpty) continue;
                Point screenLoc = MapToScreen(virtualRect.Location);
                Size screenSize = new Size((int)(virtualRect.Width * scaleFactor), (int)(virtualRect.Height * scaleFactor));
                string currentValue = homeworkData.DueTimes.ContainsKey(subject) ? homeworkData.DueTimes[subject] : "";

                var combo = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = screenLoc,
                    Size = new Size(screenSize.Width, 25),
                    Font = font22,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(40, 40, 40),
                    ForeColor = Color.White,
                    DrawMode = DrawMode.OwnerDrawFixed,
                    Tag = i
                };
                for (int j = 1; j <= appConfig.EveningClassCount; j++) combo.Items.Add($"晚修{j}");
                combo.Items.Add("无");
                int selectedIndex = combo.Items.IndexOf(currentValue);
                if (selectedIndex < 0) selectedIndex = appConfig.EveningClassCount;
                combo.SelectedIndex = selectedIndex;
                combo.DrawItem += TimeComboBox_DrawItem;
                combo.SelectedIndexChanged += TimeComboBox_SelectedIndexChanged;
                Controls.Add(combo);
                timeComboBoxes.Add(combo);
            }
        }

        private void DestroyTimeComboBoxes()
        {
            foreach (var combo in timeComboBoxes)
            {
                combo.SelectedIndexChanged -= TimeComboBox_SelectedIndexChanged;
                combo.DrawItem -= TimeComboBox_DrawItem;
                Controls.Remove(combo);
                combo.Dispose();
            }
            timeComboBoxes.Clear();
        }

        private void TimeComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var combo = sender as ComboBox;
            string text = combo.Items[e.Index].ToString();
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bgColor = selected ? Color.FromArgb(80, 80, 80) : Color.FromArgb(40, 40, 40);
            using (var bgBrush = new SolidBrush(bgColor))
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            using (var textBrush = new SolidBrush(Color.White))
                e.Graphics.DrawString(text, combo.Font, textBrush, e.Bounds.Left + 5, e.Bounds.Top + 2);
            e.DrawFocusRectangle();
        }

        private void TimeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var combo = sender as ComboBox;
            int subjectIndex = (int)combo.Tag;
            string subject = appConfig.CustomSubjects[subjectIndex];
            homeworkData.DueTimes[subject] = combo.SelectedItem?.ToString() ?? "";
            SaveHomeworkData();
        }

        // ------------------------------ 动态控件管理 ------------------------------
        private void DestroyAllDynamicControls()
        {
            if (inlineEditControl != null && !inlineEditControl.IsDisposed)
            {
                try
                {
                    Controls.Remove(inlineEditControl);
                    inlineEditControl.Dispose();
                }
                catch { }
                inlineEditControl = null;
                editingSubjectIndex = -1;
                currentEditType = EditFieldType.None;
            }

            if (timeComboBoxes != null)
            {
                foreach (var combo in timeComboBoxes.ToList())
                {
                    if (combo != null && !combo.IsDisposed)
                    {
                        try
                        {
                            combo.SelectedIndexChanged -= TimeComboBox_SelectedIndexChanged;
                            combo.DrawItem -= TimeComboBox_DrawItem;
                            Controls.Remove(combo);
                            combo.Dispose();
                        }
                        catch { }
                    }
                }
                timeComboBoxes.Clear();
            }
        }

        private void RecreateDynamicControls()
        {
            if (EditMode && !rotationMode && !IsDisposed)
            {
                CreateTimeComboBoxes();
            }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            ApplyBackgroundEffect(appConfig.BackgroundEffect);
            
            if (appConfig.UpdatePending == 1)
            {
                string upgradePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HomeworkViewerUpgrader");
                if (Directory.Exists(upgradePath)) try { Directory.Delete(upgradePath, true); } catch { }
                appConfig.UpdatePending = 0;
                appConfig.Save();
            }
            
            var version = Environment.OSVersion.Version;
            bool isWin7 = (version.Major == 6 && version.Minor == 1);
            
            // Win10 特定版本（Build < 22000）启用无边框全屏（沉浸式，全屏无任务栏）
            if (!isWin7 && version.Major == 10 && version.Minor == 0 && version.Build < 22000)
            {
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                fullscreen = true;
                UpdateScale();
            }
            // Win7 下启用传统全屏（最大化但保留边框，任务栏可见）
            else if (isWin7)
            {
                WindowState = FormWindowState.Maximized;
                fullscreen = true;
                UpdateScale();
            }
            
            InitializeDrawerPanel();
            StartCardFlyInAnimation(1);
            
            _clockAlpha = _buttonsExpanded ? 0f : 255f;
            
            if (appConfig.FirstRunCompleted == 0)
            {
                using (var wizard = new FirstRunWizard(appConfig))
                {
                    if (wizard.ShowDialog() == DialogResult.OK)
                    {
                        appConfig.FirstRunCompleted = 1;
                        appConfig.Save();
                        ApplySettings(appConfig);
                    }
                }
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e) { }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            rotationTimer?.Dispose();
            timeCheckTimer?.Dispose();
            flashTimer?.Dispose();
            scrollTimer?.Dispose();
            _animationTimer?.Dispose();
            _autoPageTimer?.Dispose();
            _shakeTimer?.Dispose();

            font12?.Dispose(); font20?.Dispose(); font24?.Dispose(); font30?.Dispose();
            font22?.Dispose(); font36?.Dispose(); hintFont?.Dispose(); buttonFont?.Dispose(); fontSmall?.Dispose();

            RED_SEMI?.Dispose(); ORANGE_SEMI?.Dispose(); GREEN_SEMI?.Dispose();
            BLUE_SEMI?.Dispose(); PURPLE_SEMI?.Dispose(); DARKORANGE_SEMI?.Dispose();

            _cachedBackground?.Dispose();
            _tileBrush?.Dispose();
            _backgroundImage?.Dispose();

            // 释放自定义字体
            _customFontChinese?.Dispose();
            _customFontEnglish?.Dispose();
            _customFontNumber?.Dispose();
            _customContentFontChinese?.Dispose();
            _customContentFontEnglish?.Dispose();
            _customContentFontNumber?.Dispose();

            base.OnFormClosed(e);
        }

        // ------------------------------ OnMouseClick 完整版 ------------------------------
        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            Point v = MapToVirtual(e.Location);
            int x = v.X, y = v.Y;

            if (_drawerOpen && _drawerPanel != null && !_drawerPanel.Bounds.Contains(e.Location))
            {
                CloseDrawer();
                return;
            }

            if (editingSubjectIndex != -1) FinishInlineEdit();

            // 非轮播模式的按钮处理（轮播模式下按钮栏依然有效）
            if (!_buttonsExpanded)
            {
                if (expandBtnRect.Contains(x, y))
                {
                    StartButtonBarAnimation(true);
                    return;
                }
                else if (editBtnRect.Contains(x, y))
                {
                    if (EditMode) SaveHomeworkData();
                    EditMode = !EditMode;
                    return;
                }
                else if (fullscreenBtnRect.Contains(x, y))
                {
                    if (EditMode) EditMode = false;
                    ToggleFullscreen();
                    return;
                }
                else if (minimizeBtnRect.Contains(x, y))
                {
                    WindowState = FormWindowState.Minimized;
                    return;
                }
            }
            else
            {
                if (_isCardEditingMode)
                {
                    if (manageBtnRect.Contains(x, y))
                    {
                        ExitCardEditMode();
                        return;
                    }
                    if (addCardBtnRect.Contains(x, y))
                    {
                        AddNewCard();
                        return;
                    }
                }
                else
                {
                    if (collapseBtnRect.Contains(x, y))
                    {
                        StartButtonBarAnimation(false);
                        return;
                    }
                    else if (settingsBtnRect.Contains(x, y))
                    {
                        using (var settingsForm = new SettingsForm(this))
                        {
                            settingsForm.ShowDialog();
                        }
                        return;
                    }
                    else if (rotateBtnRect.Contains(x, y))
                    {
                        if (rotationMode)
                        {
                            CloseDrawer();
                        }
                        else
                        {
                            bool hasContent = appConfig.CustomSubjects.Any(subj => homeworkData.Subjects.ContainsKey(subj) && !string.IsNullOrWhiteSpace(homeworkData.Subjects[subj]));
                            if (!hasContent)
                            {
                                MessageBox.Show("所有科目都没有作业内容，无法进入轮播模式", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            rotationMode = true;
                            // 设置为第一个有作业的科目
                            var nonEmpty = new List<int>();
                            for (int i = 0; i < appConfig.CustomSubjects.Count; i++)
                                if (homeworkData.Subjects.ContainsKey(appConfig.CustomSubjects[i]) && !string.IsNullOrWhiteSpace(homeworkData.Subjects[appConfig.CustomSubjects[i]]))
                                    nonEmpty.Add(i);
                            rotationIndex = nonEmpty.Count > 0 ? nonEmpty[0] : 0;
                            rotationTimer.Start();
                            int targetHeight = this.ClientSize.Height / 2;
                            OpenDrawer(targetHeight);
                            Invalidate();
                        }
                        return;
                    }
                    else if (manageBtnRect.Contains(x, y))
                    {
                        if (EditMode) EditMode = false;
                        if (rotationMode) { rotationMode = false; rotationTimer.Stop(); CloseDrawer(); }
                        if (historyMode) { historyMode = false; historyDate = null; LoadHomeworkData(currentDate); }
                        if (editingSubjectIndex != -1) FinishInlineEdit();
                        EnterCardEditMode();
                        Invalidate();
                        return;
                    }
                    else if (exportBtnRect.Contains(x, y))
                    {
                        ShowExportDialog();
                        return;
                    }
                    else if (editBtnRect.Contains(x, y))
                    {
                        if (EditMode) SaveHomeworkData();
                        EditMode = !EditMode;
                        return;
                    }
                    else if (historyBtnRect.Contains(x, y))
                    {
                        if (historyMode)
                        {
                            historyMode = false;
                            historyDate = null;
                            LoadHomeworkData(currentDate);
                            Invalidate();
                        }
                        else
                        {
                            using (var dlg = new HistoryDialog())
                            {
                                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.SelectedDate.HasValue)
                                {
                                    historyDate = dlg.SelectedDate.Value;
                                    LoadHomeworkData(historyDate.Value);
                                    historyMode = true;
                                    Invalidate();
                                }
                            }
                        }
                        return;
                    }
                    else if (minimizeBtnRect.Contains(x, y))
                    {
                        WindowState = FormWindowState.Minimized;
                        return;
                    }
                    else if (closeBtnRect.Contains(x, y))
                    {
                        Application.Exit();
                        return;
                    }
                    else if (fullscreenBtnRect.Contains(x, y))
                    {
                        if (EditMode) EditMode = false;
                        ToggleFullscreen();
                        return;
                    }
                }
            }

            // 分页箭头（内联编辑时禁用）
            if (TotalPages > 1 && inlineEditControl == null)
            {
                int arrowSize = 40;
                Rectangle leftArrow = new Rectangle(20, VIRTUAL_SIZE.Height / 2 - arrowSize / 2, arrowSize, arrowSize);
                Rectangle rightArrow = new Rectangle(VIRTUAL_SIZE.Width - 20 - arrowSize, VIRTUAL_SIZE.Height / 2 - arrowSize / 2, arrowSize, arrowSize);
                if (leftArrow.Contains(x, y) && leftArrowImage != null)
                {
                    int newPage = (_currentPage - 1 + TotalPages) % TotalPages;
                    SwitchToPage(newPage, 1);
                    return;
                }
                if (rightArrow.Contains(x, y) && rightArrowImage != null)
                {
                    int newPage = (_currentPage + 1) % TotalPages;
                    SwitchToPage(newPage, -1);
                    return;
                }
            }

            // 卡片管理模式下删除、编辑科目名、添加卡片
            if (_isCardEditingMode && !rotationMode)
            {
                for (int i = 0; i < _deleteButtonRects.Count; i++)
                {
                    if (_deleteButtonRects[i].Contains(x, y))
                    {
                        int cardIdx = (_currentPage * CARDS_PER_PAGE) + i;
                        if (cardIdx >= 0 && cardIdx < appConfig.CustomSubjects.Count)
                            DeleteCard(cardIdx);
                        return;
                    }
                }

                List<Rectangle> pageRects = GetPageGridRects();
                int baseIndex = _currentPage * CARDS_PER_PAGE;
                for (int i = 0; i < pageRects.Count; i++)
                {
                    int cardIndex = baseIndex + i;
                    if (cardIndex >= appConfig.CustomSubjects.Count) break;
                    Rectangle rect = pageRects[i];
                    Font subjectFont = font22;
                    int topOffset = 50;
                    Rectangle nameRect = new Rectangle(rect.Left + 10, rect.Top + topOffset - subjectFont.Height, rect.Width - 20, subjectFont.Height);
                    if (nameRect.Contains(x, y))
                    {
                        StartInlineEditForSubjectName(cardIndex, nameRect);
                        return;
                    }
                }

                if (_virtualAddCardRect.Contains(x, y))
                {
                    AddNewCard();
                    return;
                }
            }

            // 常规编辑模式（作业内容编辑）
            if (EditMode && !rotationMode && !_isCardEditingMode)
            {
                List<Rectangle> pageRects = GetPageGridRects();
                int startIndex = _currentPage * CARDS_PER_PAGE;
                for (int i = 0; i < pageRects.Count; i++)
                {
                    int cardIndex = startIndex + i;
                    if (cardIndex >= appConfig.CustomSubjects.Count) break;
                    Rectangle rect = pageRects[i];
                    Rectangle subjectArea = new Rectangle(rect.Left + 10, rect.Top + 60, rect.Width - 20, rect.Height - 70);
                    if (subjectArea.Contains(x, y))
                    {
                        if (!ManagementHelper.CanEdit(appConfig))
                        {
                            MessageBox.Show("当前已由管理端控制，无法编辑作业。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        StartInlineEdit(cardIndex, EditFieldType.None, subjectArea);
                        break;
                    }
                }
            }
        }

        // ------------------------------ 内联编辑辅助方法 ------------------------------
        /// <summary>
        /// 在指定位置绘制分段文本（支持中/英/数字体）
        /// </summary>
        private void DrawRotationContent(Graphics g, string subject, string content)
        {
            using (Brush textBrush = new SolidBrush(TEXT_COLOR))
            using (Font titleFont = new Font("微软雅黑", 24, FontStyle.Bold))
            using (Font contentFont = new Font("微软雅黑", 16))
            {
                int padding = 60;
                Rectangle drawRect = new Rectangle(padding, 20, _drawerPanel.Width - padding * 2, _drawerPanel.Height - 80);
                SizeF titleSize = g.MeasureString(subject, titleFont);
                float titleX = (_drawerPanel.Width - titleSize.Width) / 2;
                g.DrawString(subject, titleFont, textBrush, titleX, drawRect.Y);
                Rectangle contentRect = new Rectangle(drawRect.X, drawRect.Y + (int)titleSize.Height + 20, drawRect.Width, drawRect.Height - (int)titleSize.Height - 40);
                DrawPlainTextInArea(g, content, contentRect, contentFont, true, 0);
            }
        }
        private void StartInlineEdit(int subjectIndex, EditFieldType fieldType, Rectangle fieldRect)
        {
            if (editingSubjectIndex != -1) FinishInlineEdit();
            editingSubjectIndex = subjectIndex;
            currentEditType = EditFieldType.None;  // 作业内容编辑

            string subject = appConfig.CustomSubjects[subjectIndex];
            Point screenLoc = MapToScreen(fieldRect.Location);
            Size screenSize = new Size((int)(fieldRect.Width * scaleFactor), (int)(fieldRect.Height * scaleFactor));

            string currentText = homeworkData.Subjects.ContainsKey(subject) ? homeworkData.Subjects[subject] : "";
            Font editFont = font30;
            if (fullscreen)
            {
                int newLevel = appConfig.FontSizeLevel + 1;
                if (newLevel > 2) newLevel = 2;
                float scale = fontScales[newLevel];
                string fontName = appConfig.UseCustomFont ? appConfig.CustomFontName : "微软雅黑";
                editFont = new Font(fontName, 20 * scale);
            }

            var textBox = new TextBox
            {
                Multiline = true,
                WordWrap = true,
                ScrollBars = ScrollBars.None,  // 初始无滚动条
                Location = screenLoc,
                Size = screenSize,
                Text = currentText,
                Font = editFont,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White
            };

            // 动态判断是否需要垂直滚动条
            Size proposedSize = new Size(textBox.Width, int.MaxValue);
            int neededHeight = TextRenderer.MeasureText(currentText, editFont, proposedSize, TextFormatFlags.WordBreak).Height;
            if (neededHeight > textBox.Height)
            {
                textBox.ScrollBars = ScrollBars.Vertical;
            }

            textBox.LostFocus += InlineEdit_LostFocus;
            textBox.KeyDown += InlineEdit_KeyDown;
            inlineEditControl = textBox;
            Controls.Add(inlineEditControl);
            inlineEditControl.Focus();
        }

        private void InlineEdit_LostFocus(object sender, EventArgs e) => FinishInlineEdit();

        private void InlineEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && e.Control) { e.SuppressKeyPress = true; FinishInlineEdit(); }
            else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; CancelInlineEdit(); }
        }

        private void StartInlineEditForSubjectName(int subjectIndex, Rectangle nameRect)
        {
            if (editingSubjectIndex != -1) FinishInlineEdit();
            editingSubjectIndex = subjectIndex;
            currentEditType = EditFieldType.Subject;

            string currentName = appConfig.CustomSubjects[subjectIndex];
            Point screenLoc = MapToScreen(nameRect.Location);
            Size screenSize = new Size((int)(nameRect.Width * scaleFactor), (int)(nameRect.Height * scaleFactor));

            var textBox = new TextBox
            {
                Location = screenLoc,
                Size = screenSize,
                Text = currentName,
                Font = font22,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White
            };
            // 科目名称编辑的失焦事件绑定到 FinishEditingSubjectName
            textBox.LostFocus += (s, ev) => FinishEditingSubjectName();
            textBox.KeyDown += (s, ev) =>
            {
                if (ev.KeyCode == Keys.Enter)
                {
                    FinishEditingSubjectName();
                    ev.SuppressKeyPress = true;
                }
                else if (ev.KeyCode == Keys.Escape)
                {
                    CancelEditingSubjectName();
                    ev.SuppressKeyPress = true;
                }
            };
            inlineEditControl = textBox;
            Controls.Add(inlineEditControl);
            inlineEditControl.Focus();
        }

        private void FinishEditingSubjectName()
        {
            if (_isEditingFinishing) return;
            if (editingSubjectIndex == -1 || inlineEditControl == null || inlineEditControl.IsDisposed) return;
            _isEditingFinishing = true;
            try
            {
                TextBox textBox = inlineEditControl as TextBox;
                string newName = textBox?.Text.Trim();
                int targetIndex = editingSubjectIndex;
                string oldName = targetIndex >= 0 && targetIndex < appConfig.CustomSubjects.Count ? appConfig.CustomSubjects[targetIndex] : null;
                
                // 先清理控件（避免重复进入）
                if (inlineEditControl != null && !inlineEditControl.IsDisposed)
                {
                    Controls.Remove(inlineEditControl);
                    inlineEditControl.Dispose();
                }
                inlineEditControl = null;
                editingSubjectIndex = -1;
                currentEditType = EditFieldType.None;
                Invalidate();

                // 执行重命名
                if (!string.IsNullOrEmpty(newName) && newName != oldName && oldName != null)
                {
                    RenameCard(targetIndex, newName);
                }
            }
            finally
            {
                _isEditingFinishing = false;
            }
        }

        private void CancelEditingSubjectName()
        {
            if (_isCleaningUp) return;
            if (editingSubjectIndex == -1 || inlineEditControl == null) return;
            _isCleaningUp = true;
            CleanupSubjectEdit();
            _isCleaningUp = false;
        }

        private void CleanupSubjectEdit()
        {
            if (inlineEditControl != null && !inlineEditControl.IsDisposed)
            {
                Controls.Remove(inlineEditControl);
                inlineEditControl.Dispose();
                inlineEditControl = null;
            }
            editingSubjectIndex = -1;
            currentEditType = EditFieldType.None;
            Invalidate();
        }

        private void ShowExportDialog()
        {
            Form exportDlg = new Form()
            {
                Text = "导出作业",
                Size = new Size(400, 220),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            Label lblFormat = new Label() { Text = "导出格式：", Location = new Point(20, 20), AutoSize = true, ForeColor = Color.White };
            ComboBox cmbFormat = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(120, 17),
                Width = 120,
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbFormat.Items.AddRange(new object[] { "TXT 文本文件 (*.txt)", "HTML 网页文件 (*.html)", "JPG 图片 (*.jpg)" });
            cmbFormat.SelectedIndex = 0;

            Button btnOK = new Button()
            {
                Text = "导出",
                Location = new Point(180, 130),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            Button btnCancel = new Button()
            {
                Text = "取消",
                Location = new Point(280, 130),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            exportDlg.Controls.Add(lblFormat);
            exportDlg.Controls.Add(cmbFormat);
            exportDlg.Controls.Add(btnOK);
            exportDlg.Controls.Add(btnCancel);

            btnOK.Click += (s, e) =>
            {
                string selected = cmbFormat.SelectedItem.ToString();
                string filter = "";
                string extension = "";
                if (selected.StartsWith("TXT")) { filter = "文本文件|*.txt"; extension = ".txt"; }
                else if (selected.StartsWith("HTML")) { filter = "网页文件|*.html"; extension = ".html"; }
                else if (selected.StartsWith("JPG")) { filter = "图片文件|*.jpg"; extension = ".jpg"; }

                SaveFileDialog sfd = new SaveFileDialog()
                {
                    Title = "保存导出文件",
                    Filter = filter,
                    DefaultExt = extension,
                    FileName = $"作业_{currentDate:yyyyMMdd}"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string filePath = sfd.FileName;
                    try
                    {
                        DateTime exportDate = historyMode && historyDate.HasValue ? historyDate.Value : currentDate;
                        if (selected.StartsWith("TXT"))
                        {
                            ExportHelper.ExportToTxt(homeworkData, exportDate, filePath, false);
                            MessageBox.Show($"导出成功：{filePath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else if (selected.StartsWith("HTML"))
                        {
                            ExportHelper.ExportToHtml(homeworkData, exportDate, filePath, false);
                            MessageBox.Show($"导出成功：{filePath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else if (selected.StartsWith("JPG"))
                        {
                            bool wasEditMode = EditMode;
                            if (wasEditMode) EditMode = false;
                            Application.DoEvents();
                            ExportHelper.ExportToJpg(this, filePath);
                            if (wasEditMode) EditMode = true;
                            MessageBox.Show($"导出成功：{filePath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        exportDlg.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            btnCancel.Click += (s, e) => exportDlg.Close();
            exportDlg.ShowDialog(this);
        }

        private List<ComboBox> timeComboBoxes = new List<ComboBox>();
    }

    public class Animation
    {
        public double Progress { get; set; } = 0;
        public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(300);
        public TimeSpan StartDelay { get; set; } = TimeSpan.Zero;
        public DateTime StartTime { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsLooping { get; set; } = false;
        public Action<double> OnUpdate { get; set; }
        public Action OnComplete { get; set; }
        public object Tag { get; set; }

        public void Start()
        {
            StartTime = DateTime.Now + StartDelay;
            IsRunning = true;
            Progress = 0;
        }

        public void Update()
        {
            if (!IsRunning) return;
            double elapsed = (DateTime.Now - StartTime).TotalMilliseconds;
            if (elapsed < 0) return;
            double newProgress = Math.Min(1.0, elapsed / Duration.TotalMilliseconds);
            Progress = newProgress;
            OnUpdate?.Invoke(Progress);
            if (newProgress >= 1.0)
            {
                if (IsLooping)
                {
                    StartTime = DateTime.Now + StartDelay;
                    Progress = 0;
                }
                else
                {
                    IsRunning = false;
                    OnComplete?.Invoke();
                }
            }
        }
    }
}