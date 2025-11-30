using CommunityToolkit.Mvvm.Input;
using LemonApp.Common.Funcs;
using LemonLite.Configs;
using LemonLite.Entities;
using LemonLite.Services;
using LemonLite.Utils;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

//TODO: 提供效果选择
namespace LemonLite.Views.UserControls
{

    /// <summary>
    /// LyricView.xaml 的交互逻辑
    /// </summary>
    public partial class LyricView : UserControl
    {
        private readonly SettingsMgr<LyricOption> _settings;

        public LyricView(AppSettingService appSettingService)
        {
            InitializeComponent();
            _settings = appSettingService.GetConfigMgr<LyricOption>();
            _settings.OnDataChanged += Settings_OnDataChanged;
            Loaded += LyricView_Loaded;
        }


        #region Self-adaption
        /// <summary>
        /// respond to LyricOption changed
        /// </summary>
        private void LyricView_Loaded(object sender, RoutedEventArgs e)
        {
            ApplySettings();
        }

        private async void Settings_OnDataChanged()
        {
            await _settings.LoadAsync();
            ApplySettings();
        }
        private void ApplySettings()
        {
            this.Dispatcher.Invoke(() =>
            {
                IsShowTranslation = _settings?.Data?.ShowTranslation is true;
                IsShowRomaji = _settings?.Data?.ShowRomaji is true;
                SetFontSize(_settings?.Data?.FontSize ?? (int)LyricFontSize);
                this.FontFamily = new FontFamily(_settings?.Data?.FontFamily ?? "Segou UI");
            });
        }

        private void RefreshHostSettings()
        {
            LrcHost.SetShowTranslation(_settings.Data.ShowTranslation&&IsTranslationAvailable);
            LrcHost.SetShowRomaji(_settings.Data.ShowRomaji&&IsRomajiAvailable);
            LrcHost.ApplyFontSize(_settings.Data.FontSize, LyricFontSizeScale);
        }

        #endregion

        #region Apperance
        public double LyricFontSize = 24;
        public const double LyricFontSizeScale = 0.6;
        #endregion

        [RelayCommand]
        public void FontSizeUp() => SetFontSize((int)LyricFontSize +2);
        [RelayCommand]
        public void FontSizeDown() => SetFontSize((int)LyricFontSize - 2);
        public void SetFontSize(int size)
        {
            LyricFontSize = size;
            _settings.Data.FontSize = size;
            LrcHost.ApplyFontSize(size,LyricFontSizeScale);
            LrcHost.ScrollToCurrent();
        }


        public bool IsRomajiAvailable
        {
            get { return (bool)GetValue(IsRomajiAvailableProperty); }
            private set { SetValue(IsRomajiAvailableProperty, value); }
        }

        public static readonly DependencyProperty IsRomajiAvailableProperty =
            DependencyProperty.Register("IsRomajiAvailable", typeof(bool), typeof(LyricView), new PropertyMetadata(false));


        public bool IsTranslationAvailable
        {
            get { return (bool)GetValue(IsTranslationAvailableProperty); }
            private set { SetValue(IsTranslationAvailableProperty, value); }
        }

        public static readonly DependencyProperty IsTranslationAvailableProperty =
            DependencyProperty.Register("IsTranslationAvailable", typeof(bool), typeof(LyricView), new PropertyMetadata(false));

        public bool IsShowTranslation
        {
            get => (bool)GetValue(IsShowTranslationProperty);
            set => SetValue(IsShowTranslationProperty, value);
        }

        public static readonly DependencyProperty IsShowTranslationProperty =
            DependencyProperty.Register("IsShowTranslation", typeof(bool), typeof(LyricView), new PropertyMetadata(true, OnIsShowTranslationChanged));

        private static void OnIsShowTranslationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LyricView view)
            {
                view.SetShowTranslation(e.NewValue is true);
            }
        }

        public bool IsShowRomaji
        {
            get => (bool)GetValue(IsShowRomajiProperty);
            set => SetValue(IsShowRomajiProperty, value);
        }

        public static readonly DependencyProperty IsShowRomajiProperty =
            DependencyProperty.Register("IsShowRomaji", typeof(bool), typeof(LyricView), new PropertyMetadata(true, OnIsShowRomajiChanged));

        private static void OnIsShowRomajiChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LyricView view)
            {
                view.SetShowRomaji(e.NewValue is true);
            }
        }

        public void SetShowTranslation(bool show)
        {
            _settings.Data.ShowTranslation = show;
            LrcHost.SetShowTranslation(show);
        }
        public void SetShowRomaji(bool show)
        {
            _settings.Data.ShowRomaji = show;
            LrcHost.SetShowRomaji(show);
        }
        public event Action<(LyricsData? lrc, LyricsData? trans, LyricsData? romaji, bool isPureLrc)>? OnLyricLoaded;

        private string? _handlingMusic = null;
        public void LoadFromQid(string id)
        {
            async Task beginInit()
            {
                Dispatcher.Invoke(LrcHost.Reset);
                _handlingMusic = id;
                if (await LyricHelper.GetLyricByQmId(id) is { } dt && _handlingMusic == id)
                {
                    var model = LyricHelper.LoadLrc(dt);
                    if (model.lrc == null) return;
                    Dispatcher.Invoke(() =>
                    {
                        IsTranslationAvailable = model.trans != null;
                        IsRomajiAvailable = model.romaji != null;
                        LrcHost.Load(model.lrc, model.trans, model.romaji, model.isPureLrc);
                        RefreshHostSettings();
                        OnLyricLoaded?.Invoke(model);

                        Dispatcher.Invoke(async () =>
                        {
                            await Task.Delay(100);
                            //fade-out animation after loaded
                            var blurEffect = new BlurEffect() { Radius = 20 };
                            LrcHost.Effect = blurEffect;
                            var aniBlur = new DoubleAnimation(20, 0, TimeSpan.FromMilliseconds(300));
                            var aniOpac = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                            blurEffect.BeginAnimation(BlurEffect.RadiusProperty, aniBlur);
                            LrcHost.BeginAnimation(OpacityProperty, aniOpac);
                        }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                    });
                }
            }

            {
                //a fade-out animation before load lrc.
                var blurEffect = new BlurEffect() { Radius = 0 };
                LrcHost.Effect = blurEffect;
                var aniBlur = new DoubleAnimation(0, 20, TimeSpan.FromMilliseconds(300));
                var aniOpac = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                aniOpac.Completed += delegate { _ = beginInit(); };
                blurEffect.BeginAnimation(BlurEffect.RadiusProperty, aniBlur);
                LrcHost.BeginAnimation(OpacityProperty, aniOpac);
            }
        }
        
    }
}
