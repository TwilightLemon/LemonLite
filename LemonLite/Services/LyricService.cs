using LemonLite.Entities;
using LemonLite.Utils;
using Lyricify.Lyrics.Models;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LemonLite.Services;

/// <summary>
/// 媒体元数据更新事件参数
/// </summary>
public record class MediaMetaDataUpdatedEventArgs(
    string? Title,
    string? Artist,
    string? Album,
    int DurationMs,
    string? SourceIdentifier
);

/// <summary>
/// 歌词行数据
/// </summary>
public record class LrcLine(ILineInfo Lrc, string? Trans = null, ILineInfo? Romaji = null);

/// <summary>
/// 歌词管理服务 [Singleton]
/// 负责歌词的获取、加载、时间同步以及当前行事件通知
/// </summary>
public class LyricService
{
    private readonly SmtcService _smtcService;

    public LyricService(SmtcService smtcService)
    {
        _smtcService = smtcService;
        _smtcService.SmtcListener.MediaPropertiesChanged += OnMediaPropertiesChanged;
        _smtcService.SmtcListener.SessionChanged += OnMediaPropertiesChanged;
        _smtcService.PositionChanged += OnPositionChanged;
        _ = LoadLyricFromCurrentMediaAsync();
    }

    #region Events
    /// <summary>
    /// 当歌词加载完成时触发
    /// </summary>
    public event Action<LyricLoadedEventArgs>? LyricLoaded;

    /// <summary>
    /// 当歌词行到达时触发（用于DesktopLyric等）
    /// </summary>
    public event Action<LrcLine>? CurrentLineChanged;

    /// <summary>
    /// 当播放时间更新时触发
    /// </summary>
    public event Action<int>? TimeUpdated;

    /// <summary>
    /// 当媒体变更（需要重置）时触发
    /// </summary>
    public event Action? MediaChanged;

    /// <summary>
    /// 当媒体元数据从搜索结果更新时触发
    /// </summary>
    public event Action<MediaMetaDataUpdatedEventArgs>? MediaMetaDataUpdated;
    #endregion

    #region State
    private string? _currentMusicInfo;
    private string? _smtcSource;
    private MusicMetaData? _currentMusicMetaData;
    private LyricsData? _currentLyric;
    private LyricsData? _currentTrans;
    private LyricsData? _currentRomaji;
    private bool _isPureLrc;
    private LrcLine? _currentLine;
    private ILineInfo? _notifiedLine;
    private CancellationTokenSource? _loadingCts;

    /// <summary>
    /// 当前歌词数据
    /// </summary>
    public LyricsData? CurrentLyric => _currentLyric;

    /// <summary>
    /// 当前翻译数据
    /// </summary>
    public LyricsData? CurrentTrans => _currentTrans;

    /// <summary>
    /// 当前罗马音数据
    /// </summary>
    public LyricsData? CurrentRomaji => _currentRomaji;

    /// <summary>
    /// 是否为纯歌词（无逐词时间）
    /// </summary>
    public bool IsPureLrc => _isPureLrc;

    public LrcLine? CurrentLine=>_currentLine;

    /// <summary>
    /// 当前音乐元数据
    /// </summary>
    public MusicMetaData? CurrentMusicMetaData => _currentMusicMetaData;
    #endregion

    #region Media Change Handling
    private async void OnMediaPropertiesChanged(object? sender, EventArgs e)
    {
        await LoadLyricFromCurrentMediaAsync();
    }

    
    /// <summary>
    /// 从当前SMTC媒体加载歌词
    /// </summary>
    private async Task LoadLyricFromCurrentMediaAsync()
    {
        if (await _smtcService.SmtcListener.GetMediaInfoAsync() is not { PlaybackType: Windows.Media.MediaPlaybackType.Music } info) return;
        //不能没有title
        if(string.IsNullOrEmpty(info.Title)) return;

        var newInfo=info.Title + info.Artist;
        if (_currentMusicInfo == newInfo) return;
        _currentMusicInfo = newInfo;

        Reset();
        MediaChanged?.Invoke();
        Debug.WriteLine("New session!!");

        _loadingCts?.Cancel();
        _loadingCts?.Dispose();
        _loadingCts = new CancellationTokenSource();
        var cancellationToken = _loadingCts.Token;
        int durationMs=(int)_smtcService.Duration * 1000;
        try
        {
            if (await LyricHelper.SearchMusicAsync(info.Title, info.Artist, info.AlbumTitle, durationMs, cancellationToken) is { Id: not null } musicMetaData)
            {
                if (cancellationToken.IsCancellationRequested) return;

                _currentMusicMetaData = musicMetaData;

                // 更新备用时长（当SMTC未提供有效时间线时）
                if (musicMetaData.DurationMs > 0)
                {
                    _smtcService.SetFallbackDuration(musicMetaData.DurationMs / 1000.0);
                }

                // 通知媒体元数据更新
                MediaMetaDataUpdated?.Invoke(new MediaMetaDataUpdatedEventArgs(
                    musicMetaData.Title,
                    musicMetaData.ArtistString,
                    musicMetaData.Album,
                    musicMetaData.DurationMs,
                    musicMetaData.Searcher?.DisplayName ?? "Unknown Source"
                ));

                var mediaId = _smtcService.SmtcListener.GetAppMediaId().ToLower();
                _smtcSource = _smtcSource?.EndsWith(".exe") is true ? mediaId[..^4] : mediaId;

                var source = musicMetaData.Searcher?.Name?.ToLower();
                await LoadLyricByIdAsync(musicMetaData.Id,source, cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// 通过ID加载歌词
    /// </summary>
    private async Task LoadLyricByIdAsync(string id,string source, CancellationToken cancellationToken)
    {
        try
        {
            if (await LyricHelper.GetLyricById(id,source, cancellationToken) is { } dt)
            {
                if (cancellationToken.IsCancellationRequested ) return;

                var model = LyricHelper.LoadLrc(dt);
                if (model.lrc == null) return;

                if (cancellationToken.IsCancellationRequested) return;

                _currentLyric = model.lrc;
                _currentTrans = model.trans;
                _currentRomaji = model.romaji;
                _isPureLrc = model.isPureLrc;

                LyricLoaded?.Invoke(new LyricLoadedEventArgs(
                    model.lrc,
                    model.trans,
                    model.romaji,
                    model.isPureLrc
                ));
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// 重置歌词状态
    /// </summary>
    public void Reset()
    {
        _smtcSource = null;
        _currentMusicMetaData = null;
        _currentLyric = null;
        _currentTrans = null;
        _currentRomaji = null;
        _isPureLrc = false;
        _currentLine = null;
        _notifiedLine = null;
    }
    #endregion

    #region Time Sync
    private void OnPositionChanged(double seconds)
    {
        int ms = (int)(seconds * 1000);
        TimeUpdated?.Invoke(ms);
        UpdateCurrentLine(ms);
    }

    /// <summary>
    /// 更新当前歌词行
    /// </summary>
    private void UpdateCurrentLine(int ms)
    {
        if (_currentLyric?.Lines == null) return;

        ILineInfo? target = null;
        ILineInfo? lastItem = null;

        if (!_isPureLrc)
        {
            foreach (var line in _currentLyric.Lines)
            {
                if ((lastItem?.EndTime ?? line.StartTime) <= ms && line.EndTime >= ms)
                {
                    target = line;
                    break;
                }
                lastItem = line;
            }
        }
        else
        {
            // 纯歌词模式：找到最后一个开始时间小于等于当前时间的行
            foreach (var line in _currentLyric.Lines)
            {
                if (line.StartTime <= ms)
                    target = line;
                else
                    break;
            }
        }

        if (target != null && target != _notifiedLine)
        {
            _notifiedLine = target;

            // 获取对应的翻译和罗马音
            string? trans = null;
            ILineInfo? romaji = null;

            if (_currentTrans?.Lines != null)
            {
                foreach (var line in _currentTrans.Lines)
                {
                    if (line.StartTime >= target.StartTime - 10)
                    {
                        if (line.Text != "//")
                            trans = line.Text;
                        break;
                    }
                }
            }

            if (_currentRomaji?.Lines != null)
            {
                foreach (var line in _currentRomaji.Lines)
                {
                    if (line.StartTime >= target.StartTime - 10)
                    {
                        romaji = line;
                        break;
                    }
                }
            }
            _currentLine = new LrcLine(target, trans, romaji);
            CurrentLineChanged?.Invoke(_currentLine);
        }
    }
    #endregion

    public void Dispose()
    {
        _loadingCts?.Cancel();
        _loadingCts?.Dispose();
        _smtcService.SmtcListener.MediaPropertiesChanged -= OnMediaPropertiesChanged;
        _smtcService.PositionChanged -= OnPositionChanged;
    }
}

/// <summary>
/// 歌词加载完成事件参数
/// </summary>
public record class LyricLoadedEventArgs(
    LyricsData Lyric,
    LyricsData? Trans,
    LyricsData? Romaji,
    bool IsPureLrc
);
