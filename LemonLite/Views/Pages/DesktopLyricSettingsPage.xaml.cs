using CommunityToolkit.Mvvm.ComponentModel;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using System.Windows;
using System.Windows.Controls;

namespace LemonLite.Views.Pages
{
    /// <summary>
    /// Interaction logic for DesktopLyricSettingsPage.xaml
    /// </summary>
    [ObservableObject]
    public partial class DesktopLyricSettingsPage : Page
    {
        private readonly SettingsMgr<DesktopLyricOption> settings;

        public DesktopLyricSettingsPage(AppSettingService appSettingService)
        {
            InitializeComponent();
            DataContext = this;
            settings = appSettingService.GetConfigMgr<DesktopLyricOption>();
            Loaded += DesktopLyricSettingsPage_Loaded;
        }

        private void DesktopLyricSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            ShowTranslation = settings.Data.ShowTranslation;
            ShowRomaji = settings.Data.ShowRomaji;
            LrcFontSize = settings.Data.LrcFontSize;
            LyricFontFamily = settings.Data.FontFamily;
            EnableBackground = settings.Data.EnableBackground;
            UseHighlightLyricEffect = settings.Data.UseHighlightLyricEffect;
        }

        [ObservableProperty]
        private bool _showTranslation;

        [ObservableProperty]
        private bool _showRomaji;

        [ObservableProperty]
        private double _lrcFontSize;

        [ObservableProperty]
        private string _lyricFontFamily = "";

        [ObservableProperty]
        private bool _enableBackground = false;

        [ObservableProperty]
        private bool _useHighlightLyricEffect = false;

        partial void OnUseHighlightLyricEffectChanged(bool value)
        {
            settings.Data.UseHighlightLyricEffect = value;
            settings.TriggerDataChanged();
        }

        partial void OnEnableBackgroundChanged(bool value)
        {
            settings.Data.EnableBackground = value;
            settings.TriggerDataChanged();
        }

        partial void OnShowTranslationChanged(bool value)
        {
            settings.Data.ShowTranslation = value;
            settings.TriggerDataChanged();
        }

        partial void OnShowRomajiChanged(bool value)
        {
            settings.Data.ShowRomaji = value;
            settings.TriggerDataChanged();
        }

        partial void OnLrcFontSizeChanged(double value)
        {
            settings.Data.LrcFontSize = value;
            settings.TriggerDataChanged();
        }

        partial void OnLyricFontFamilyChanged(string value)
        {
            settings.Data.FontFamily = value;
            settings.TriggerDataChanged();
        }
    }
}
