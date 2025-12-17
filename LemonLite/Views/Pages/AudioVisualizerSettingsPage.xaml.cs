using CommunityToolkit.Mvvm.ComponentModel;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using System.Windows;
using System.Windows.Controls;

namespace LemonLite.Views.Pages
{
    /// <summary>
    /// Interaction logic for AudioVisualizerSettingsPage.xaml
    /// </summary>
    [ObservableObject]
    public partial class AudioVisualizerSettingsPage : Page
    {
        private readonly SettingsMgr<AudioVisualizerConfig> settings;

        public AudioVisualizerSettingsPage(AppSettingService appSettingService)
        {
            InitializeComponent();
            DataContext = this;
            settings = appSettingService.GetConfigMgr<AudioVisualizerConfig>();
            Loaded += AudioVisualizerSettingsPage_Loaded;
        }

        private void AudioVisualizerSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            WindowOpacity = settings.Data.Opacity;
            EnableBorderRendering = settings.Data.EnableBorderRendering;
            EnableStripsRendering = settings.Data.EnableStripsRendering;
        }

        [ObservableProperty]
        private double _windowOpacity;

        [ObservableProperty]
        private bool _enableBorderRendering;

        [ObservableProperty]
        private bool _enableStripsRendering;

        partial void OnWindowOpacityChanged(double value)
        {
            settings.Data.Opacity = value;
            settings.TriggerDataChanged();
        }

        partial void OnEnableBorderRenderingChanged(bool value)
        {
            settings.Data.EnableBorderRendering = value;
            settings.TriggerDataChanged();
        }

        partial void OnEnableStripsRenderingChanged(bool value)
        {
            settings.Data.EnableStripsRendering = value;
            settings.TriggerDataChanged();
        }
    }
}
