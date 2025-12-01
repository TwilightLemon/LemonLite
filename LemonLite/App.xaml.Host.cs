using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.ViewModels;
using LemonLite.Views.UserControls;
using LemonLite.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace LemonLite;

public partial class App
{
    private static void BuildHost(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddHostedService(p => p.GetRequiredService<AppSettingService>()
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
    public static IServiceProvider Services => Current.Host.Services;
}
