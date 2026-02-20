using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using LemonLite.Views.Windows;
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
        private readonly SmtcService smtc;

        public AppSettingsPage(AppSettingService appSettingService, SmtcService smtcService)
        {
            InitializeComponent();
            DataContext = this;
            Loaded += AppSettingsPage_Loaded;
            settings=appSettingService.GetConfigMgr<AppOption>();
            appearanceSettings=appSettingService.GetConfigMgr<Appearance>();
            ColorMode = appearanceSettings.Data.ColorMode;
            smtc = smtcService;
        }

        private void AppSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            EnableMainWindow = settings.Data.StartWithMainWindow;
            EnableDesktopLyricWindow = settings.Data.StartWithDesktopLyric;
            EnableAudioVisualizer = settings.Data.EnableAudioVisualizer;
            AppFontFamily = appearanceSettings.Data.DefaultFontFamily;
            BackgroundType = appearanceSettings.Data.Background;
            AcrylicOpacity = appearanceSettings.Data.AcylicOpacity;
            BackgroundImagePath = appearanceSettings.Data.BackgroundImagePath ?? "";
            BackgroundOpacity = appearanceSettings.Data.BackgroundOpacity;
            ShowInTaskbarWhenMiniMode = appearanceSettings.Data.ShowInTaskbarWhenMiniMode;
        }

        [ObservableProperty]
        private bool _enableMainWindow;
        [ObservableProperty]
        private bool _enableDesktopLyricWindow;
        [ObservableProperty]
        private bool _enableAudioVisualizer;

        partial void OnEnableMainWindowChanged(bool value)
        {
            settings.Data.StartWithMainWindow = value;
            if (smtc.IsSessionValid)
                App.WindowManager.SetWindowState<MainWindow>(value);
        }
        partial void OnEnableDesktopLyricWindowChanged(bool value)
        {
            settings.Data.StartWithDesktopLyric = value;
            if (smtc.IsSessionValid)
                App.WindowManager.SetWindowState<DesktopLyricWindow>(value);
        }
        partial void OnEnableAudioVisualizerChanged(bool value)
        {
            settings.Data.EnableAudioVisualizer = value;
            if (smtc.IsSessionValid)
                App.WindowManager.SetWindowState<AudioVisualizerWindow>(value);
        }
        
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

        [ObservableProperty]
        private string _appFontFamily = "";
        private void ApplyAppFontFamily()
        {
            appearanceSettings.Data.DefaultFontFamily = AppFontFamily;
            App.Services.GetRequiredService<UIResourceService>().UpdateAppFontFamily();
        }
        partial void OnAppFontFamilyChanged(string value)
        {
            ApplyAppFontFamily();
        }
 
        [ObservableProperty]
        private BackgroundType _backgroundType;

        partial void OnBackgroundTypeChanged(BackgroundType value)
        {
            appearanceSettings.Data.Background = value;
            appearanceSettings.TriggerDataChanged();
            OnPropertyChanged(nameof(IsBackgroundNone));
            OnPropertyChanged(nameof(IsBackgroundAcrylic));
            OnPropertyChanged(nameof(IsBackgroundImage));
            OnPropertyChanged(nameof(AcrylicSettingsVisibility));
            OnPropertyChanged(nameof(ImageSettingsVisibility));
        }

        public bool IsBackgroundNone
        {
            get => BackgroundType == BackgroundType.None;
            set { if (value) BackgroundType = BackgroundType.None; }
        }

        public bool IsBackgroundAcrylic
        {
            get => BackgroundType == BackgroundType.Acrylic;
            set { if (value) BackgroundType = BackgroundType.Acrylic; }
        }

        public bool IsBackgroundImage
        {
            get => BackgroundType == BackgroundType.Image;
            set { if (value) BackgroundType = BackgroundType.Image; }
        }

        public Visibility AcrylicSettingsVisibility => BackgroundType == BackgroundType.Acrylic ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ImageSettingsVisibility => BackgroundType == BackgroundType.Image ? Visibility.Visible : Visibility.Collapsed;

        [ObservableProperty]
        private double _acrylicOpacity;

        partial void OnAcrylicOpacityChanged(double value)
        {
            appearanceSettings.Data.AcylicOpacity = value;
            appearanceSettings.TriggerDataChanged();
        }

        [ObservableProperty]
        private string _backgroundImagePath = "";

        [RelayCommand]
        private void BrowseBackgroundImage()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Background Image",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                BackgroundImagePath = dialog.FileName;
                appearanceSettings.Data.BackgroundImagePath = dialog.FileName;
                appearanceSettings.TriggerDataChanged();
            }
        }

        // Background Opacity
        [ObservableProperty]
        private double _backgroundOpacity;

        partial void OnBackgroundOpacityChanged(double value)
        {
            appearanceSettings.Data.BackgroundOpacity = value;
            appearanceSettings.TriggerDataChanged();
        }

        [ObservableProperty]
        private bool _showInTaskbarWhenMiniMode;
        partial void OnShowInTaskbarWhenMiniModeChanged(bool value)
        {
            appearanceSettings.Data.ShowInTaskbarWhenMiniMode = value;
            appearanceSettings.TriggerDataChanged();
        }
    }
}
