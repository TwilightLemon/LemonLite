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

        var openLrcWindow = new MenuItem 
        { 
            Header = "Lyric Window", 
            IsCheckable = true, 
            IsChecked = opt.Data.StartWithMainWindow 
        };
        openLrcWindow.Click += (s, e) =>
        {
            opt.Data.StartWithMainWindow = !opt.Data.StartWithMainWindow;
            openLrcWindow.IsChecked = opt.Data.StartWithMainWindow;
            App.WindowManager.SetWindowState<MainWindow>(opt.Data.StartWithMainWindow);
        };

        var desktop = new MenuItem 
        { 
            Header = "Desktop Lyrics", 
            IsCheckable = true, 
            IsChecked = opt.Data.StartWithDesktopLyric 
        };
        desktop.Click += (s, e) =>
        {
            opt.Data.StartWithDesktopLyric = !opt.Data.StartWithDesktopLyric;
            desktop.IsChecked = opt.Data.StartWithDesktopLyric;
            App.WindowManager.SetWindowState<DesktopLyricWindow>(opt.Data.StartWithDesktopLyric);
        };

        var audioVisualizer = new MenuItem
        {
            Header = "Audio Visualizer",
            IsCheckable = true,
            IsChecked = opt.Data.EnableAudioVisualizer
        };
        audioVisualizer.Click += (s, e) =>
        {
            opt.Data.EnableAudioVisualizer = !opt.Data.EnableAudioVisualizer;
            audioVisualizer.IsChecked = opt.Data.EnableAudioVisualizer;
            App.WindowManager.SetWindowState<AudioVisualizerWindow>(opt.Data.EnableAudioVisualizer);
        };

        var settings =new MenuItem { Header = "Settings" };
        settings.Click += (s, e) => {
            App.Services.GetRequiredService<SettingsWindow>().Show();
        };

        var refresh = new MenuItem { Header = "Refresh Lyrics" };
        refresh.Click += async (s, e) =>
        {
            var smtc = App.Services.GetRequiredService<SmtcService>();
            await smtc.StopAsync(default).ContinueWith(_=>smtc.StartAsync(default));
            App.Current.Dispatcher.Invoke(App.ApplyAppOptions);
        };

        var exit = new MenuItem { Header = "Exit" };
        exit.Click += (s, e) => App.Current.Shutdown();

        contextMenu.Items.Add(openLrcWindow);
        contextMenu.Items.Add(desktop);
        contextMenu.Items.Add(audioVisualizer);
        contextMenu.Items.Add(settings);
        contextMenu.Items.Add(refresh);
        contextMenu.Items.Add(exit);

        _notifyIcon.ContextMenu = contextMenu;
        //by default, enable efficiency mode as background app
        _notifyIcon.ForceCreate();
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
