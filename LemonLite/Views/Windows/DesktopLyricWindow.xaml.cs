using EleCho.WpfSuite;
using FluentWpfCore.Helpers;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using LemonLite.ViewModels;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace LemonLite.Views.Windows
{
    public partial class DesktopLyricWindow : Window
    {
        // ── Win32 平滑 resize ──────────────────────────
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        const int WM_SYSCOMMAND = 0x112;
        const int SC_SIZE_LEFT  = 0xF001; // SC_SIZE + WMSZ_LEFT(1)
        const int SC_SIZE_RIGHT = 0xF002; // SC_SIZE + WMSZ_RIGHT(2)
        const double IslandMinWidth = 60d;

        // ── 字段 ──────────────────────────────────────
        private readonly DropShadowEffect shadowEffect = new() { BlurRadius = 5, Direction = 0, ShadowDepth = 0 };
        private readonly DesktopLyricWindowViewModel vm;
        private readonly SettingsMgr<DesktopLyricOption> _settingsMgr;
        private readonly SettingsMgr<LyricOption> _lyricSettingsMgr;

        private bool _isIslandMode = false;
        private double _restoredWidth  = 720d;
        private double _restoredHeight = 145d;
        private double _restoredLeft   = -1d;

        private bool _hasLyricSource = false;

        private const double IslandEmptyWidth  = 170d;
        private const double IslandEmptyHeight = 32d;
        private const double IslandExitThreshold = 80d;

        private CancellationTokenSource? _sizeCts = null;
        private WindowResizeAdorner? _resizeAdorner;

        public DesktopLyricWindow(DesktopLyricWindowViewModel vm, AppSettingService appSettingsService)
        {
            InitializeComponent();
            DataContext = vm;
            this.vm = vm;

            _settingsMgr      = appSettingsService.GetConfigMgr<DesktopLyricOption>();
            _lyricSettingsMgr = appSettingsService.GetConfigMgr<LyricOption>();

            vm.UpdateAnimation = ShowLyricAnimation;
            vm.ScrollLrc       = ScrollLrc;
            vm.SetWindow(this);
            vm.PropertyChanged += Vm_PropertyChanged;

            var sc = SystemParameters.WorkArea;
            Top  = sc.Bottom - Height;
            Left = (sc.Right - Width) / 2;

            Loaded           += DesktopLyricWindow_Loaded;
            MouseEnter       += DesktopLyricWindow_MouseEnter;
            MouseLeave       += DesktopLyricWindow_MouseLeave;
            MouseDoubleClick += DesktopLyricWindow_MouseDoubleClick;
            Closing          += DesktopLyricWindow_Closing;
            LocationChanged  += DesktopLyricWindow_LocationChanged;
            SizeChanged      += DesktopLyricWindow_SizeChanged;
        }

        // ───────────────────────────────────────────
        // Island resize：Win32 syscommand（平滑无抖动）
        // ───────────────────────────────────────────

        private void IslandResizeLeft_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isIslandMode || !_hasLyricSource) return;
            e.Handled = true;
            ReleaseCapture();
            SendMessage(new WindowInteropHelper(this).Handle, WM_SYSCOMMAND,
                new IntPtr(SC_SIZE_LEFT), IntPtr.Zero);
        }

        private void IslandResizeRight_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isIslandMode || !_hasLyricSource) return;
            e.Handled = true;
            ReleaseCapture();
            SendMessage(new WindowInteropHelper(this).Handle, WM_SYSCOMMAND,
                new IntPtr(SC_SIZE_RIGHT), IntPtr.Zero);
        }

        // 拖拽完成后把新宽度保存到配置，并强制居中
        private void DesktopLyricWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (!_isIslandMode || !_hasLyricSource) return;
            // 强制最小宽度
            if (Width < IslandMinWidth)
                Width = IslandMinWidth;
            // 保存宽度
            _settingsMgr.Data.IslandWidth = Width;
            // 保持水平居中
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
        }

        // ───────────────────────────────────────────
        // Island 中间区域：拖动整个窗口
        // ───────────────────────────────────────────

        private void IslandDragOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        // ───────────────────────────────────────────
        // Island 进入 / 退出
        // ───────────────────────────────────────────

        private void DesktopLyricWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (Top <= 1)
                EnterIslandMode();
            else if (_isIslandMode && Top > IslandExitThreshold)
                ExitIslandMode();
        }

        private void EnterIslandMode()
        {
            if (_isIslandMode) return;
            _isIslandMode = true;

            _restoredWidth  = Width;
            _restoredHeight = Height;
            _restoredLeft   = Left;

            SyncTranslationSettings();
            CancelSizeAnim();

            this.BeginAnimation(WidthProperty,  null);
            this.BeginAnimation(HeightProperty, null);
            this.Background = Brushes.Transparent;

            AnimatedBackgroundBd.TopCutRadius = 12d;
            IsLandBaseBackground.TopCutRadius = 12d;
            IsLandBaseBackground.Visibility   = Visibility.Visible;
            IsLandBaseBackground.Width        = double.NaN;
            IsLandBaseBackground.Height       = double.NaN;
            IsLandBaseBackground.HorizontalAlignment = HorizontalAlignment.Stretch;
            IsLandBaseBackground.VerticalAlignment   = VerticalAlignment.Stretch;

            windowRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
            windowRoot.BeginAnimation(WidthProperty,  null);
            windowRoot.BeginAnimation(HeightProperty, null);
            windowRoot.Width  = double.NaN;
            windowRoot.Height = double.NaN;

            // Island 模式下 Adorner 完全禁用，由专用 resize 条处理
            if (_resizeAdorner != null) _resizeAdorner.IsEnabled = false;

            IslandDragOverlay.Visibility  = Visibility.Visible;
            // resize 条只在有歌词时显示
            IslandResizeLeft.Visibility  = Visibility.Collapsed;
            IslandResizeRight.Visibility = Visibility.Collapsed;

            ApplyIslandSize();
        }

        private void ExitIslandMode()
        {
            if (!_isIslandMode) return;
            _isIslandMode = false;

            SyncTranslationSettings();
            CancelSizeAnim();

            this.Background    = null;
            this.SizeToContent = SizeToContent.Manual;

            AnimatedBackgroundBd.TopCutRadius  = 0d;
            IsLandBaseBackground.Visibility    = Visibility.Collapsed;
            IslandDragOverlay.Visibility        = Visibility.Collapsed;
            IslandResizeLeft.Visibility         = Visibility.Collapsed;
            IslandResizeRight.Visibility        = Visibility.Collapsed;
            LrcHost.Visibility                  = Visibility.Visible;

            windowRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
            windowRoot.BeginAnimation(WidthProperty,  null);
            windowRoot.BeginAnimation(HeightProperty, null);
            windowRoot.Width  = double.NaN;
            windowRoot.Height = double.NaN;

            this.BeginAnimation(WidthProperty,  null);
            this.BeginAnimation(HeightProperty, null);
            this.Width  = _restoredWidth  > 0 ? _restoredWidth  : 720;
            this.Height = _restoredHeight > 0 ? _restoredHeight : 145;
            this.Left   = _restoredLeft >= 0
                ? _restoredLeft
                : (SystemParameters.WorkArea.Right - this.Width) / 2;

            if (_resizeAdorner != null) _resizeAdorner.IsEnabled = true;
        }

        // 根据有无歌词切换 Island 的两种形态
        private void ApplyIslandSize()
        {
            if (!_isIslandMode) return;

            if (!_hasLyricSource)
            {
                // 无歌词：固定小胶囊，resize 条隐藏
                this.SizeToContent = SizeToContent.Manual;
                LrcHost.Visibility = Visibility.Collapsed;
                IslandResizeLeft.Visibility  = Visibility.Collapsed;
                IslandResizeRight.Visibility = Visibility.Collapsed;
                this.Width  = IslandEmptyWidth;
                this.Height = IslandEmptyHeight;
                this.Top    = 0;
                this.Left   = (SystemParameters.PrimaryScreenWidth - IslandEmptyWidth) / 2;
            }
            else
            {
                // 有歌词：宽度固定，高度跟随内容，显示 resize 条
                LrcHost.Visibility = Visibility.Visible;
                this.SizeToContent = SizeToContent.Height;
                double w = _settingsMgr.Data.IslandWidth > 0 ? _settingsMgr.Data.IslandWidth : 480d;
                this.Width = w;
                this.Top   = 0;
                this.Left  = (SystemParameters.PrimaryScreenWidth - w) / 2;
                IslandResizeLeft.Visibility  = Visibility.Visible;
                IslandResizeRight.Visibility = Visibility.Visible;
            }
        }

        private void SyncTranslationSettings()
        {
            vm.ShowTranslation = _lyricSettingsMgr.Data.ShowTranslation;
            vm.ShowRomaji      = _lyricSettingsMgr.Data.ShowRomaji;
        }

        // ───────────────────────────────────────────
        // Island 宽度动画（切歌时）
        // ───────────────────────────────────────────

        private void AnimateToWidth(double targetW)
        {
            if (!_isIslandMode) return;
            CancelSizeAnim();
            this.BeginAnimation(WidthProperty,
                new DoubleAnimation(targetW, TimeSpan.FromMilliseconds(240))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } });
            this.Left = (SystemParameters.PrimaryScreenWidth - targetW) / 2;
        }

        private void CancelSizeAnim()
        {
            _sizeCts?.Cancel();
            _sizeCts = null;
        }

        // ───────────────────────────────────────────
        // 歌词内容切换动画
        // ───────────────────────────────────────────

        private void ShowLyricAnimation()
        {
            if (_isIslandMode)
            {
                const int fadeOutMs = 120;
                const int waitMs    = 200;
                const int fadeInMs  = 200;

                var easeIn  = new CubicEase { EasingMode = EasingMode.EaseIn  };
                var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };

                LrcHost.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(1d, 0d, TimeSpan.FromMilliseconds(fadeOutMs))
                    { EasingFunction = easeIn });

                _ = Task.Run(async () =>
                {
                    await Task.Delay(waitMs);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        LrcHost.BeginAnimation(OpacityProperty,
                            new DoubleAnimation(0d, 1d, TimeSpan.FromMilliseconds(fadeInMs))
                            { EasingFunction = easeOut });
                        LrcScrollViewer.BeginAnimation(ScrollViewerUtils.HorizontalOffsetProperty, null);
                        ScrollViewerUtils.SetHorizontalOffset(LrcScrollViewer, 0);
                    });
                });
                return;
            }

            var blur = new BlurEffect() { Radius = 0 };
            LrcHost.Effect = blur;
            LrcHost.BeginAnimation(OpacityProperty, new DoubleAnimationUsingKeyFrames()
            {
                KeyFrames =
                [
                    new LinearDoubleKeyFrame(0, TimeSpan.FromMilliseconds(200)),
                    new LinearDoubleKeyFrame(0, TimeSpan.FromMilliseconds(300)),
                    new LinearDoubleKeyFrame(1, TimeSpan.FromMilliseconds(500))
                ]
            });
            blur.BeginAnimation(BlurEffect.RadiusProperty, new DoubleAnimationUsingKeyFrames()
            {
                KeyFrames =
                [
                    new LinearDoubleKeyFrame(20, TimeSpan.FromMilliseconds(200)),
                    new LinearDoubleKeyFrame(20, TimeSpan.FromMilliseconds(300)),
                    new LinearDoubleKeyFrame(0,  TimeSpan.FromMilliseconds(500))
                ]
            });
            _ = Task.Run(async () =>
            {
                await Task.Delay(200);
                await Dispatcher.InvokeAsync(() =>
                {
                    LrcScrollViewer.BeginAnimation(ScrollViewerUtils.HorizontalOffsetProperty, null);
                    ScrollViewerUtils.SetHorizontalOffset(LrcScrollViewer, 0);
                });
            });
        }

        // ───────────────────────────────────────────
        // SetHasLyricSource
        // ───────────────────────────────────────────

        public void SetHasLyricSource(bool hasLyric)
        {
            if (_hasLyricSource == hasLyric) return;
            _hasLyricSource = hasLyric;

            if (!_isIslandMode) return;

            if (!hasLyric)
            {
                IslandResizeLeft.Visibility  = Visibility.Collapsed;
                IslandResizeRight.Visibility = Visibility.Collapsed;
                LrcHost.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(1d, 0d, TimeSpan.FromMilliseconds(150)));
                _ = Task.Delay(160).ContinueWith(_ => Dispatcher.BeginInvoke(() =>
                {
                    this.SizeToContent = SizeToContent.Manual;
                    LrcHost.Visibility = Visibility.Collapsed;
                    AnimateToWidth(IslandEmptyWidth);
                    this.BeginAnimation(HeightProperty,
                        new DoubleAnimation(IslandEmptyHeight, TimeSpan.FromMilliseconds(240))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } });
                }));
            }
            else
            {
                double w = _settingsMgr.Data.IslandWidth > 0 ? _settingsMgr.Data.IslandWidth : 480d;
                LrcHost.Opacity    = 0;
                LrcHost.Visibility = Visibility.Visible;
                this.SizeToContent = SizeToContent.Height;
                AnimateToWidth(w);
                IslandResizeLeft.Visibility  = Visibility.Visible;
                IslandResizeRight.Visibility = Visibility.Visible;
                _ = Task.Delay(200).ContinueWith(_ =>
                    Dispatcher.BeginInvoke(() =>
                        LrcHost.BeginAnimation(OpacityProperty,
                            new DoubleAnimation(0d, 1d, TimeSpan.FromMilliseconds(200)))));
            }
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // SizeToContent=Height 时翻译行 Visibility 变化自动触发高度更新
        }

        // ───────────────────────────────────────────
        // 水平滚动
        // ───────────────────────────────────────────

        private FrameworkElement? currentBlock = null;

        private void ScrollLrc(FrameworkElement block)
        {
            try
            {
                if (block == currentBlock || block == null) return;
                var position = block.TransformToVisual(LrcHost);
                Point p = position.Transform(new Point(0, 0));
                LrcScrollViewer.BeginAnimation(
                    ScrollViewerUtils.HorizontalOffsetProperty,
                    new DoubleAnimation(p.X - LrcScrollViewer.ViewportWidth * 0.4,
                        TimeSpan.FromMilliseconds(500)));
                currentBlock = block;
            }
            catch { }
        }

        // ───────────────────────────────────────────
        // 其他事件
        // ───────────────────────────────────────────

        private void DesktopLyricWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            CancelSizeAnim();
            vm.UpdateAnimation = null;
            vm.PropertyChanged -= Vm_PropertyChanged;
            vm.SetWindow(null!);
            _settingsMgr.Data.WindowSize = new Size(
                _isIslandMode ? _restoredWidth  : Width,
                _isIslandMode ? _restoredHeight : Height);
            _settingsMgr.Data.IsIslandMode = false;
            vm.Dispose();
        }

        private void DesktopLyricWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!_isIslandMode)
            {
                var sc = SystemParameters.WorkArea;
                Left = (sc.Right - Width) / 2;
            }
        }

        private void DesktopLyricWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            cancelShowFunc?.Cancel();
            cancelShowFunc = null;
            preShowFunc = false;
            LrcPanel.Effect = shadowEffect;
            LrcPanel.BeginAnimation(OpacityProperty, null);
            FuncPanel.Visibility = Visibility.Collapsed;
            FuncPanel.BeginAnimation(OpacityProperty, null);
        }

        private bool preShowFunc = false;
        private CancellationTokenSource? cancelShowFunc = null;

        private async void DesktopLyricWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isIslandMode) return;

            preShowFunc = true;
            cancelShowFunc ??= new();
            try
            {
                await Task.Delay(300, cancelShowFunc.Token);
                if (preShowFunc)
                {
                    LrcPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.6, TimeSpan.FromMilliseconds(100)));
                    LrcPanel.Effect = new BlurEffect() { Radius = 12 };
                    FuncPanel.Visibility = Visibility.Visible;
                    FuncPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
                }
            }
            catch { }
        }

        private void DesktopLyricWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowFlagsHelper.SetToolWindow(this);
            var c     = this.Content as UIElement;
            var layer = AdornerLayer.GetAdornerLayer(c);
            _resizeAdorner = new WindowResizeAdorner(c!);
            layer?.Add(_resizeAdorner);

            LrcPanel.Effect = shadowEffect;

            if (_settingsMgr.Data.WindowSize is { Width: > 0, Height: > 0 } size)
            {
                _restoredWidth  = size.Width;
                _restoredHeight = size.Height;
                Width  = size.Width;
                Height = size.Height;
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
    }
}
