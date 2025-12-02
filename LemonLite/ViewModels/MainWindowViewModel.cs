using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonLite.Services;
using LemonLite.Utils;
using LemonLite.Views.UserControls;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Media.Control;

namespace LemonLite.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SmtcListener _smtcListener;
    private readonly SmtcService _smtc;
    private readonly UIResourceService uiResourceService;

    public MainWindowViewModel(LyricView lyricView, UIResourceService uiResourceService, SmtcService playback)
    {
        _smtcListener = playback.SmtcListener;
        LyricView = lyricView;
        this.uiResourceService = uiResourceService;

        _smtc = playback;
        _smtc.PositionChanged += OnPositionChanged;
        _smtc.DurationChanged += OnDurationChanged;
        _smtc.PlayingStateChanged += OnPlayingStateChanged;
        _smtcListener.MediaPropertiesChanged += SmtcListener_MediaPropertiesChanged;
        _smtcListener.SessionExited += SmtcListener_SessionExited;
        _smtcListener.SessionChanged += SmtcListener_SessionChanged;
        uiResourceService.OnColorModeChanged += UiResourceService_OnColorModeChanged;

        UpdateSmtcInfo();
        InitPlaybackStatus();
    }

    private void SmtcListener_SessionExited(object? sender, EventArgs e)
    {
        App.Current.Dispatcher.Invoke(App.DestroyMainWindow);
    }

    private void SmtcListener_SessionChanged(object? sender, EventArgs e)
    {
        // 会话切换时重新加载媒体信息
        UpdateSmtcInfo();
    }

    private void InitPlaybackStatus()
    {
        IsPlaying = _smtc.IsPlaying;
        OnPositionChanged(_smtc.Position);
        OnDurationChanged(_smtc.Duration);
    }

    private void UiResourceService_OnColorModeChanged()
    {
        UpdateCover();
    }

    public void Dispose()
    {
        _smtc.PositionChanged -= OnPositionChanged;
        _smtc.DurationChanged -= OnDurationChanged;
        _smtc.PlayingStateChanged -= OnPlayingStateChanged;
        _smtcListener.MediaPropertiesChanged -= SmtcListener_MediaPropertiesChanged;
        _smtcListener.SessionExited -= SmtcListener_SessionExited;
        _smtcListener.SessionChanged -= SmtcListener_SessionChanged;
        uiResourceService.OnColorModeChanged -= UiResourceService_OnColorModeChanged;
    }

    public LyricView LyricView { get; set; }

    [ObservableProperty]
    private GlobalSystemMediaTransportControlsSessionMediaProperties? mediaInfo;

    [ObservableProperty]
    private Brush? coverImage;
    [ObservableProperty]
    private BitmapImage? backgroundImageSource;

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

    private async void UpdateSmtcInfo()
    {
        // clean up previous info
        MediaInfo = null;

        if (await _smtcListener.GetMediaInfoAsync() is { PlaybackType: Windows.Media.MediaPlaybackType.Music } info)
        {
            MediaInfo = info;
            UpdateCover();
            // 歌词加载由LyricService处理
        }
    }

    private async void UpdateCover(int retryCount=0)
    {
        if (await _smtcListener.GetMediaInfoAsync() is { PlaybackType: Windows.Media.MediaPlaybackType.Music } info)
        {
            try
            {
                // load cover
                if (info.Thumbnail != null)
                {
                    using var streamRef = await info.Thumbnail.OpenReadAsync();
                    using var stream = streamRef.AsStreamForRead();

                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    var img = new BitmapImage();
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.StreamSource = memoryStream;
                    img.EndInit();
                    if (img.CanFreeze)
                        img.Freeze();
                    App.Current.Dispatcher.Invoke(() => CoverImage = new ImageBrush(img));

                    var bitmap = img.ToBitmap();
                    var isdark = uiResourceService.GetIsDarkMode();
                    var accentColor = bitmap.GetMajorColor().AdjustColor(isdark);
                    var focusColor = accentColor.ApplyColorMode(isdark);
                    UIResourceService.UpdateAccentColor(accentColor, focusColor);

                    bitmap.ApplyMicaEffect(isdark);
                    var background = bitmap.ToBitmapImage();
                    App.Current.Dispatcher.Invoke(() => BackgroundImageSource = background);
                }
            }
            catch {
                if (retryCount > 3)
                    ResetCoverImg();
                else
                {
                    await Task.Delay(retryCount * 200);
                    UpdateCover(retryCount++);
                }
            }
        }
    }

    private void ResetCoverImg()
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            CoverImage = Brushes.Azure;
            BackgroundImageSource = null;
        });
    }

    [RelayCommand]
    private async Task PlayPause() => await _smtcListener.PlayOrPause();

    [RelayCommand]
    private async Task PlayNext() => await _smtcListener.Next();

    [RelayCommand]
    private async Task PlayLast() => await _smtcListener.Previous();
}
