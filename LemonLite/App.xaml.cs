using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using LemonLite.ViewModels;
using LemonLite.Views.UserControls;
using LemonLite.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LemonLite;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        Settings.LoadPath();

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        
        Host = new HostBuilder().ConfigureServices(BuildHost).Build();
        Startup += App_Startup;
        Exit += App_Exit;

        //override default style for Pages
        FrameworkElement.StyleProperty.OverrideMetadata(typeof(System.Windows.Controls.Page), new FrameworkPropertyMetadata
        {
            DefaultValue = App.Current.FindResource(typeof(System.Windows.Controls.Page))
        });
    }

    public static new App Current => (App)Application.Current;
    public DesktopLyricWindow? DesktopLyricWindowInstance { get; set; }
    public new MainWindow? MainWindow { get; set; }
    private static readonly object _applyOptionsLock = new();
    public static void CreateMainWindow()
    {
        if (Current.MainWindow is { IsLoaded: true } window)
        {
            window.Activate();
            return;
        }
        Current.MainWindow = Services.GetRequiredService<MainWindow>();
        Current.MainWindow.Closed += MainWindow_Closed;
        Current.MainWindow.Show();
    }

    private static void MainWindow_Closed(object? sender, EventArgs e)
    {
        Current.MainWindow = null;
    }

    public static void DestroyMainWindow()
    {
        if (Current.MainWindow is { } window)
        {
            window.Close();
            Current.MainWindow = null;
        }
    }
    public static void CreateDesktopLyricWindow()
    {
        if (Current.DesktopLyricWindowInstance is { IsLoaded: true } exist)
        {
            exist.Activate();
            return;
        }
        var window = Services.GetRequiredService<DesktopLyricWindow>();
        window.Closed += DesktopLyricWindow_Closed;
        window.Show();
        Current.DesktopLyricWindowInstance = window;
    }

    private static void DesktopLyricWindow_Closed(object? sender, EventArgs e)
    {
        Current.DesktopLyricWindowInstance = null;
    }

    public static void DestroyDesktopLyricWindow()
    {
        if (Current.DesktopLyricWindowInstance is { } window)
        {
            window.Close();
            Current.DesktopLyricWindowInstance = null;
        }
    }

    public static void ApplyAppOptions()
    {
        var smtc = Services.GetRequiredService<SmtcService>();
        var opt = Services.GetRequiredService<AppSettingService>().GetConfigMgr<AppOption>();
        LyricHelper.EndPoint = opt.Data.LiteLyricServerHost;

        if (smtc.IsSessionValid)
        {
            lock (_applyOptionsLock)
            {
                if (opt.Data.StartWithMainWindow && Current.MainWindow == null)
                {
                    CreateMainWindow();
                }
                else if (!opt.Data.StartWithMainWindow && Current.MainWindow != null)
                {
                    DestroyMainWindow();
                }

                if (opt.Data.StartWithDesktopLyric && Current.DesktopLyricWindowInstance == null)
                {
                    CreateDesktopLyricWindow();
                }
                else if (!opt.Data.StartWithDesktopLyric && Current.DesktopLyricWindowInstance != null)
                {
                    DestroyDesktopLyricWindow();
                }
            }

        }
    }
    private async void App_Startup(object sender, StartupEventArgs e)
    {
        Host.Start();
        Services.GetRequiredService<NotifyIconService>().InitNotifyIcon();

        ApplyAppOptions();
        var smtc = Services.GetRequiredService<SmtcService>();
        smtc.SmtcListener.SessionChanged += delegate
        {
            Dispatcher.Invoke(ApplyAppOptions);
        };
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {
        Host.StopAsync().Wait();
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Logger.Fatal("AppDomain Unhandled Exception", ex);
        }
    }

    private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error("Dispatcher Unhandled Exception", e.Exception);
        e.Handled = true;
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logger.Error("TaskScheduler Unobserved Task Exception", e.Exception);
        e.SetObserved();
    }
}
