using LemonLite.Configs;
using LemonLite.Utils;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Media.Control;

namespace LemonLite.Services;

/// <summary>
/// 播放时间同步服务
/// 负责与SMTC同步播放时间，并提供平滑的时间更新
/// </summary>
public class SmtcService(AppSettingService appSettingService) : IHostedService
{
    private SmtcListener _smtcListener;
    private readonly SettingsMgr<AppOption> appOption=appSettingService.GetConfigMgr<AppOption>();
    private readonly DispatcherTimer _playbackTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(200)
    };
    private DateTime _lastUpdateTime;
    private bool _hasValidTimeline = false;
    private bool _isPlaying = false;

    public SmtcListener SmtcListener=> _smtcListener;
    public bool IsSessionValid => _smtcListener.HasValidSession;
    /// <summary>
    /// 当前播放位置（秒）
    /// </summary>
    public double Position { get; private set; } = 0;

    /// <summary>
    /// 当前播放时长（秒）
    /// </summary>
    public double Duration { get; private set; } = 0;

    /// <summary>
    /// 是否正在播放
    /// </summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// 当播放位置更新时触发（约200ms一次）
    /// </summary>
    public event Action<double>? PositionChanged;

    /// <summary>
    /// 当播放时长更新时触发
    /// </summary>
    public event Action<double>? DurationChanged;

    /// <summary>
    /// 当播放状态更新时触发
    /// </summary>
    public event Action<bool>? PlayingStateChanged;

    /// <summary>
    /// 重置播放位置和时长
    /// </summary>
    public void Reset()
    {
        Position = 0;
        Duration = 0;
        PositionChanged?.Invoke(Position);
        DurationChanged?.Invoke(Duration);
    }

    public void Dispose()
    {
        _playbackTimer.Stop();
        _playbackTimer.Tick -= PlaybackTimer_Tick;
        _smtcListener.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        _smtcListener.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        _smtcListener.SessionExited -= OnSessionExited;
        _smtcListener.SessionChanged -= OnSessionChanged;
    }

    private void OnPlaybackInfoChanged(object? sender, EventArgs e)
    {
        UpdatePlayingState();
    }

    private void OnTimelinePropertiesChanged(object? sender, EventArgs e)
    {
        SyncTimelineFromSmtc();
    }

    private void OnSessionExited(object? sender, EventArgs e)
    {
        Reset();
        _isPlaying = false;
        PlayingStateChanged?.Invoke(_isPlaying);
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        // 会话切换时重新同步播放状态和时间线
        Reset();
        UpdatePlayingState();
    }

    private void UpdatePlayingState()
    {
        var wasPlaying = _isPlaying;
        _isPlaying = _smtcListener.GetPlaybackStatus() == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

        if (wasPlaying != _isPlaying)
        {
            PlayingStateChanged?.Invoke(_isPlaying);
        }

        var timeline = _smtcListener.GetTimeline();

        // 检查SMTC是否提供有效的Timeline
        _hasValidTimeline = timeline != null && timeline.EndTime > timeline.StartTime;

        if (_hasValidTimeline)
        {
            // SMTC提供了有效Timeline，使用SMTC数据
            var duration = timeline!.EndTime - timeline.StartTime;
            UpdateDuration(duration.TotalSeconds);

            var position = timeline.Position - timeline.StartTime;
            UpdatePosition(position.TotalSeconds);
        }

        if (_isPlaying)
        {
            _lastUpdateTime = DateTime.Now;
            _playbackTimer.Start();
        }
        else
        {
            _playbackTimer.Stop();
        }
    }

    /// <summary>
    /// 从SMTC同步Timeline数据
    /// </summary>
    private void SyncTimelineFromSmtc()
    {
        try
        {
            var timeline = _smtcListener.GetTimeline();
            if (timeline == null || timeline.EndTime <= timeline.StartTime)
            {
                _hasValidTimeline = false;
                return;
            }

            _hasValidTimeline = true;

            // 更新时长
            var duration = timeline.EndTime - timeline.StartTime;
            UpdateDuration(duration.TotalSeconds);

            // 更新位置
            var position = timeline.Position - timeline.StartTime;
            UpdatePosition(position.TotalSeconds);

            // 重置计时器基准
            _lastUpdateTime = DateTime.Now;
        }
        catch { }
    }

    private void PlaybackTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isPlaying) return;

        var now = DateTime.Now;
        var elapsed = now - _lastUpdateTime;
        _lastUpdateTime = now;

        // 无论是否有SMTC Timeline，都使用本地Timer平滑推进
        var newPosition = Position + elapsed.TotalSeconds;

        if (Duration > 0 && newPosition >= Duration)
        {
            newPosition = Duration;
        }

        UpdatePosition(newPosition);
    }

    private void UpdatePosition(double positionSeconds)
    {
        Position = positionSeconds;
        PositionChanged?.Invoke(positionSeconds);
    }

    private void UpdateDuration(double durationSeconds)
    {
        if (Math.Abs(Duration - durationSeconds) > 0.01)
        {
            Duration = durationSeconds;
            DurationChanged?.Invoke(durationSeconds);
        }
    }

    private bool ValidSmtcSessionChecker(string? id)
    {
        if(string.IsNullOrEmpty(id)) return false;
        return appOption.Data.SmtcMediaIds.Contains(id.ToLower());
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _smtcListener = SmtcListener.CreateInstance().GetAwaiter().GetResult();
        _smtcListener.SessionIdFlitter = ValidSmtcSessionChecker;
        _playbackTimer.Tick += PlaybackTimer_Tick;
        _smtcListener.PlaybackInfoChanged += OnPlaybackInfoChanged;
        _smtcListener.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
        _smtcListener.SessionExited += OnSessionExited;
        _smtcListener.SessionChanged += OnSessionChanged;
        UpdatePlayingState();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }
}
