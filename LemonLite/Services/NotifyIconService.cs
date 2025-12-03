using LemonLite.Utils;
using LemonLite.Configs;
using System.Windows.Controls;
using System.Windows;
using H.NotifyIcon;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using LemonLite.Views.Windows;

namespace LemonLite.Services;

public class NotifyIconService(AppSettingService appSettingService, UIResourceService uiResourceService)
{
    private TaskbarIcon? _notifyIcon;
    private readonly SettingsMgr<AppOption> opt = appSettingService.GetConfigMgr<AppOption>();
    private readonly UIResourceService _uiResourceService = uiResourceService;
    private Window? _messageWindow;

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
            App.ApplyAppOptions();
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
            App.ApplyAppOptions();
        };

        var settings =new MenuItem { Header = "Settings" };
        settings.Click += (s, e) => {
            App.Services.GetRequiredService<SettingsWindow>().Show();
        };

        var exit = new MenuItem { Header = "Exit" };
        exit.Click += (s, e) => App.Current.Shutdown();

        contextMenu.Items.Add(openLrcWindow);
        contextMenu.Items.Add(desktop);
        contextMenu.Items.Add(settings);
        contextMenu.Items.Add(exit);

        _notifyIcon.ContextMenu = contextMenu;
        _notifyIcon.ForceCreate(true);
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
