using LemonLite.Entities;
using LemonLite.Utils;
using Lyricify.Lyrics.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LemonLite.Services;

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
    #endregion

    #region State
    private string? _currentMusicId;
    private string? _smtcSource;
    private LyricsData? _currentLyric;
    private LyricsData? _currentTrans;
    private LyricsData? _currentRomaji;
    private bool _isPureLrc;
    private LrcLine? _currentLine;
    private ILineInfo? _notifiedLine;
    private CancellationTokenSource? _loadingCts;
    private int _loadingSessionId;

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
    /// 是否为纯歌词（无时间轴）
    /// </summary>
    public bool IsPureLrc => _isPureLrc;

    public LrcLine? CurrentLine=>_currentLine;

    /// <summary>
    /// 当前音乐ID
    /// </summary>
    public string? CurrentMusicId => _currentMusicId;
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
        _loadingCts?.Cancel();
        _loadingCts?.Dispose();
        _loadingCts = new CancellationTokenSource();
        var currentSessionId = ++_loadingSessionId;
        var cancellationToken = _loadingCts.Token;

        try
        {
            if (await _smtcService.SmtcListener.GetMediaInfoAsync() is { PlaybackType: Windows.Media.MediaPlaybackType.Music } info)
            {
                if (cancellationToken.IsCancellationRequested || currentSessionId != _loadingSessionId) return;

                if (await LyricHelper.SearchQid(info.Title, info.Artist, cancellationToken) is { } id)
                {
                    if (cancellationToken.IsCancellationRequested || currentSessionId != _loadingSessionId) return;
                    
                    // 如果是同一首歌，不需要重新加载
                    if (_currentMusicId == id) return;

                    // 不同的歌曲，重置状态并加载
                    var mediaId = _smtcService.SmtcListener.GetAppMediaId().ToLower();
                    _smtcSource = mediaId[..^4];
                    
                    ResetPlaybackState();
                    MediaChanged?.Invoke();
                    
                    await LoadLyricByIdAsync(id, currentSessionId, cancellationToken);
                }
                else
                {
                    // 搜索不到歌曲ID，重置状态
                    Reset();
                    MediaChanged?.Invoke();
                }
            }
            else
            {
                // 没有音乐播放，重置状态
                Reset();
                MediaChanged?.Invoke();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// 通过ID加载歌词
    /// </summary>
    private async Task LoadLyricByIdAsync(string id, int sessionId, CancellationToken cancellationToken)
    {
        _currentMusicId = id;

        try
        {
            if (await LyricHelper.GetLyricByQmId(id, cancellationToken) is { } dt)
            {
                if (cancellationToken.IsCancellationRequested || sessionId != _loadingSessionId) return;

                var model = LyricHelper.LoadLrc(dt);
                if (model.lrc == null) return;

                if (cancellationToken.IsCancellationRequested || sessionId != _loadingSessionId) return;

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
    /// 重置播放状态（保留音乐ID用于判断）
    /// </summary>
    private void ResetPlaybackState()
    {
        _currentLine = null;
        _notifiedLine = null;
    }

    /// <summary>
    /// 完全重置歌词状态
    /// </summary>
    public void Reset()
    {
        _currentMusicId = null;
        _smtcSource = null;
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
