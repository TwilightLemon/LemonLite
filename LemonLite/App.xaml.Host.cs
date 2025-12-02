using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.ViewModels;
using LemonLite.Views.UserControls;
using LemonLite.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace LemonLite;

public partial class App
{
    public const string AzureLiteHttpClientFlag = "lemonlite.azurewebsites.net";
    private static void BuildHost(IServiceCollection services)
    {
        services.AddHttpClient(AzureLiteHttpClientFlag, client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.9,en;q=0.8");
            //client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
            //client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
        }).ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new System.Net.Http.HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                UseProxy = false
            };
        });
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
