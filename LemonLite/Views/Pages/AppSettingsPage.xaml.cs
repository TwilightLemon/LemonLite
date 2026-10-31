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
    [ObservableObject]
    public partial class AppSettingsPage : Page
    {
        private readonly SettingsMgr<AppOption> settings;
        private readonly SettingsMgr<Appearance> appearanceSettings;
        private readonly SettingsMgr<HotkeyConfig> hotkeySettings;
        private readonly GlobalHotkeyService _hotkeyService;
        private readonly SmtcService smtc;

        public AppSettingsPage(AppSettingService appSettingService, SmtcService smtcService, GlobalHotkeyService hotkeyService)
        {
            InitializeComponent();
            DataContext = this;
            Loaded += AppSettingsPage_Loaded;
            Unloaded += AppSettingsPage_Unloaded;
            settings=appSettingService.GetConfigMgr<AppOption>();
            appearanceSettings=appSettingService.GetConfigMgr<Appearance>();
            hotkeySettings=appSettingService.GetConfigMgr<HotkeyConfig>();
            _hotkeyService = hotkeyService;
            ColorMode = appearanceSettings.Data.ColorMode;
            smtc = smtcService;
        }

        private void AppSettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            var localizationService = LocalizationService.Instance;
            localizationService.LanguageChanged -= OnLanguageChanged;
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

            EnableGlobalHotkeys = hotkeySettings.Data.EnableGlobalHotkeys;
            PlayPauseHotkey = hotkeySettings.Data.PlayPause.Clone();
            PlayNextHotkey = hotkeySettings.Data.PlayNext.Clone();
            PlayPreviousHotkey = hotkeySettings.Data.PlayPrevious.Clone();
            UpdateConflictStates();

            var localizationService = LocalizationService.Instance;
            localizationService.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            OnPropertyChanged(nameof(IsEnglishLanguage));
            OnPropertyChanged(nameof(IsChineseLanguage));
            UpdateConflictStates();
        }

        [ObservableProperty]
        private bool _enableMainWindow;
        [ObservableProperty]
        private bool _enableDesktopLyricWindow;
        [ObservableProperty]
        private bool _enableAudioVisualizer;
        [ObservableProperty]
        private bool _enableEmbeddedWindow;

        partial void OnEnableEmbeddedWindowChanged(bool value)
        {
            settings.Data.StartWithEmbeddedWindow = value;
             if (smtc.IsSessionValid)
                App.WindowManager.SetWindowState<EmbeddedWindow>(value);
        }

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
        private bool _enableGlobalHotkeys;

        partial void OnEnableGlobalHotkeysChanged(bool value)
        {
            hotkeySettings.Data.EnableGlobalHotkeys = value;
            hotkeySettings.TriggerDataChanged();
            OnPropertyChanged(nameof(HotkeySettingsVisibility));
            _hotkeyService.RegisterAllHotkeys();
            if (!value)
            {
                PlayPauseConflict = false;
                PlayNextConflict = false;
                PlayPreviousConflict = false;
                ConflictWarningText = "";
                OnPropertyChanged(nameof(ConflictWarningVisibility));
            }
            else
            {
                UpdateConflictStates();
            }
        }

        public Visibility HotkeySettingsVisibility => EnableGlobalHotkeys ? Visibility.Visible : Visibility.Collapsed;

        [ObservableProperty]
        private HotkeyBinding _playPauseHotkey = new();

        partial void OnPlayPauseHotkeyChanged(HotkeyBinding value)
        {
            if (hotkeySettings == null) return;
            hotkeySettings.Data.PlayPause = value.Clone();
            hotkeySettings.TriggerDataChanged();
            _hotkeyService.RegisterAllHotkeys();
            UpdateConflictStates();
        }

        [ObservableProperty]
        private HotkeyBinding _playNextHotkey = new();

        partial void OnPlayNextHotkeyChanged(HotkeyBinding value)
        {
            if (hotkeySettings == null) return;
            hotkeySettings.Data.PlayNext = value.Clone();
            hotkeySettings.TriggerDataChanged();
            _hotkeyService.RegisterAllHotkeys();
            UpdateConflictStates();
        }

        [ObservableProperty]
        private HotkeyBinding _playPreviousHotkey = new();

        partial void OnPlayPreviousHotkeyChanged(HotkeyBinding value)
        {
            if (hotkeySettings == null) return;
            hotkeySettings.Data.PlayPrevious = value.Clone();
            hotkeySettings.TriggerDataChanged();
            _hotkeyService.RegisterAllHotkeys();
            UpdateConflictStates();
        }

        [ObservableProperty]
        private bool _playPauseConflict;
        [ObservableProperty]
        private bool _playNextConflict;
        [ObservableProperty]
        private bool _playPreviousConflict;

        [ObservableProperty]
        private string _conflictWarningText = "";
        public Visibility ConflictWarningVisibility => string.IsNullOrEmpty(ConflictWarningText) ? Visibility.Collapsed : Visibility.Visible;

        private void UpdateConflictStates()
        {
            PlayPauseConflict = !_hotkeyService.IsPlayPauseRegistered && EnableGlobalHotkeys && !hotkeySettings.Data.PlayPause.IsEmpty;
            PlayNextConflict = !_hotkeyService.IsPlayNextRegistered && EnableGlobalHotkeys && !hotkeySettings.Data.PlayNext.IsEmpty;
            PlayPreviousConflict = !_hotkeyService.IsPlayPreviousRegistered && EnableGlobalHotkeys && !hotkeySettings.Data.PlayPrevious.IsEmpty;

            var conflicts = new System.Collections.Generic.List<string>();
            if (PlayPauseConflict && _hotkeyService.PlayPauseConflictMessage != null)
                conflicts.Add(_hotkeyService.PlayPauseConflictMessage);
            if (PlayNextConflict && _hotkeyService.PlayNextConflictMessage != null)
                conflicts.Add(_hotkeyService.PlayNextConflictMessage);
            if (PlayPreviousConflict && _hotkeyService.PlayPreviousConflictMessage != null)
                conflicts.Add(_hotkeyService.PlayPreviousConflictMessage);

            ConflictWarningText = conflicts.Count > 0
                ? LocalizationService.Instance["HotkeyConflict"] + "\n" + string.Join("\n", conflicts)
                : "";
            OnPropertyChanged(nameof(ConflictWarningVisibility));
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

        public bool IsEnglishLanguage
        {
            get => LocalizationService.Instance.CurrentLanguage == "en";
            set { if (value) LocalizationService.Instance.SetLanguage("en"); }
        }

        public bool IsChineseLanguage
        {
            get => LocalizationService.Instance.CurrentLanguage == "zh";
            set { if (value) LocalizationService.Instance.SetLanguage("zh"); }
        }
    }
}
