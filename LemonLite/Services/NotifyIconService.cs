using LemonLite.Utils;
using LemonLite.Configs;
using System.Windows.Controls;
using System.Windows;
using H.NotifyIcon;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using LemonLite.Views.Windows;
using System.Threading.Tasks;
using System.Windows.Media;
using System;

namespace LemonLite.Services;

public class NotifyIconService(AppSettingService appSettingService, UIResourceService uiResourceService, SmtcService smtc) : IDisposable
{
    private TaskbarIcon? _notifyIcon;
    private readonly SettingsMgr<AppOption> opt = appSettingService.GetConfigMgr<AppOption>();
    private readonly UIResourceService _uiResourceService = uiResourceService;
    private Window? _messageWindow;
    private ContextMenu? _contextMenu;

    private void UpdateMediaInfo()
    {
        App.Current.Dispatcher.BeginInvoke(async () => _notifyIcon!.ToolTipText = await smtc.SmtcListener.GetMediaInfoAsync() is { PlaybackType: Windows.Media.MediaPlaybackType.Music } info
            ? $"Lemon Lite \r\nPlaying: {info.Title} - {info.Artist}"
            : "Lemon Lite");
    }

    private MenuItem? _openLrcWindowMenuItem;
    private MenuItem? _desktopMenuItem;
    private MenuItem? _audioVisualizerMenuItem;
    private MenuItem? _settingsMenuItem;
    private MenuItem? _refreshMenuItem;
    private MenuItem? _exitMenuItem;

    public void InitNotifyIcon()
    {
        if (_notifyIcon != null) return;

        CreateMessageWindow();

        _notifyIcon = new TaskbarIcon
        {
            ToolTipText = "Lemon Lite",
            IconSource = new System.Windows.Media.Imaging.BitmapImage(
                new System.Uri("pack://application:,,,/LemonLite;component/Resources/icon.ico"))
        };
        UpdateMediaInfo();
        smtc.SmtcListener.MediaPropertiesChanged += (s, e) => UpdateMediaInfo();
        smtc.SmtcListener.SessionChanged += (s, e) => UpdateMediaInfo();

        _contextMenu = new ContextMenu();

        _openLrcWindowMenuItem = new MenuItem
        {
            Header = LocalizationService.Instance["LyricWindow"],
            IsCheckable = true,
            IsChecked = opt.Data.StartWithMainWindow
        };
        _openLrcWindowMenuItem.Click += (s, e) =>
        {
            opt.Data.StartWithMainWindow = !opt.Data.StartWithMainWindow;
            _openLrcWindowMenuItem.IsChecked = opt.Data.StartWithMainWindow;
            App.WindowManager.SetWindowState<MainWindow>(opt.Data.StartWithMainWindow);
        };

        _desktopMenuItem = new MenuItem
        {
            Header = LocalizationService.Instance["DesktopLyrics"],
            IsCheckable = true,
            IsChecked = opt.Data.StartWithDesktopLyric
        };
        _desktopMenuItem.Click += (s, e) =>
        {
            opt.Data.StartWithDesktopLyric = !opt.Data.StartWithDesktopLyric;
            _desktopMenuItem.IsChecked = opt.Data.StartWithDesktopLyric;
            App.WindowManager.SetWindowState<DesktopLyricWindow>(opt.Data.StartWithDesktopLyric);
        };

        _audioVisualizerMenuItem = new MenuItem
        {
            Header = LocalizationService.Instance["AudioVisualizer"],
            IsCheckable = true,
            IsChecked = opt.Data.EnableAudioVisualizer
        };
        _audioVisualizerMenuItem.Click += (s, e) =>
        {
            opt.Data.EnableAudioVisualizer = !opt.Data.EnableAudioVisualizer;
            _audioVisualizerMenuItem.IsChecked = opt.Data.EnableAudioVisualizer;
            App.WindowManager.SetWindowState<AudioVisualizerWindow>(opt.Data.EnableAudioVisualizer);
        };

        _settingsMenuItem = new MenuItem { Header = LocalizationService.Instance["Settings"] };
        _settingsMenuItem.Click += (s, e) =>
        {
            App.Services.GetRequiredService<SettingsWindow>().Show();
        };

        _refreshMenuItem = new MenuItem { Header = LocalizationService.Instance["RefreshLyrics"] };
        _refreshMenuItem.Click += async (s, e) =>
        {
            var smtc = App.Services.GetRequiredService<SmtcService>();
            await smtc.StopAsync(default).ContinueWith(_ => smtc.StartAsync(default));
            App.Current.Dispatcher.Invoke(App.ApplyAppOptions);
        };

        _exitMenuItem = new MenuItem { Header = LocalizationService.Instance["Exit"] };
        _exitMenuItem.Click += (s, e) => App.Current.Shutdown();

        _contextMenu.Items.Add(_openLrcWindowMenuItem);
        _contextMenu.Items.Add(_desktopMenuItem);
        _contextMenu.Items.Add(_audioVisualizerMenuItem);
        _contextMenu.Items.Add(_settingsMenuItem);
        _contextMenu.Items.Add(_refreshMenuItem);
        _contextMenu.Items.Add(_exitMenuItem);

        _notifyIcon.ContextMenu = _contextMenu;
        _notifyIcon.ForceCreate();

        // 初始应用当前主题颜色
        ApplyThemeColors();

        // 订阅主题变更
        _uiResourceService.OnColorModeChanged += OnColorModeChanged;

        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>
    /// 从 App 全局资源里取颜色，手动刷新 ContextMenu 和所有 MenuItem 的颜色。
    /// ContextMenu 不在可视树中，DynamicResource 不生效，必须用代码设置。
    /// </summary>
    private void ApplyThemeColors()
    {
        if (_contextMenu == null) return;

        var bg = App.Current.Resources["BackgroundColor"] as Brush ?? new SolidColorBrush(Colors.White);
        var fg = App.Current.Resources["ForeColor"] as Brush ?? new SolidColorBrush(Colors.Black);
        var hover = App.Current.Resources["MaskColor"] as Brush ?? new SolidColorBrush(Color.FromArgb(30, 128, 128, 128));

        _contextMenu.Background = bg;
        _contextMenu.Foreground = fg;
        _contextMenu.BorderThickness = new Thickness(0);

        foreach (var item in _contextMenu.Items)
        {
            if (item is MenuItem mi)
            {
                mi.Background = bg;
                mi.Foreground = fg;
            }
        }
    }

    private void OnColorModeChanged()
    {
        App.Current.Dispatcher.Invoke(ApplyThemeColors);
    }

    private void OnLanguageChanged()
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (_openLrcWindowMenuItem != null)
                _openLrcWindowMenuItem.Header = LocalizationService.Instance["LyricWindow"];
            if (_desktopMenuItem != null)
                _desktopMenuItem.Header = LocalizationService.Instance["DesktopLyrics"];
            if (_audioVisualizerMenuItem != null)
                _audioVisualizerMenuItem.Header = LocalizationService.Instance["AudioVisualizer"];
            if (_settingsMenuItem != null)
                _settingsMenuItem.Header = LocalizationService.Instance["Settings"];
            if (_refreshMenuItem != null)
                _refreshMenuItem.Header = LocalizationService.Instance["RefreshLyrics"];
            if (_exitMenuItem != null)
                _exitMenuItem.Header = LocalizationService.Instance["Exit"];
        });
    }

    public void Dispose()
    {
        _uiResourceService.OnColorModeChanged -= OnColorModeChanged;
        LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
        _notifyIcon?.Dispose();
        _messageWindow?.Close();
    }

    private void CreateMessageWindow()
    {
        _messageWindow = new Window
        {
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            AllowsTransparency = true,
            Background = null,
            Width = 0,
            Height = 0,
            Left = -10000,
            Top = -10000,
            ShowActivated = false
        };

        _messageWindow.Show();
        _messageWindow.Hide();

        SystemThemeAPI.RegesterOnThemeChanged(_messageWindow, () =>
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _uiResourceService.UpdateColorMode();
            });
        }, null);
    }
}
