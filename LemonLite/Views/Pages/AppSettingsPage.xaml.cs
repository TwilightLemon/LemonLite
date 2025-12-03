using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using static LemonLite.Configs.Appearance;

namespace LemonLite.Views.Pages
{
    /// <summary>
    /// Interaction logic for AppSettingsPage.xaml
    /// </summary>
    [ObservableObject]
    public partial class AppSettingsPage : Page
    {
        private readonly SettingsMgr<AppOption> settings;
        private readonly SettingsMgr<Appearance> appearanceSettings;

        public AppSettingsPage(AppSettingService appSettingService)
        {
            InitializeComponent();
            DataContext = this;
            Loaded += AppSettingsPage_Loaded;
            this.Unloaded += AppSettingsPage_Unloaded;
            settings=appSettingService.GetConfigMgr<AppOption>();
            appearanceSettings=appSettingService.GetConfigMgr<Appearance>();
            ColorMode = appearanceSettings.Data.ColorMode;
        }

        private void AppSettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            App.ApplyAppOptions();
        }

        private void AppSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            EnableMainWindow = settings.Data.StartWithMainWindow;
            EnableDesktopLyricWindow = settings.Data.StartWithDesktopLyric;
            LiteServerHost = settings.Data.LiteLyricServerHost;
        }

        [ObservableProperty]
        private bool _enableMainWindow;
        [ObservableProperty]
        private bool _enableDesktopLyricWindow;
        [ObservableProperty]
        private string _liteServerHost = "";
        
        [ObservableProperty]
        private ColorModeType _colorMode;

        partial void OnColorModeChanged(ColorModeType value)
        {
            appearanceSettings.Data.ColorMode = ColorMode;
            App.Services.GetRequiredService<UIResourceService>().UpdateColorMode();
        }

        public bool IsLightMode
        {
            get => ColorMode == ColorModeType.Light;
            set { if (value) ColorMode = ColorModeType.Light; }
        }

        public bool IsDarkMode
        {
            get => ColorMode == ColorModeType.Dark;
            set { if (value) ColorMode = ColorModeType.Dark; }
        }

        public bool IsSystemMode
        {
            get => ColorMode == ColorModeType.Auto;
            set { if (value) ColorMode = ColorModeType.Auto; }
        }
    }
}
