using EleCho.WpfSuite;
using FluentWpfCore.Helpers;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using LemonLite.ViewModels;
using System;
using System.Diagnostics;
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
        const double IslandMinWidth = 60d;

        private readonly DropShadowEffect shadowEffect = new() { BlurRadius = 5, Direction = 0, ShadowDepth = 0 };
        private readonly DesktopLyricWindowViewModel vm;
        private readonly SettingsMgr<DesktopLyricOption> _settingsMgr;

        private bool _isIslandMode = false;
        private bool _isMouseIn = false;
        private double _restoredWidth = 720d;
        private double _restoredHeight = 145d;

        private bool _hasLyricSource = false;

        private const double IslandEmptyWidth = 170d;
        private const double IslandEmptyHeight = 32d;
        private const double IslandExitThreshold = 40d;

        private WindowResizeAdorner? _resizeAdorner;
        private readonly GlobalHotkeyService _hotkeyService;
        private CancellationTokenSource? _hotkeyInfoPanelCts;

        public DesktopLyricWindow(DesktopLyricWindowViewModel vm, AppSettingService appSettingsService, GlobalHotkeyService hotkeyService)
        {
            InitializeComponent();
            DataContext = vm;
            this.vm = vm;
            _hotkeyService = hotkeyService;

            _settingsMgr = appSettingsService.GetConfigMgr<DesktopLyricOption>();
            _settingsMgr.OnDataChanged += SettingsMgr_OnDataChanged;

            vm.HideLineAnimation =(callback) =>
            {
                if (!_isIslandInfoOpen)
                    HideLyricAnimation(callback);
                else callback(false);
            };
            vm.ShowLineAnimation = ShowLyricAnimation;

            vm.ScrollLrc = ScrollLrc;
            vm.SetWindow(this);

            var sc = SystemParameters.WorkArea;
            Top = sc.Bottom - Height;
            Left = (sc.Right - Width) / 2;

            Loaded += DesktopLyricWindow_Loaded;
            MouseEnter += DesktopLyricWindow_MouseEnter;
            MouseLeave += DesktopLyricWindow_MouseLeave;
            MouseDoubleClick += DesktopLyricWindow_MouseDoubleClick;
            Closing += DesktopLyricWindow_Closing;
            LocationChanged += DesktopLyricWindow_LocationChanged;
            SizeChanged += DesktopLyricWindow_SizeChanged;

            _hotkeyService.HotkeyActionTriggered += OnHotkeyActionTriggered;
        }

        private void SettingsMgr_OnDataChanged()
        {
            Dispatcher.BeginInvoke(() =>
            {
                ApplyBackground();
                if (ShouldAddShadowEffect)
                    LrcPanel.Effect = shadowEffect;
                if (_isIslandMode)
                {
                    if (_settingsMgr.Data.UsePopupAnimation)
                    {
                        //windowRoot 自动大小，最大宽度由窗口限制
                        windowRoot.HorizontalAlignment = HorizontalAlignment.Center;
                    }
                    else
                    {
                        windowRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
                    }
                }
            });
        }

        private void DesktopLyricWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (!_isIslandMode || !_hasLyricSource) return;
            if (Width < IslandMinWidth)
                Width = IslandMinWidth;
        }

        private void DesktopLyricWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (Top <= 1)
                EnterIslandMode();
            else if (_isIslandMode && Top > IslandExitThreshold)
                ExitIslandMode();

            if (_isIslandMode) Top = 0;
        }

        private void ApplyBackground()
        {
            bool useAnimatedBackground = _settingsMgr.Data.EnableBackground;
            if (_isIslandMode)
            {
                //island模式下，如果启用背景则AnimatedBackgroundBd可见；如果不启用背景则AnimatedBackgroundBd不可见，IsLandBaseBackground可见，提供纯色背景和圆角
                if (useAnimatedBackground)
                {
                    AnimatedBackgroundBd.Visibility = Visibility.Visible;
                    IsLandBaseBackground.Visibility = Visibility.Collapsed;
                }
                else
                {
                    AnimatedBackgroundBd.Visibility = Visibility.Collapsed;
                    IsLandBaseBackground.Visibility = Visibility.Visible;
                }
            }
            else
            {
                //桌面歌词模式下，直接选择是否启用AnimatedBackgroundBd
                AnimatedBackgroundBd.Visibility = useAnimatedBackground ? Visibility.Visible : Visibility.Collapsed;
                IsLandBaseBackground.Visibility= Visibility.Collapsed;
            }
        }

        private bool ShouldAddShadowEffect
        {
            get=> !_isIslandMode && !_settingsMgr.Data.EnableBackground;
        }

        private void EnterIslandMode()
        {
            if (_isIslandMode) return;
            _isIslandMode = true;

            _restoredWidth = Width;
            _restoredHeight = Height;

            AnimatedBackgroundBd.TopCutRadius = 8d;
            AnimatedBackgroundBd.CornerRadius = new CornerRadius(24);

            ApplyBackground();
            LrcPanel.Effect = null;

            if (_settingsMgr.Data.UsePopupAnimation)
            {
                //windowRoot 自动大小，最大宽度由窗口限制
                windowRoot.HorizontalAlignment = HorizontalAlignment.Center;
            }
            else
            {
                windowRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
            }
            windowRoot.BeginAnimation(WidthProperty, null);
            windowRoot.BeginAnimation(HeightProperty, null);
            windowRoot.Width = double.NaN;
            windowRoot.Height = double.NaN;

            //island模式下只允许调整宽度和拖动窗口
            if (_resizeAdorner != null)
            {
                _resizeAdorner.IslandMode = true;
            }

            ApplyIslandSize();
        }

        private void ExitIslandMode()
        {
            if (!_isIslandMode) return;
            _isIslandMode = false;

            this.SizeToContent = SizeToContent.Manual;

            AnimatedBackgroundBd.TopCutRadius = 0d;
            AnimatedBackgroundBd.CornerRadius = new CornerRadius(12);
            ApplyBackground();

            LrcHost.Visibility = Visibility.Visible;
            LrcPanel.Visibility = Visibility.Visible;
            InfoPanel.Visibility = Visibility.Collapsed;
            _isIslandInfoOpen = false;
            if (ShouldAddShadowEffect)
                LrcPanel.Effect = shadowEffect;

            windowRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
            windowRoot.BeginAnimation(WidthProperty, null);
            windowRoot.BeginAnimation(HeightProperty, null);
            windowRoot.Width = double.NaN;
            windowRoot.Height = double.NaN;

            if (_resizeAdorner != null)
            {
                _resizeAdorner.IslandMode = false;
            }

            Dispatcher.BeginInvoke(async() => {
                await Task.Delay(300);
                this.Width = _restoredWidth > 0 ? _restoredWidth : 720;
                this.Height = _restoredHeight > 0 ? _restoredHeight : 145;
            }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        // 根据有无歌词切换 Island 的两种形态
        private void ApplyIslandSize()
        {
            if (!_isIslandMode || _isIslandInfoOpen) return;

            if (!_hasLyricSource)
            {
                // 无歌词：固定小胶囊
                this.SizeToContent = SizeToContent.Manual;
                LrcHost.Visibility = Visibility.Collapsed;
                windowRoot.Width = IslandEmptyWidth;
                windowRoot.Height = IslandEmptyHeight;
                windowRoot.VerticalAlignment = VerticalAlignment.Top;
            }
            else
            {
                // 有歌词：窗体宽度固定，高度跟随内容
                this.SizeToContent = SizeToContent.Height;
                LrcHost.Visibility = Visibility.Visible;
                windowRoot.Width = double.NaN;
                windowRoot.Height = double.NaN;
                windowRoot.VerticalAlignment = VerticalAlignment.Stretch;
                if (_settingsMgr.Data.UsePopupAnimation)
                {
                    //windowRoot 自动大小，最大宽度由窗口限制
                    windowRoot.HorizontalAlignment = HorizontalAlignment.Center;
                }
                else
                {
                    windowRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
                }
            }
        }
        private (ScaleTransform scale, TranslateTransform translate) EnsureWindowRootTransformGroup()
        {
            if (windowRoot.RenderTransform is TransformGroup tg && tg.Children.Count >= 2
                && tg.Children[0] is ScaleTransform st && tg.Children[1] is TranslateTransform tt)
            {
                return (st, tt);
            }

            var scale = new ScaleTransform(1, 1);
            var translate = new TranslateTransform(0, 0);
            tg = new TransformGroup();
            tg.Children.Add(scale);
            tg.Children.Add(translate);
            windowRoot.RenderTransform = tg;
            windowRoot.RenderTransformOrigin = new Point(0.5, 0);
            return (scale, translate);
        }
        private void HideLyricAnimation(Action<bool> callback)
        {
            if (_isIslandMode)
            {
                if (!_isMouseIn && _settingsMgr.Data.UsePopupAnimation)
                {
                    //收回隐藏
                    Storyboard sb = new();
                    EnsureWindowRootTransformGroup();

                    DoubleAnimation scaleAni = new(1, 0, TimeSpan.FromMilliseconds(300))
                    {
                        EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(scaleAni, windowRoot);
                    Storyboard.SetTargetProperty(scaleAni, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));

                    DoubleAnimation yAni = new(0, -windowRoot.ActualHeight, TimeSpan.FromMilliseconds(300))
                    {
                        EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseIn }
                    };
                    Storyboard.SetTarget(yAni, windowRoot);
                    Storyboard.SetTargetProperty(yAni, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));

                    sb.Children.Add(scaleAni);
                    sb.Children.Add(yAni);
                    sb.Completed += delegate
                    {
                        LrcScrollViewer.BeginAnimation(ScrollViewerUtils.HorizontalOffsetProperty, null);
                        ScrollViewerUtils.SetHorizontalOffset(LrcScrollViewer, 0);
                        callback?.Invoke(true);
                    };
                    sb.Begin();
                }
            }

            var blur = new BlurEffect() { Radius = 0 };
            LrcHost.Effect = blur;
            LrcHost.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(300)));
            var anim = new DoubleAnimation(20, TimeSpan.FromMilliseconds(300));
            anim.Completed += delegate
            {
                LrcScrollViewer.BeginAnimation(ScrollViewerUtils.HorizontalOffsetProperty, null);
                ScrollViewerUtils.SetHorizontalOffset(LrcScrollViewer, 0);
                callback?.Invoke(true);
            };
            blur.BeginAnimation(BlurEffect.RadiusProperty, anim);
        }

        private async void ShowLyricAnimation(int gap)
        {
            if (_isIslandMode)
            {
                if (_settingsMgr.Data.UsePopupAnimation)
                {
                    Storyboard sb = new();
                    var (scale, translate) = EnsureWindowRootTransformGroup();
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                    translate.BeginAnimation(TranslateTransform.YProperty, null);
                    LrcHost.BeginAnimation(OpacityProperty, null);
                    LrcHost.Effect = null;

                    DoubleAnimation scaleAni = new(0, 1, TimeSpan.FromMilliseconds(300))
                    {
                        EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(scaleAni, windowRoot);
                    Storyboard.SetTargetProperty(scaleAni, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));

                    DoubleAnimation yAni = new(-windowRoot.ActualHeight, 0, TimeSpan.FromMilliseconds(300))
                    {
                        EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(yAni, windowRoot);
                    Storyboard.SetTargetProperty(yAni, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)"));

                    sb.Children.Add(scaleAni);
                    sb.Children.Add(yAni);
                    sb.Completed += delegate
                    {
                        scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                        translate.BeginAnimation(TranslateTransform.YProperty, null);
                    };
                    sb.Begin();
                }
            }

            var blur = new BlurEffect() { Radius = 20 };
            LrcHost.Effect = blur;
            LrcHost.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));
            var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200));
            anim.Completed += delegate
            {
                LrcHost.BeginAnimation(OpacityProperty, null);
                LrcHost.Effect = null;
            };
            blur.BeginAnimation(BlurEffect.RadiusProperty, anim);
        }

        public void SetHasLyricSource(bool hasLyric)
        {
            if (_hasLyricSource == hasLyric) return;
            _hasLyricSource = hasLyric;

            ApplyIslandSize();
        }


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


        private void DesktopLyricWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _hotkeyService.HotkeyActionTriggered -= OnHotkeyActionTriggered;
            _hotkeyInfoPanelCts?.Cancel();
            vm.HideLineAnimation = null;
            vm.ShowLineAnimation = null;
            vm.SetWindow(null!);
            _settingsMgr.OnDataChanged -= SettingsMgr_OnDataChanged;
            _settingsMgr.Data.WindowSize = new Size(
                _isIslandMode ? _restoredWidth : Width,
                _isIslandMode ? _restoredHeight : Height);
            _settingsMgr.Data.IsIslandMode = _isIslandMode;
            if(_isIslandMode ) {
                _settingsMgr.Data.IslandWindowLeft = Left;
                _settingsMgr.Data.IslandMaxWidth = Width;
            }
            vm.Dispose();
        }

        private void DesktopLyricWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var sc = SystemParameters.WorkArea;
            Left = (sc.Right - Width) / 2;
        }

        bool _isIslandInfoOpen = false;
        private void DesktopLyricWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            _isMouseIn = false;
            cancelShowFunc?.Cancel();
            cancelShowFunc = null;
            preShowFunc = false;

            if (_isIslandMode && _isIslandInfoOpen)
            {
                CloseIslandInfoPanel();
                return;
            }

            if (ShouldAddShadowEffect)
                LrcPanel.Effect = shadowEffect;
            else LrcPanel.Effect = null;
            LrcPanel.BeginAnimation(OpacityProperty, null);
            FuncPanel.Visibility = Visibility.Collapsed;
            FuncPanel.BeginAnimation(OpacityProperty, null);
        }

        private void OpenIslandInfoPanel()
        {
            if (_isIslandInfoOpen) return;
            _isIslandInfoOpen = true;

            double width = windowRoot.ActualWidth, height = windowRoot.ActualHeight;
            double targetWidth = this.ActualWidth;
            double targetHeight = 146d; // InfoPanel的高度

            var storyBoard = new Storyboard();
            var widthAnim = new DoubleAnimation(width, targetWidth, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut }
            };
            var heightAnim = new DoubleAnimation(height, targetHeight, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new BackEase() { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(widthAnim, windowRoot);
            Storyboard.SetTargetProperty(widthAnim, new PropertyPath(WidthProperty));
            Storyboard.SetTarget(heightAnim, windowRoot);
            Storyboard.SetTargetProperty(heightAnim, new PropertyPath(HeightProperty));
            storyBoard.Children.Add(widthAnim);
            storyBoard.Children.Add(heightAnim);

            InfoPanel.Visibility = Visibility.Visible;
            InfoPanel.Opacity = 0;
            var opacityAni = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            Storyboard.SetTarget(opacityAni, InfoPanel);
            Storyboard.SetTargetProperty(opacityAni, new PropertyPath(OpacityProperty));
            storyBoard.Children.Add(opacityAni);

            storyBoard.Completed += delegate
            {
                windowRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
                windowRoot.BeginAnimation(WidthProperty, null);
                windowRoot.BeginAnimation(HeightProperty, null);
                windowRoot.Width = double.NaN;
                windowRoot.Height = double.NaN;
            };
            storyBoard.Begin();
            LrcPanel.Visibility = Visibility.Collapsed;
        }

        private void CloseIslandInfoPanel()
        {
            if (!_isIslandInfoOpen) return;
            _isIslandInfoOpen = false;

            if (_hasLyricSource)
            {
                InfoPanel.Visibility = Visibility.Collapsed;
                LrcPanel.Visibility = Visibility.Visible;
                LrcPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var size = LrcPanel.DesiredSize;
                double height = windowRoot.ActualHeight, width = windowRoot.ActualWidth;
                if (!_settingsMgr.Data.UsePopupAnimation)
                {
                    size.Width = width;
                }

                var storyBoard = new Storyboard();
                var widthAnim = new DoubleAnimation(width, size.Width, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseIn }
                };
                var heightAnim = new DoubleAnimation(height, size.Height, TimeSpan.FromMilliseconds(260))
                {
                    EasingFunction = new BackEase() { EasingMode = EasingMode.EaseIn }
                };
                Storyboard.SetTarget(widthAnim, windowRoot);
                Storyboard.SetTargetProperty(widthAnim, new PropertyPath(WidthProperty));
                Storyboard.SetTarget(heightAnim, windowRoot);
                Storyboard.SetTargetProperty(heightAnim, new PropertyPath(HeightProperty));
                storyBoard.Children.Add(widthAnim);
                storyBoard.Children.Add(heightAnim);

                var blur = new BlurEffect() { Radius = 20 };
                LrcHost.Effect = blur;
                LrcHost.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500)));
                var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(500));
                anim.Completed += delegate
                {
                    LrcHost.BeginAnimation(OpacityProperty, null);
                    LrcHost.Effect = null;
                };
                blur.BeginAnimation(BlurEffect.RadiusProperty, anim);

                storyBoard.Completed += delegate
                {
                    LrcPanel.Visibility = Visibility.Visible;
                    InfoPanel.Visibility = Visibility.Collapsed;
                    windowRoot.BeginAnimation(WidthProperty, null);
                    windowRoot.BeginAnimation(HeightProperty, null);
                    ApplyIslandSize();
                };
                storyBoard.Begin();
            }
            else
            {
                LrcPanel.Visibility = Visibility.Visible;
                InfoPanel.Visibility = Visibility.Collapsed;
                ApplyIslandSize();
            }
        }

        private bool preShowFunc = false;
        private CancellationTokenSource? cancelShowFunc = null;

        private async void DesktopLyricWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            _isMouseIn = true;
            if (_isIslandMode)
            {
                if (!_hasLyricSource)
                {
                    // 无歌词：固定小胶囊
                    this.SizeToContent = SizeToContent.Manual;
                    LrcHost.Visibility = Visibility.Collapsed;
                    windowRoot.Width = IslandEmptyWidth;
                    windowRoot.Height = IslandEmptyHeight;
                    windowRoot.VerticalAlignment = VerticalAlignment.Top;
                }
            }

            //鼠标进入后300毫秒显示面板，期间如果鼠标离开则取消显示
            preShowFunc = true;
            cancelShowFunc ??= new();
            try
            {
                await Task.Delay(300, cancelShowFunc.Token);
                if (preShowFunc)
                {
                    if (_isIslandMode)
                        {
                            if (!_hasLyricSource || _isIslandInfoOpen) return;
                            OpenIslandInfoPanel();
                        }
                    else
                    {
                        LrcPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.6, TimeSpan.FromMilliseconds(100)));
                        LrcPanel.Effect = new BlurEffect() { Radius = 12 };
                        FuncPanel.Visibility = Visibility.Visible;
                        FuncPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
                    }
                }
            }
            catch { }
        }

        private void DesktopLyricWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowFlagsHelper.SetToolWindow(this);
            var c = this.Content as UIElement;
            var layer = AdornerLayer.GetAdornerLayer(c);
            _resizeAdorner = new WindowResizeAdorner(c!);
            layer?.Add(_resizeAdorner);

            if (_settingsMgr.Data.WindowSize is { Width: > 0, Height: > 0 } size)
            {
                _restoredWidth = size.Width;
                _restoredHeight = size.Height;
                Width = size.Width;
                Height = size.Height;
            }
            if (_settingsMgr.Data.IsIslandMode)
            {
                Top = 0;
                if(_settingsMgr.Data.IslandWindowLeft is double left && left > 0) 
                    Left = left;
                if (_settingsMgr.Data.IslandMaxWidth is double width)
                    Width = width;
            }
            if (ShouldAddShadowEffect)
                LrcPanel.Effect = shadowEffect;
        }

        private async void OnHotkeyActionTriggered(GlobalHotkeyService.HotkeyActionType action)
        {
            if (!_isIslandMode || _isMouseIn) return;

            // 取消上一次热键触发的延迟关闭
            _hotkeyInfoPanelCts?.Cancel();
            _hotkeyInfoPanelCts = new CancellationTokenSource();
            var cts = _hotkeyInfoPanelCts;

            if (!_isIslandInfoOpen)
                OpenIslandInfoPanel();

            try
            {
                await Task.Delay(1500, cts.Token);
                if (!_isMouseIn)
                    CloseIslandInfoPanel();
            }
            catch (OperationCanceledException) { }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
    }
}
