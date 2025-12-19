using FluentWpfCore.Helpers;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using LemonLite.Views.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LemonLite.Views.Windows
{
    /// <summary>
    /// Interaction logic for AudioVisualizerWindow.xaml
    /// </summary>
    public partial class AudioVisualizerWindow : Window
    {
        private readonly SettingsMgr<AudioVisualizerConfig> settings;
        private readonly SmtcService smtcService;
        private readonly nint WS_EX_TRANSPARENT=0x20;

        public AudioVisualizerWindow(AppSettingService settingService, SmtcService smtcService)
        {
            InitializeComponent();
            settings=settingService.GetConfigMgr<AudioVisualizerConfig>();
            settings.OnDataChanged += Settings_OnDataChanged;
            ApplySettings();
            this.smtcService = smtcService;
            smtcService.SmtcListener.SessionChanged += UpdatePlayingState;
            smtcService.SmtcListener.SessionExited += UpdatePlayingState;
            smtcService.SmtcListener.PlaybackInfoChanged += UpdatePlayingState;
            visualizerControl.RenderEnabled = smtcService.IsSessionValid && smtcService.IsPlaying;
            Closed += AudioVisualizerWindow_Closed;
            SourceInitialized += AudioVisualizerWindow_SourceInitialized;
        }

        private void AudioVisualizerWindow_SourceInitialized(object? sender, EventArgs e)
        {
            nint hwnd = new WindowInteropHelper(this).Handle;
            nint style = WindowFlagsHelper.GetWindowLong(hwnd, (int)WindowFlagsHelper.GetWindowLongFields.GWL_EXSTYLE)
                                | WS_EX_TRANSPARENT
                                | (int)WindowFlagsHelper.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            WindowFlagsHelper.SetWindowLong(hwnd, (int)WindowFlagsHelper.GetWindowLongFields.GWL_EXSTYLE, style);
        }

        private void AudioVisualizerWindow_Closed(object? sender, EventArgs e)
        {
            visualizerControl.RenderEnabled = false;
            settings.OnDataChanged -= Settings_OnDataChanged;
            smtcService.SmtcListener.SessionChanged -= UpdatePlayingState;
            smtcService.SmtcListener.SessionExited -= UpdatePlayingState;
            smtcService.SmtcListener.PlaybackInfoChanged -= UpdatePlayingState;
        }

        private void UpdatePlayingState(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                visualizerControl.RenderEnabled = smtcService.IsSessionValid && smtcService.IsPlaying;
            });
        }

        private void Settings_OnDataChanged()
        {
            ApplySettings();
        }

        private void ApplySettings()
        {
            var config = settings.Data;
            this.Opacity = config.Opacity;
            visualizerControl.EnableBorderRendering = config.EnableBorderRendering;
            visualizerControl.EnableStripsRendering = config.EnableStripsRendering;
        }
    }
}
