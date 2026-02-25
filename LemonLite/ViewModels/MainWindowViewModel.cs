using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonLite.Services;
using LemonLite.Utils;
using LemonLite.Views.UserControls;
using LemonLite.Views.Windows;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LemonLite.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SmtcListener _smtcListener;
    private readonly SmtcService _smtcService;
    private readonly LyricService _lyricService;

    public MainWindowViewModel(LyricView lyricView, SmtcService playback, LyricService lyricService)
    {
        _smtcListener = playback.SmtcListener;
        LyricView = lyricView;
        _lyricService = lyricService;

        _smtcService = playback;
        _smtcService.PositionChanged += OnPositionChanged;
        _smtcService.DurationChanged += OnDurationChanged;
        _smtcService.PlayingStateChanged += OnPlayingStateChanged;
        _smtcListener.MediaPropertiesChanged += SmtcListener_MediaPropertiesChanged;
        _smtcListener.SessionExited += SmtcListener_SessionExited;
        _smtcListener.SessionChanged += SmtcListener_SessionChanged;
        _lyricService.MediaMetaDataUpdated += OnMediaMetaDataUpdated;

        _smtcService.CoverUpdated += Smtc_CoverUpdated;
        _smtcService.UpdateCoverFailed += Smtc_UpdateCoverFailed;

        UpdateSmtcInfo();
        InitPlaybackStatus();
        Smtc_CoverUpdated();
    }

    private void SmtcListener_SessionExited(object? sender, EventArgs e)
    {
        App.Current.Dispatcher.Invoke(App.WindowManager.Destroy<MainWindow>);
    }

    private void SmtcListener_SessionChanged(object? sender, EventArgs e)
    {
        // 会话切换时重新加载媒体信息
        UpdateSmtcInfo();
    }

    private void InitPlaybackStatus()
    {
        IsPlaying = _smtcService.IsPlaying;
        OnPositionChanged(_smtcService.Position);
        OnDurationChanged(_smtcService.Duration);
    }

    public void Dispose()
    {
        _smtcService.PositionChanged -= OnPositionChanged;
        _smtcService.DurationChanged -= OnDurationChanged;
        _smtcService.PlayingStateChanged -= OnPlayingStateChanged;
        _smtcListener.MediaPropertiesChanged -= SmtcListener_MediaPropertiesChanged;
        _smtcListener.SessionExited -= SmtcListener_SessionExited;
        _smtcListener.SessionChanged -= SmtcListener_SessionChanged;

        _smtcService.CoverUpdated -= Smtc_CoverUpdated;
        _smtcService.UpdateCoverFailed -= Smtc_UpdateCoverFailed;

        _lyricService.MediaMetaDataUpdated -= OnMediaMetaDataUpdated;
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
            if (_smtcService.BackgroundImageSource != null)
            {
                BackgroundImageSource = _smtcService.BackgroundImageSource;
                IsBackgroundValid= true;
            }
            else
            {
                IsBackgroundValid = false;
            }
        });
    }

    public LyricView LyricView { get; set; }

    [ObservableProperty]
    private string _sourceIdentifier = string.Empty;
    [ObservableProperty]
    private string _title = "Welcome~";

    [ObservableProperty]
    private string _artist = string.Empty;

    [ObservableProperty]
    private string _album = string.Empty;

    [ObservableProperty]
    private Brush? coverImage;
    [ObservableProperty]
    private ImageSource? backgroundImageSource;

    [ObservableProperty]
    private double _currentPlayingPosition = 0;

    [ObservableProperty]
    private double _currentPlayingDuration = 0;

    [ObservableProperty]
    private string _currentPlayingPositionText = "00:00";

    [ObservableProperty]
    private string _currentPlayingDurationText = "00:00";

    [ObservableProperty]
    private bool _isPlaying = false;

    [ObservableProperty]
    private bool _isBackgroundValid = false;

    private void OnPositionChanged(double position)
    {
        CurrentPlayingPosition = position;
        CurrentPlayingPositionText = TimeSpan.FromSeconds(position).ToString(@"mm\:ss");
        // 时间同步由LyricService处理
    }

    private void OnDurationChanged(double duration)
    {
        CurrentPlayingDuration = duration;
        CurrentPlayingDurationText = TimeSpan.FromSeconds(duration).ToString(@"mm\:ss");
    }

    private void OnPlayingStateChanged(bool isPlaying)
    {
        IsPlaying = isPlaying;
    }

    private void SmtcListener_MediaPropertiesChanged(object? sender, EventArgs e)
    {
        UpdateSmtcInfo();
    }

    private void OnMediaMetaDataUpdated(MediaMetaDataUpdatedEventArgs args)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            if (!string.IsNullOrEmpty(args.Title))
                Title = args.Title;
            if (!string.IsNullOrEmpty(args.Artist))
                Artist = args.Artist;
            if (!string.IsNullOrEmpty(args.Album))
                Album = args.Album;
            if (!string.IsNullOrEmpty(args.SourceIdentifier))
                SourceIdentifier = args.SourceIdentifier;
        });
    }

    private async void UpdateSmtcInfo()
    {
        // clean up previous info
        App.Current.Dispatcher.Invoke(() =>
        {
            Title = LemonLite.Services.LocalizationService.Instance["Welcome~"];
            Artist = string.Empty;
            Album = string.Empty;
        });

        if (await _smtcListener.GetNormalizedMediaInfoAsync() is { PlaybackType: Windows.Media.MediaPlaybackType.Music } info)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Title = info.Title ?? LemonLite.Services.LocalizationService.Instance["Welcome~"];
                Artist = info.Artist ?? string.Empty;
                Album = info.Album ?? string.Empty;
            });
        }
    }

    [RelayCommand]
    private async Task PlayPause() => await _smtcListener.PlayOrPause();

    [RelayCommand]
    private async Task PlayNext() => await _smtcListener.Next();

    [RelayCommand]
    private async Task PlayLast() => await _smtcListener.Previous();
}
