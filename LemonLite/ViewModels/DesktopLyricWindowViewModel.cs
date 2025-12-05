using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonLite.Utils;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Views.UserControls;
using Lyricify.Lyrics.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LemonLite.ViewModels;
/// <summary>
/// [Singleton] ViewModel for DesktopLyricWindow
/// </summary>
public partial class DesktopLyricWindowViewModel:ObservableObject
{
    public DesktopLyricWindowViewModel(
        SmtcService smtcService,
        LyricService lyricService,
        AppSettingService appSettingsService)
    {
        _smtcService = smtcService;
        _lyricService = lyricService;
        IsPlaying = _smtcService.IsPlaying;
        _settingsMgr = appSettingsService.GetConfigMgr<DesktopLyricOption>();
        _settingsMgr.OnDataChanged += _settingsMgr_OnDataChanged;
        ShowTranslation = _settingsMgr.Data.ShowTranslation;

        _smtcService.PlayingStateChanged += _smtcService_PlayingStateChanged;
        _lyricService.CurrentLineChanged += OnCurrentLineChanged;
        _lyricService.TimeUpdated += OnTimeUpdated;
        _lyricService.MediaChanged += OnMediaChanged;
        smtcService.SmtcListener.SessionExited += SmtcListener_SessionExited;
        CustomLyricControlStyle();
        LyricControl.Dispatcher.Invoke(() => Update(_lyricService.CurrentLine));
    }

    private void SmtcListener_SessionExited(object? sender, EventArgs e)
    {
        App.Current.Dispatcher.Invoke(App.DestroyDesktopLyricWindow);
    }

    public void Dispose()
    {
        _settingsMgr.OnDataChanged -= _settingsMgr_OnDataChanged;
        _smtcService.PlayingStateChanged -= _smtcService_PlayingStateChanged;
        _smtcService.SmtcListener.SessionExited -= SmtcListener_SessionExited;
        _lyricService.CurrentLineChanged -= OnCurrentLineChanged;
        _lyricService.TimeUpdated -= OnTimeUpdated;
        _lyricService.MediaChanged -= OnMediaChanged;
    }

    private void OnMediaChanged()
    {
        LyricControl.Dispatcher.Invoke(() => LyricControl.ClearAll());
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

    private void OnCurrentLineChanged(LrcLine lrc)
    {
        LyricControl.Dispatcher.Invoke(() => Update(lrc));
    }

    private void _smtcService_PlayingStateChanged(bool isPlaying)
    {
        IsPlaying = isPlaying;
    }

    private void _settingsMgr_OnDataChanged()
    {
        LyricControl.Dispatcher.Invoke(ApplySettings);
    }

    public Action? UpdateAnimation { get;set; }
    public Action<FrameworkElement>? ScrollLrc { get; set; }
    private void ApplySettings()
    {
#pragma warning disable MVVMTK0034 
        if (_lyricControl != null)
        {
            ShowTranslation = _settingsMgr.Data.ShowTranslation;
            _lyricControl.FontFamily = new FontFamily(_settingsMgr.Data.FontFamily);
            _lyricControl.FontSize = _settingsMgr.Data.LrcFontSize*0.6;
            foreach (var block in _lyricControl.MainSyllableLrcs)
            {
                block.Value.FontSize = _settingsMgr.Data.LrcFontSize;
            }
        }
#pragma warning restore MVVMTK0034
    }

    private static readonly SolidColorBrush NormalLrcColor = new SolidColorBrush(Color.FromRgb(0xEF, 0xEF, 0xEF));
    private void CustomLyricControlStyle()
    {
#pragma warning disable MVVMTK0034
        ApplySettings();
        _lyricControl.TranslationLrc.TextAlignment = TextAlignment.Center;
        _lyricControl.MainLrcContainer.HorizontalAlignment = HorizontalAlignment.Center;
        _lyricControl.RomajiLrcContainer.HorizontalAlignment = HorizontalAlignment.Center;

        _lyricControl.CustomNormalColor = NormalLrcColor;
        _lyricControl.TranslationLrc.Foreground = NormalLrcColor;
        _lyricControl.SetResourceReference(LyricLineControl.CustomHighlighterColorProperty, "HighlightThemeColor");
#pragma warning restore MVVMTK0034
    }

    private readonly SmtcService _smtcService;
    private readonly LyricService _lyricService;
    private readonly SettingsMgr<DesktopLyricOption> _settingsMgr;
    [ObservableProperty]
    private bool _isPlaying=false;
    [ObservableProperty]
    private bool _showTranslation = true;
    [ObservableProperty]
    private LyricLineControl _lyricControl = new() { IsCurrent=true};
    partial void OnShowTranslationChanged(bool value)
    {
        _settingsMgr.Data.ShowTranslation = value;
        if (LyricControl != null)
        {
            var visible= value ? Visibility.Visible : Visibility.Collapsed;
            LyricControl.TranslationLrc.Visibility = visible;
            LyricControl.RomajiLrcContainer.Visibility = visible;
        }
    }
    private async void Update(LrcLine? lrc)
    {
        if (lrc == null) return;
        UpdateAnimation?.Invoke();
        double fontSize = _settingsMgr.Data.LrcFontSize;
        await Task.Delay(200);
        
        if (lrc.Lrc is LineInfo pure)
            LyricControl.LoadPlainLrc(pure.Text, fontSize);
        else if (lrc.Lrc is SyllableLineInfo line)
            LyricControl.LoadMainLrc(line.Syllables, fontSize);

        if (lrc.Romaji is LineInfo pureRomaji)
            LyricControl.LoadPlainRomaji(pureRomaji.Text);
        else if (lrc.Romaji is SyllableLineInfo romaji)
            LyricControl.LoadRomajiLrc(romaji ?? new SyllableLineInfo([]));
        else LyricControl.LoadRomajiLrc(new([]));

            LyricControl.TranslationLrc.Text = lrc.Trans;
        if (ShowTranslation)
            LyricControl.TranslationLrc.Visibility = string.IsNullOrEmpty(lrc.Trans) ? Visibility.Collapsed : Visibility.Visible;
        else 
        {
            LyricControl.TranslationLrc.Visibility = Visibility.Collapsed;
            LyricControl.RomajiLrcContainer.Visibility = Visibility.Collapsed;
        }
    }

    [RelayCommand]
    private Task<bool> PlayOrPause() => _smtcService.SmtcListener.PlayOrPause();
    [RelayCommand]
    private Task<bool>  PlayNext()=> _smtcService.SmtcListener.Next();
    [RelayCommand]
    private Task<bool> PlayLast()=>_smtcService.SmtcListener.Previous();

    [RelayCommand]
    private void ShowMainWindow() => App.CreateMainWindow();

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
