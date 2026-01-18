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

#if !DEBUG
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
#endif 
        
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
    public static WindowInstanceManager WindowManager => Services.GetRequiredService<WindowInstanceManager>();

    public static void ApplyAppOptions()
    {
        var smtc = Services.GetRequiredService<SmtcService>();
        var opt = Services.GetRequiredService<AppSettingService>().GetConfigMgr<AppOption>();

        if (smtc.IsSessionValid)
        {
            WindowManager.SetWindowState<MainWindow>(opt.Data.StartWithMainWindow);
            WindowManager.SetWindowState<DesktopLyricWindow>(opt.Data.StartWithDesktopLyric);
            WindowManager.SetWindowState<AudioVisualizerWindow>(opt.Data.EnableAudioVisualizer);
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
        Services.GetRequiredService<NotifyIconService>().Dispose();
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
