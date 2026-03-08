using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using LemonLite.Views.UserControls;
using LemonLite.Views.Windows;
using Lyricify.Lyrics.Helpers.Types;
using Lyricify.Lyrics.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LemonLite.ViewModels;

/// <summary>
/// [Singleton] ViewModel for DesktopLyricWindow
/// </summary>
public partial class DesktopLyricWindowViewModel : ObservableObject
{
    public DesktopLyricWindowViewModel(
        SmtcService smtcService,
        LyricService lyricService,
        AppSettingService appSettingsService,
        UIResourceService uiResourceService)
    {
        _smtcService = smtcService;
        _lyricService = lyricService;
        _uiResourceService = uiResourceService;
        IsPlaying = _smtcService.IsPlaying;
        _settingsMgr = appSettingsService.GetConfigMgr<DesktopLyricOption>();
        _settingsMgr.OnDataChanged += _settingsMgr_OnDataChanged;

        _smtcService.PlayingStateChanged += _smtcService_PlayingStateChanged;
        _lyricService.CurrentLineEnded += OnCurrentLineChanged;
        _lyricService.TimeUpdated += OnTimeUpdated;
        _lyricService.MediaChanged += OnMediaChanged;
        smtcService.SmtcListener.SessionExited += SmtcListener_SessionExited;

        _smtcService.CoverUpdated += Smtc_CoverUpdated;
        _smtcService.UpdateCoverFailed += Smtc_UpdateCoverFailed;
        _uiResourceService.OnColorModeChanged += OnColorModeChanged;

        ApplySettings();
        LyricControl.Dispatcher.BeginInvoke(()=> UpdateLrc(0));
    }

    private DesktopLyricWindow? _window;
    public void SetWindow(DesktopLyricWindow window) => _window = window;

    private void SmtcListener_SessionExited(object? sender, EventArgs e)
    {
        App.Current.Dispatcher.Invoke(App.WindowManager.Destroy<DesktopLyricWindow>);
    }

    public void Dispose()
    {
        _settingsMgr.OnDataChanged -= _settingsMgr_OnDataChanged;
        _smtcService.PlayingStateChanged -= _smtcService_PlayingStateChanged;
        _smtcService.SmtcListener.SessionExited -= SmtcListener_SessionExited;
        _lyricService.CurrentLineEnded -= OnCurrentLineChanged;
        _lyricService.TimeUpdated -= OnTimeUpdated;
        _lyricService.MediaChanged -= OnMediaChanged;
        _smtcService.CoverUpdated -= Smtc_CoverUpdated;
        _smtcService.UpdateCoverFailed -= Smtc_UpdateCoverFailed;
        _uiResourceService.OnColorModeChanged -= OnColorModeChanged;
        _window = null;
    }

    private void OnColorModeChanged()
    {
        LyricControl.Dispatcher.Invoke(ApplySettings);
    }

    private void OnMediaChanged()
    {
        LyricControl.Dispatcher.Invoke(() =>
        {
            LyricControl.ClearAll();
            _window?.SetHasLyricSource(false);
        });
    }

    private void OnTimeUpdated(int ms)
    {
        if (LyricControl.IsVisible)
        {
            LyricControl.Dispatcher.Invoke(() =>
            {
                LyricControl.UpdateTime(ms);
                var block = LyricControl.MainSyllableLrcs.FirstOrDefault(x => x.Key.StartTime > ms).Value;
                ScrollLrc?.Invoke(block);
            });
        }
    }

    private void OnCurrentLineChanged(LrcLine lrc,int gap)
    {
        LyricControl.Dispatcher.BeginInvoke(()=> UpdateLrc(gap));
    }

    private void _smtcService_PlayingStateChanged(bool isPlaying)
    {
        IsPlaying = isPlaying;
    }

    private void _settingsMgr_OnDataChanged()
    {
        LyricControl.Dispatcher.Invoke(ApplySettings);
    }

    public Action<Action>? HideLineAnimation { get; set; }
    public Action<int>? ShowLineAnimation { get; set; }
    public Action<FrameworkElement>? ScrollLrc { get; set; }

    private void ApplySettings()
    {
#pragma warning disable MVVMTK0034
        if (_lyricControl != null)
        {
            CustomLyricControlStyle();
            Smtc_CoverUpdated();
            ShowTranslation = _settingsMgr.Data.ShowTranslation;
            ShowRomaji = _settingsMgr.Data.ShowRomaji;
            _lyricControl.FontFamily = new FontFamily(_settingsMgr.Data.FontFamily);
            _lyricControl.FontSize = _settingsMgr.Data.LrcFontSize * 0.6;

            foreach (Control block in _lyricControl.MainLrcContainer.Children)
                block.FontSize = _settingsMgr.Data.LrcFontSize;
        }
#pragma warning restore MVVMTK0034
    }

    private static readonly SolidColorBrush NormalLrcColor = new(Color.FromRgb(0xEF, 0xEF, 0xEF));
    private static readonly SolidColorBrush NormalLrcColorLight = new(Color.FromArgb(0x5E, 0x1B, 0x19, 0x34));

    private void CustomLyricControlStyle()
    {
#pragma warning disable MVVMTK0034
        _lyricControl.TranslationLrc.TextAlignment = TextAlignment.Center;
        _lyricControl.MainLrcContainer.HorizontalAlignment = HorizontalAlignment.Center;
        _lyricControl.RomajiLrcContainer.HorizontalAlignment = HorizontalAlignment.Center;
        BackgroundVisibility = _settingsMgr.Data.EnableBackground
            ? Visibility.Visible : Visibility.Collapsed;

        bool isDark = _uiResourceService.GetIsDarkMode();
        if (_settingsMgr.Data.UseHighlightLyricEffect)
        {
            _lyricControl.FontWeight = FontWeights.Bold;
            _lyricControl.SetValue(HighlightTextBlock.UseAdditiveProperty, isDark);
            _lyricControl.Resources.Remove("InActiveLrcForeground");
            _lyricControl.SetResourceReference(LyricLineControl.CustomHighlightColorBrushProperty, "ActiveLrcForegroundColor");
        }
        else
        {
            _lyricControl.FontWeight = FontWeights.Normal;
            _lyricControl.SetValue(HighlightTextBlock.UseAdditiveProperty, false);
            _lyricControl.Resources["InActiveLrcForeground"] = isDark ? NormalLrcColor : NormalLrcColorLight;
            _lyricControl.SetResourceReference(LyricLineControl.CustomHighlightColorBrushProperty, "AccentColorKey");
        }
#pragma warning restore MVVMTK0034
    }

    private readonly SmtcService _smtcService;
    private readonly LyricService _lyricService;
    private readonly UIResourceService _uiResourceService;
    private readonly SettingsMgr<DesktopLyricOption> _settingsMgr;

    [ObservableProperty] private bool _isPlaying = false;
    [ObservableProperty] private bool _showTranslation = true;
    [ObservableProperty] private bool _showRomaji = true;
    [ObservableProperty] private LyricLineControl _lyricControl = new() { IsCurrent = true };
    [ObservableProperty] private Brush? coverImage;
    [ObservableProperty] private ImageSource? backgroundImageSource;
    [ObservableProperty] private Visibility _backgroundVisibility = Visibility.Visible;
    [ObservableProperty] private bool _isBackgroundValid = false;

    partial void OnShowTranslationChanged(bool value)
    {
        _settingsMgr.Data.ShowTranslation = value;
        if (LyricControl != null)
            LyricControl.TranslationLrc.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    partial void OnShowRomajiChanged(bool value)
    {
        _settingsMgr.Data.ShowRomaji = value;
        if (LyricControl != null)
            LyricControl.RomajiLrcContainer.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Smtc_UpdateCoverFailed()
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            CoverImage = Brushes.Azure;
            BackgroundImageSource = null;
            IsBackgroundValid = false;
        });
    }

    private void Smtc_CoverUpdated()
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (_smtcService.CoverImageSource != null)
                CoverImage = new ImageBrush(_smtcService.CoverImageSource);
            if (_settingsMgr.Data.EnableBackground)
            {
                if (_smtcService.BackgroundImageSource != null)
                {
                    BackgroundImageSource = _smtcService.BackgroundImageSource;
                    IsBackgroundValid = true;
                }
                else
                {
                    IsBackgroundValid = false;
                }
            }
            else
            {
                IsBackgroundValid = false;
            }
        });
    }

    private void UpdateLrc(int gap)
    {
        if (_lyricService.CurrentLine == null)
        {
            LyricControl.ClearAll();
            return;
        }

        _window?.SetHasLyricSource(true);

        HideLineAnimation?.Invoke(()=> UpdateLineAfterHideAnimation(gap));
    }

    private void UpdateLineAfterHideAnimation(int gap)
    {
        var lrc = _lyricService.CurrentLine;
        if (lrc == null) return;
        double fontSize = _settingsMgr.Data.LrcFontSize;
        if (lrc.Lrc is LineInfo pure)
            LyricControl.LoadPlainLrc(pure.Text, fontSize);
        else if (lrc.Lrc is SyllableLineInfo line)
            LyricControl.LoadMainLrc(line.Syllables, fontSize);

        if (lrc.Romaji is LineInfo pureRomaji)
            LyricControl.LoadPlainRomaji(pureRomaji.Text);
        else if (lrc.Romaji is SyllableLineInfo romaji && !string.IsNullOrWhiteSpace(romaji.Text))
            LyricControl.LoadRomajiLrc(romaji ?? new SyllableLineInfo([]));
        else
            LyricControl.LoadRomajiLrc(new([]));

        LyricControl.TranslationLrc.Text = lrc.Trans;

        if (_lyricService.IsPureLrc)
            LyricControl.SetActiveState(true);

        // 修复"高一截"bug：
        // 翻译行必须同时满足"开关开启 + 本行有翻译内容"才显示。
        // 若仅凭 ShowTranslation=true 就 Visible，英文歌 lrc.Trans 为空时
        // 空 TextBlock 在 Island SizeToContent=Height 模式下仍占行高，
        // 导致切到中文歌后 Island 偏高一截。
        LyricControl.TranslationLrc.Visibility =
            (ShowTranslation && !string.IsNullOrEmpty(lrc.Trans))
                ? Visibility.Visible : Visibility.Collapsed;

        // Romaji 同理：有实际内容 AND 开关打开才显示
        bool hasRomaji = LyricControl.RomajiLrcContainer.Children.Count > 0;
        LyricControl.RomajiLrcContainer.Visibility =
            (ShowRomaji && hasRomaji)
                ? Visibility.Visible : Visibility.Collapsed;

        ShowLineAnimation?.Invoke(gap);
    }

    [RelayCommand]
    private Task<bool> PlayOrPause() => _smtcService.SmtcListener.PlayOrPause();
    [RelayCommand]
    private Task<bool> PlayNext() => _smtcService.SmtcListener.Next();
    [RelayCommand]
    private Task<bool> PlayLast() => _smtcService.SmtcListener.Previous();

    [RelayCommand]
    private void ShowMainWindow()
    {
        App.Services.GetRequiredService<AppSettingService>()
                             .GetConfigMgr<AppOption>()
                             .Data.StartWithMainWindow = true;
        App.WindowManager.CreateOrActivate<MainWindow>();
    }

    [RelayCommand]
    private void FontSizeUp()
    {
        _settingsMgr.Data.LrcFontSize += 2;
        ApplySettings();
    }

    [RelayCommand]
    private void FontSizeDown()
    {
        _settingsMgr.Data.LrcFontSize -= 2;
        ApplySettings();
    }
}
