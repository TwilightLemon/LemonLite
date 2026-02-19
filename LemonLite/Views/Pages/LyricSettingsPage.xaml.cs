using CommunityToolkit.Mvvm.ComponentModel;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using System.Windows;
using System.Windows.Controls;

namespace LemonLite.Views.Pages
{
    /// <summary>
    /// Interaction logic for LyricSettingsPage.xaml
    /// </summary>
    [ObservableObject]
    public partial class LyricSettingsPage : Page
    {
        private readonly SettingsMgr<LyricOption> settings;

        public LyricSettingsPage(AppSettingService appSettingService)
        {
            InitializeComponent();
            DataContext = this;
            settings = appSettingService.GetConfigMgr<LyricOption>();
            Loaded += LyricSettingsPage_Loaded;
        }

        private void LyricSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            ShowTranslation = settings.Data.ShowTranslation;
            ShowRomaji = settings.Data.ShowRomaji;
            MainFontSize = settings.Data.FontSize;
            FontSizeMiniMode = settings.Data.FontSizeMiniMode;
            LyricFontFamily = settings.Data.FontFamily;
        }

        [ObservableProperty]
        private bool _showTranslation;

        [ObservableProperty]
        private bool _showRomaji;

        [ObservableProperty]
        private int _mainFontSize;

        [ObservableProperty]
        private int _fontSizeMiniMode;

        [ObservableProperty]
        private string _lyricFontFamily = "";

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

        partial void OnMainFontSizeChanged(int value)
        {
            settings.Data.FontSize = value;
            settings.TriggerDataChanged();
        }

        partial void OnFontSizeMiniModeChanged(int value)
        {
            settings.Data.FontSizeMiniMode = value;
            settings.TriggerDataChanged();
        }

        partial void OnLyricFontFamilyChanged(string value)
        {
            settings.Data.FontFamily = value;
            settings.TriggerDataChanged();
        }
    }
}
