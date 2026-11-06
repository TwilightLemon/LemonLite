using System.Windows;
using LemonLite.Behaviors;
using Microsoft.Xaml.Behaviors;
using System.Windows.Shell;
using wsButton = EleCho.WpfSuite.Controls.Button;
using System.ComponentModel;
using System.Windows.Media;
using FluentWpfCore.Interop;
using FluentWpfCore.AttachedProperties;
using System;
using System.Windows.Controls;

namespace LemonLite.Utils;

/// <summary>
/// 提供含有标题栏的FluentWindow样式基类
/// </summary>
public class FluentWindowBase : Window
{
    private wsButton? CloseBtn, MaximizeBtn, MinimizeBtn;
    protected Grid? PART_TitleBar;
    private readonly BehaviorCollection _behaviors;
    private readonly BlurWindowBehavior _blurBehavior;
    private readonly WindowChrome _windowChrome;
    private DependencyPropertyDescriptor? _resizeModeDescriptor;

    public const int DEFAULT_WINDOW_CAPTION_HEIGHT = 48;
    private Thickness _resizeBorderThickness = new(6);

    public MaterialType Mode
    {
        get => (MaterialType)_blurBehavior.GetValue(BlurWindowBehavior.ModeProperty);
        set => _blurBehavior.SetCurrentValue(BlurWindowBehavior.ModeProperty, value);
    }

    public bool IsToolWindow
    {
        get => _blurBehavior.IsToolWindow;
        set => _blurBehavior.IsToolWindow = value;
    }

    public FluentWindowBase()
    {
        Style = (Style)FindResource("FluentWindowStyle");

        var osVersion = Environment.OSVersion.Version;
        var windows11 = new Version(10, 0, 22621);
        if (osVersion >= windows11)
        {
            WindowMaterial.SetWindowCorner(this, MaterialApis.WindowCorner.Round);
            DwmAnimation.SetEnableDwmAnimation(this, true);
        }

        _behaviors = Interaction.GetBehaviors(this);
        _behaviors.Add(new WindowDragMoveBehavior());
        _windowChrome = new()
        {
            CaptionHeight = DEFAULT_WINDOW_CAPTION_HEIGHT,
            ResizeBorderThickness = _resizeBorderThickness
        };
        _blurBehavior = new()
        {
            WindowChromeEx = _windowChrome
        };
        _behaviors.Add(_blurBehavior);
    }

    protected override void OnClosed(EventArgs e)
    {
        //解除描述符监听，否则其内部强引用会钉死整个窗口
        _resizeModeDescriptor?.RemoveValueChanged(this, ResizeModeChanged);
        _resizeModeDescriptor = null;
        _behaviors.Clear();
        base.OnClosed(e);
    }

    public double WindowCaptionHeight
    {
        get => _windowChrome.CaptionHeight;
        set => _windowChrome.CaptionHeight = value;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        CloseBtn = (wsButton)Template.FindName("CloseBtn", this);
        MaximizeBtn = (wsButton)Template.FindName("MaximizeBtn", this);
        MinimizeBtn = (wsButton)Template.FindName("MinimizeBtn", this);

        PART_TitleBar = (Grid)Template.FindName("PART_TitleBar",this);

        if (CloseBtn == null || MaximizeBtn == null || MinimizeBtn == null)
            throw new NullReferenceException("!!!");

        CloseBtn.Click += CloseBtn_Click;
        MaximizeBtn.Click += MaximizeBtn_Click;
        MinimizeBtn.Click += MinimizeBtn_Click;

        //接管ResizeMode属性
        ApplyResizeMode();
        _resizeModeDescriptor = DependencyPropertyDescriptor.FromProperty(ResizeModeProperty, typeof(FluentWindowBase));
        _resizeModeDescriptor.AddValueChanged(this, ResizeModeChanged);
    }
    private void ApplyResizeMode()
    {
        bool allShown = ResizeMode != ResizeMode.NoResize;
        MinimizeBtn!.Visibility = MaximizeBtn!.Visibility = allShown ? Visibility.Visible : Visibility.Collapsed;
        MaximizeBtn.IsEnabled = ResizeMode != ResizeMode.CanMinimize;
        MaximizeBtn.SetResourceReference(ForegroundProperty, MaximizeBtn.IsEnabled ? "ForeColor" : "FocusMaskColor");
        CloseBtn!.MouseEnter += CloseBtn_MouseEnter;
        CloseBtn!.MouseLeave += FluentWindowBase_MouseLeave;

        _windowChrome.ResizeBorderThickness =
            ResizeMode != ResizeMode.CanResize && ResizeMode != ResizeMode.CanResizeWithGrip
            ? default : _resizeBorderThickness;
        _blurBehavior.WindowChromeEx = _windowChrome;
    }

    private void FluentWindowBase_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        CloseBtn!.SetResourceReference(wsButton.ForegroundProperty, "ForeColor");
    }

    private void CloseBtn_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        CloseBtn!.Foreground = Brushes.White;
    }

    private void ResizeModeChanged(object? sender, EventArgs e)
    {
        ApplyResizeMode();
    }

    public bool ExitOnCloseBtnClicked { get; set; } = true;

    protected virtual void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        if (ExitOnCloseBtnClicked)
            Close();
        else{
            Hide();
        }
    }

    protected virtual void MaximizeBtn_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;          
    }

    protected  void MinimizeBtn_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
}