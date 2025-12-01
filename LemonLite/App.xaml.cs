using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using LemonLite.ViewModels;
using LemonLite.Views.UserControls;
using LemonLite.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
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
        Host = new HostBuilder().ConfigureServices(BuildHost).Build();
        Startup += App_Startup;
        Exit += App_Exit;
    }

    private static void BuildHost(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddHostedService(p=>p.GetRequiredService<AppSettingService>()
                                                            .AddConfig<LyricOption>()
                                                            .AddConfig<Appearance>()
                                                            .AddConfig<DesktopLyricOption>()
                                                            .AddConfig<AppOption>());
        services.AddHostedService(p => p.GetRequiredService<SmtcService>());

        services.AddSingleton<AppSettingService>();
        services.AddSingleton<NotifyIconService>();
        services.AddSingleton<UIResourceService>();
        services.AddSingleton<SmtcService>();
        services.AddSingleton<LyricService>();

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
        services.AddTransient<LyricView>();

        services.AddTransient<DesktopLyricWindow>();
        services.AddTransient<DesktopLyricWindowViewModel>();
    }
    private IHost Host { get; init; }
    public static new App Current => (App)Application.Current;
    public static IServiceProvider Services => Current.Host.Services;
    public static void CreateMainWindow()
    {
        if(Current.MainWindow is { IsLoaded: true} window)
        {
            window.Activate();
            return;
        }
        Current.MainWindow = Services.GetRequiredService<MainWindow>();
        Current.MainWindow.Show();

        SystemThemeAPI.RegesterOnThemeChanged(Current.MainWindow, () => {
            var ui = Services.GetRequiredService<UIResourceService>();
            ui.UpdateColorMode();
        }, null);
    }
    public static void CreateDesktopLyricWindow()
    {
        var window = Services.GetRequiredService<DesktopLyricWindow>();
        window.Show();
        window.Activate();
    }
    private async void App_Startup(object sender, StartupEventArgs e)
    {
        Host.Start();
        Services.GetRequiredService<NotifyIconService>().InitNotifyIcon();
    }
    private void App_Exit(object sender, ExitEventArgs e)
    {
        Host.StopAsync().Wait();
    }
}
