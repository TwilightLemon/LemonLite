using LemonApp.Common.Funcs;
using LemonLite.Configs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LemonLite.Services;

public class NotifyIconService(AppSettingService appSettingService)
{
    System.Windows.Forms.NotifyIcon? NotifyIcon;
    private readonly SettingsMgr<AppOption> opt = appSettingService.GetConfigMgr<AppOption>();
    public void InitNotifyIcon()
    {
        if (NotifyIcon != null) return;
        NotifyIcon = new()
        {
            Icon = Properties.Resources.icon,
            Text = "Lemon Lite",
            Visible = true
        };
        NotifyIcon.MouseClick += NotifyIcon_MouseClick;
    }

    private void NotifyIcon_MouseClick(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        var menu = new ContextMenu()
        {
            Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
            StaysOpen=false
        };

        var openLrcWindow = new MenuItem() { Header = "Open Lyric Window" };
        openLrcWindow.Click += (s, e) =>
        {
            opt.Data.StartWithMainWindow = true;
            App.CreateMainWindow();
        };
        var desktop=new MenuItem() { Header= "Show Desktop Lyrics" };
        desktop.Click += (s, e) =>
        {
            opt.Data.StartWithDesktopLyric = true;
            App.CreateDesktopLyricWindow();
        };
        var exit = new MenuItem() { Header = "Exit" };
        exit.Click += (s, e) => App.Current.Shutdown();


        menu.Items.Add(openLrcWindow);
        menu.Items.Add(desktop);
        menu.Items.Add(exit);
        menu.IsOpen = true;
    }
}
