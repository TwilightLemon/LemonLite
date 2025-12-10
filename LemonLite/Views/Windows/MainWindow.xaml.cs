using FluentWpfCore.Interop;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using LemonLite.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LemonLite.Views.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel vm;
    private readonly SmtcService smtcService;
    private readonly UIResourceService ui;
    private readonly SettingsMgr<Appearance> _mgr;
    private const double MobileLayoutThreshold = 600;
    private bool _isMobileLayout = false;
    private Storyboard? LyricImgRTAni;

    public MainWindow(MainWindowViewModel vm,AppSettingService appSettingService, SmtcService smtcService,UIResourceService uiResourceService)
    {
        InitializeComponent();
        this.DataContext = vm;
        _mgr = appSettingService.GetConfigMgr<Appearance>();
        _mgr.OnDataChanged += Appearance_OnDataChanged;
        LyricViewHost.Child = vm.LyricView;
        this.vm = vm;
        this.smtcService = smtcService;
        this.ui = uiResourceService;
        SizeChanged += MainWindow_SizeChanged;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;

        this.MouseEnter += MainWindow_MouseEnter;
        this.MouseLeave += MainWindow_MouseLeave;

        ApplySettings(isFirstCall: true);
    }

    private void Appearance_OnDataChanged()
    {
        Dispatcher.BeginInvoke(()=> ApplySettings());
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        vm.Dispose();
        _mgr.Data.TopMost = Topmost;
        _mgr.Data.Window = new Rect(Left, Top, Width, Height);
        _mgr.OnDataChanged -= Appearance_OnDataChanged;
    }

    private void ApplySettings(bool isFirstCall=false)
    {
        //窗口rect只允许在启动时更新（读取上次的位置）
        if (isFirstCall && !_mgr.Data.Window.IsEmpty)
        {
            if (_mgr.Data.Window.Width >= MinWidth && _mgr.Data.Window.Height >= MinHeight)
            {
                this.Width = _mgr.Data.Window.Width;
                this.Height = _mgr.Data.Window.Height;
                if (_mgr.Data.Window.X >= 0 && _mgr.Data.Window.Y >= 0)
                {
                    this.Left = _mgr.Data.Window.X;
                    this.Top = _mgr.Data.Window.Y;
                }
            }
        }
        Topmost = _mgr.Data.TopMost;

        //Background Type
        if (_mgr.Data.Background == Appearance.BackgroundType.Acrylic)
        {
            material.MaterialMode = MaterialType.Acrylic;
            material.CompositonColor = ui.GetIsDarkMode() ? Color.FromArgb(0x01, 0, 0, 0) : Color.FromArgb(0x01, 0xff, 0xff, 0xff);
            material.UseWindowComposition = true;
            AnimatedBackgroundBd.Visibility = Visibility.Visible;
            AnimatedBackgroundBd.Opacity = _mgr.Data.AcylicOpacity;
            Background = Brushes.Transparent;
            ImageBackground.Background = null;
        }
        else if (_mgr.Data.Background == Appearance.BackgroundType.Image)
        {
            if (!string.IsNullOrEmpty(_mgr.Data.BackgroundImagePath))
            {
                material.MaterialMode = MaterialType.None;
                material.UseWindowComposition = false;
                AnimatedBackgroundBd.Visibility= Visibility.Collapsed;
                SetResourceReference(BackgroundProperty, "BackgroundColor");
                ImageBackground.Opacity= _mgr.Data.BackgroundOpacity;
                ImageBackground.Background = new ImageBrush(new BitmapImage(new Uri(_mgr.Data.BackgroundImagePath))) { Stretch = Stretch.UniformToFill };
            }
        }
        else
        {
            AnimatedBackgroundBd.Visibility = Visibility.Visible;
            AnimatedBackgroundBd.Opacity = 1;
            material.MaterialMode = MaterialType.None;
            material.UseWindowComposition = false;
            SetResourceReference(BackgroundProperty, "BackgroundColor");
            ImageBackground.Background = null;
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateLayout(ActualWidth);
        
        vm.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(vm.IsPlaying))
            {
                ControlRotationAnimation(vm.IsPlaying);
            }
        };
        
        ControlRotationAnimation(vm.IsPlaying);
    }

    private bool _isSliderCtrl = false;
    private void PlaySlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is Slider PlaySlider)
        {
            double perc = e.GetPosition(PlaySlider).X / PlaySlider.ActualWidth;
            double value = perc * PlaySlider.Maximum;
            //暂时移除PlaySlider的Value binding
            BindingOperations.ClearBinding(PlaySlider, Slider.ValueProperty);
            PlaySlider.Value = value;
            _isSliderCtrl = true;
        }
    }

    private async void PlaySlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isSliderCtrl && sender is Slider PlaySlider)
        {
            //提交value
            await smtcService.SmtcListener.SetPosition(TimeSpan.FromSeconds(PlaySlider.Value));
            vm.CurrentPlayingPosition = PlaySlider.Value;
            //重新绑定PlaySlider的Value
            var binding = new Binding("CurrentPlayingPosition")
            {
                Source = vm,
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            PlaySlider.SetBinding(Slider.ValueProperty, binding);
            _isSliderCtrl = false;
        }
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateLayout(e.NewSize.Width);
    }

    private void UpdateLayout(double width)
    {
        bool shouldBeMobile = width < MobileLayoutThreshold;
        
        if (shouldBeMobile == _isMobileLayout) return;
        
        _isMobileLayout = shouldBeMobile;

        if (_isMobileLayout)
        {
            SwitchToMobileLayout();
        }
        else
        {
            SwitchToDesktopLayout();
        }
    }

    private void SwitchToMobileLayout()
    {
        // 设置RootGrid为2行布局：顶部歌曲信息、中间歌词
        RootGrid.RowDefinitions.Clear();
        RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
        RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // 隐藏列分隔
        LeftColumn.Width = new GridLength(1, GridUnitType.Star);
        RightColumn.Width = new GridLength(0);

        // 歌曲信息面板占据第一行，跨两列
        Grid.SetColumn(SongInfoPanel, 0);
        Grid.SetRow(SongInfoPanel, 0);
        Grid.SetColumnSpan(SongInfoPanel, 2);

        // SongInfoPanel内部使用2行1列布局：第一行放封面和信息文本（左右），第二行放控制按钮
        CoverRow.Height = new GridLength(1, GridUnitType.Auto);
        InfoRow.Height = new GridLength(1, GridUnitType.Auto);
        
        // SongInfoPanel内部列布局：左边封面，右边歌曲信息
        var coverCol = SongInfoPanel.ColumnDefinitions[0];
        var infoCol = SongInfoPanel.ColumnDefinitions[1];
        coverCol.Width = new GridLength(120); // 封面固定宽度
        infoCol.Width = new GridLength(1, GridUnitType.Star); // 信息区域占据剩余空间

        // 调整封面区域 - 移动到左上角
        Grid.SetRow(CoverArea, 0);
        Grid.SetColumn(CoverArea, 0);
        Grid.SetRowSpan(CoverArea, 1);
        LyricPage_ImgEdge.MinWidth = 0;
      //  LyricPage_ImgEdge.MaxWidth = 120;
        LyricPage_ImgEdge.MinHeight = 80;
        LyricPage_ImgEdge.Margin = new Thickness(10);
        LyricPage_Img.HorizontalAlignment = HorizontalAlignment.Center;
        LyricPage_Img.VerticalAlignment = VerticalAlignment.Center;

        // 歌曲文本信息 - 移动到右上角
        Grid.SetRow(SongTextInfo, 0);
        Grid.SetColumn(SongTextInfo, 1);
        SongTextInfo.Margin = new Thickness(10, 20, 20, 10);
        SongTextInfo.VerticalAlignment = VerticalAlignment.Top;
        SongTitleBlock.Margin = new Thickness(0, 0, 0, 4);
        SongTitleBlock.TextAlignment = TextAlignment.Left;
        SongTitleBlock.TextWrapping = TextWrapping.NoWrap;
        SongTitleBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        SongArtistBlock.Margin = new Thickness(0);
        SongArtistBlock.TextAlignment = TextAlignment.Left;
        SongArtistBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        SongArtistBlock.TextWrapping = TextWrapping.NoWrap;

        //ProgressPanel.Margin = new Thickness(0, 10, 0, 0);
        //ProgressPanel.MaxWidth = double.PositiveInfinity;
        ProgressPanel.Visibility = Visibility.Collapsed;

        // 播放控制按钮 - 移动到底部，跨两列
        Grid.SetRow(PlayControlPanel, 1);
        Grid.SetColumn(PlayControlPanel, 0);
        Grid.SetColumnSpan(PlayControlPanel, 2);
        PlayControlPanel.Margin = new Thickness(32, 0, 32, 0);
        PlayControlPanel.VerticalAlignment = VerticalAlignment.Center;
        PlayControlPanel.MaxWidth = double.PositiveInfinity;
        PlayControlPanel.Visibility = Visibility.Collapsed;

        // 歌词区域占据第二行（中间），跨两列
        Grid.SetColumn(LyricViewHost, 0);
        Grid.SetRow(LyricViewHost, 1);
        Grid.SetColumnSpan(LyricViewHost, 2);
        LyricViewHost.Margin = new Thickness(20, 0, 20, 10);

        // 歌词工具栏移到歌词区域右侧
        Grid.SetColumn(LyricToolBar, 0);
        Grid.SetRow(LyricToolBar, 1);
        Grid.SetColumnSpan(LyricToolBar, 2);
        LyricToolBar.Margin = new Thickness(0, 0, 0, 10);
        LyricToolBar.VerticalAlignment = VerticalAlignment.Bottom;
        LyricToolBar.HorizontalAlignment = HorizontalAlignment.Center;
        LyricToolBarPanel.Orientation = Orientation.Horizontal;
        LyricToolBar.Height = 36;
        LyricToolBar.Width = double.NaN;
        LyricToolBar.Visibility = Visibility.Collapsed;
    }

    private void MainWindow_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isMobileLayout)
        {
            PlayControlPanel.Height = double.NaN;
            CoverArea.Visibility = SongTextInfo.Visibility = Visibility.Visible;
            PlayControlPanel.Visibility = Visibility.Collapsed;

            LyricToolBar.Visibility = Visibility.Collapsed;
        }
    }

    private void MainWindow_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isMobileLayout)
        {
            PlayControlPanel.Height = CoverArea.ActualHeight;
            CoverArea.Visibility = SongTextInfo.Visibility = Visibility.Collapsed;
            PlayControlPanel.Visibility = Visibility.Visible;

            LyricToolBar.Visibility = Visibility.Visible;
        }
    }
    
    private void SwitchToDesktopLayout()
    {
        // 清除行定义，恢复为单行布局
        RootGrid.RowDefinitions.Clear();

        // 恢复左右分栏
        LeftColumn.Width = new GridLength(6, GridUnitType.Star);
        RightColumn.Width = new GridLength(7, GridUnitType.Star);

        // 歌曲信息面板回到左列
        Grid.SetColumn(SongInfoPanel, 0);
        Grid.SetRow(SongInfoPanel, 0);
        Grid.SetColumnSpan(SongInfoPanel, 1);

        // 恢复SongInfoPanel内部布局：2行1列，列宽度重置
        CoverRow.Height = new GridLength(2, GridUnitType.Star);
        InfoRow.Height = new GridLength(1, GridUnitType.Star);
        
        var coverCol = SongInfoPanel.ColumnDefinitions[0];
        var infoCol = SongInfoPanel.ColumnDefinitions[1];
        coverCol.Width = new GridLength(1, GridUnitType.Star);
        infoCol.Width = new GridLength(0);

        // 恢复封面区域到第一行
        Grid.SetRow(CoverArea, 0);
        Grid.SetColumn(CoverArea, 0);
        Grid.SetRowSpan(CoverArea, 1);
        Grid.SetColumnSpan(CoverArea, 1);
        LyricPage_ImgEdge.MinWidth = 200;
        LyricPage_ImgEdge.MaxWidth = 500;
        LyricPage_ImgEdge.Margin = new Thickness(60);
        LyricPage_Img.HorizontalAlignment = HorizontalAlignment.Stretch;
        LyricPage_Img.VerticalAlignment = VerticalAlignment.Stretch;

        // 恢复歌曲文本信息到第二行
        Grid.SetRow(SongTextInfo, 1);
        Grid.SetColumn(SongTextInfo, 0);
        SongTextInfo.Margin = new Thickness(0, -180, 0, 20);
        SongTextInfo.VerticalAlignment = VerticalAlignment.Center;
        SongTitleBlock.Margin = new Thickness(48, 0, 48, 8);
        SongTitleBlock.TextAlignment = TextAlignment.Center;
        SongTitleBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        SongTitleBlock.TextWrapping = TextWrapping.Wrap;
        SongArtistBlock.Margin = new Thickness(48, 0, 48, 0);
        SongArtistBlock.TextAlignment = TextAlignment.Center;
        SongArtistBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        SongArtistBlock.TextWrapping = TextWrapping.Wrap;

        ProgressPanel.Margin = new Thickness(60, 20, 60, 0);
        ProgressPanel.MaxWidth = 480;
        PlayControlPanel.Visibility = Visibility.Visible;

        // 恢复播放控制按钮到第二行底部
        Grid.SetRow(PlayControlPanel, 1);
        Grid.SetColumn(PlayControlPanel, 0);
        Grid.SetColumnSpan(PlayControlPanel, 1);
        PlayControlPanel.Margin = new Thickness(32, 0, 32, 100);
        PlayControlPanel.VerticalAlignment = VerticalAlignment.Bottom;
        PlayControlPanel.MaxWidth = 240;
        ProgressPanel.Visibility = Visibility.Visible;

        // 歌词区域回到右列
        Grid.SetColumn(LyricViewHost, 1);
        Grid.SetRow(LyricViewHost, 0);
        Grid.SetColumnSpan(LyricViewHost, 1);
        LyricViewHost.Margin = new Thickness(10, 20, 80, 20);

        // 歌词工具栏回到右列右侧
        Grid.SetColumn(LyricToolBar, 1);
        Grid.SetRow(LyricToolBar, 0);
        Grid.SetColumnSpan(LyricToolBar, 1);
        LyricToolBar.Margin = new Thickness(0, 0, 36, 0);
        LyricToolBar.VerticalAlignment = VerticalAlignment.Center;
        LyricToolBar.HorizontalAlignment = HorizontalAlignment.Right;
        LyricToolBarPanel.Orientation = Orientation.Vertical;
        LyricToolBar.Height = double.NaN;
        LyricToolBar.Width = 36;
        LyricToolBar.Visibility= Visibility.Visible;
    }

    private void ControlRotationAnimation(bool play)
    {
        if (LyricImgRTAni == null)
        {
            LyricImgRTAni = new();
            DoubleAnimation da = new(0, 360, TimeSpan.FromSeconds(15))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(da, LyricImgRT);
            Storyboard.SetTargetProperty(da, new PropertyPath("(RotateTransform.Angle)"));
            LyricImgRTAni.Children.Add(da);
            LyricImgRTAni.Freeze();
            LyricImgRTAni.Begin();
        }
        else
        {
            //if no background, pause animation
            if (vm.BackgroundImageSource == null)
            {
                LyricImgRTAni.Pause();
                return;
            }
            if (play)
            {
                LyricImgRTAni.Resume();
            }
            else
            {
                LyricImgRTAni.Pause();
            }
        }
    }

}