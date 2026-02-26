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

public class NotifyIconService(AppSettingService appSettingService, UIResourceService uiResourceService,SmtcService smtc):IDisposable
{
    private TaskbarIcon? _notifyIcon;
    private readonly SettingsMgr<AppOption> opt = appSettingService.GetConfigMgr<AppOption>();
    private readonly UIResourceService _uiResourceService = uiResourceService;
    private Window? _messageWindow;

    private void UpdateMediaInfo()
    {
        App.Current.Dispatcher.BeginInvoke(async() => _notifyIcon!.ToolTipText = await smtc.SmtcListener.GetMediaInfoAsync() is { PlaybackType: Windows.Media.MediaPlaybackType.Music } info
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
        
        // Create a permanent message-only window for system theme monitoring
        CreateMessageWindow();
        
        // Create TaskbarIcon with ContextMenu
        _notifyIcon = new TaskbarIcon
        {
            ToolTipText = "Lemon Lite",
            IconSource = new System.Windows.Media.Imaging.BitmapImage(
                new System.Uri("pack://application:,,,/LemonLite;component/Resources/icon.ico"))
        };
        UpdateMediaInfo();
        smtc.SmtcListener.MediaPropertiesChanged += (s, e) => UpdateMediaInfo();
        smtc.SmtcListener.SessionChanged += (s, e) => UpdateMediaInfo();

        // Create context menu
        var contextMenu = new ContextMenu();

        _openLrcWindowMenuItem = new MenuItem 
        {
            Header = LocalizationService.Instance["Lyric Window"],
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
            Header = LocalizationService.Instance["Desktop Lyrics"],
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
            Header = LocalizationService.Instance["Audio Visualizer"],
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
        _settingsMenuItem.Click += (s, e) => {
            App.Services.GetRequiredService<SettingsWindow>().Show();
        };

        _refreshMenuItem = new MenuItem { Header = LocalizationService.Instance["Refresh Lyrics"] };
        _refreshMenuItem.Click += async (s, e) =>
        {
            var smtc = App.Services.GetRequiredService<SmtcService>();
            await smtc.StopAsync(default).ContinueWith(_=>smtc.StartAsync(default));
            App.Current.Dispatcher.Invoke(App.ApplyAppOptions);
        };

        _exitMenuItem = new MenuItem { Header = LocalizationService.Instance["Exit"] };
        _exitMenuItem.Click += (s, e) => App.Current.Shutdown();

        contextMenu.Items.Add(_openLrcWindowMenuItem);
        contextMenu.Items.Add(_desktopMenuItem);
        contextMenu.Items.Add(_audioVisualizerMenuItem);
        contextMenu.Items.Add(_settingsMenuItem);
        contextMenu.Items.Add(_refreshMenuItem);
        contextMenu.Items.Add(_exitMenuItem);

        _notifyIcon.ContextMenu = contextMenu;
        //by default, enable efficiency mode as background app
        _notifyIcon.ForceCreate();

        // Listen for language changes
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (_openLrcWindowMenuItem != null)
                _openLrcWindowMenuItem.Header = LocalizationService.Instance["Lyric Window"];
            if (_desktopMenuItem != null)
                _desktopMenuItem.Header = LocalizationService.Instance["Desktop Lyrics"];
            if (_audioVisualizerMenuItem != null)
                _audioVisualizerMenuItem.Header = LocalizationService.Instance["Audio Visualizer"];
            if (_settingsMenuItem != null)
                _settingsMenuItem.Header = LocalizationService.Instance["Settings"];
            if (_refreshMenuItem != null)
                _refreshMenuItem.Header = LocalizationService.Instance["Refresh Lyrics"];
            if (_exitMenuItem != null)
                _exitMenuItem.Header = LocalizationService.Instance["Exit"];
        });
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
        _messageWindow?.Close();
    }

    private void CreateMessageWindow()
    {
        // Create a minimal, invisible window for receiving Windows messages
        _messageWindow = new Window
        {
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            AllowsTransparency = true,
            Background = null,
            Width = 0,
            Height = 0,
            Left = -10000, // Position off-screen
            Top = -10000,
            ShowActivated = false
        };

        _messageWindow.Show();
        _messageWindow.Hide(); // Immediately hide after showing to get HWND

        // Register for system theme changes
        SystemThemeAPI.RegesterOnThemeChanged(_messageWindow, () =>
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                _uiResourceService.UpdateColorMode();
            });
        }, null);
    }
}
