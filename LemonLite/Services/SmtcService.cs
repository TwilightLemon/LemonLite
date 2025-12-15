using LemonLite.Configs;
using LemonLite.Utils;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Media.Control;

namespace LemonLite.Services;

/// <summary>
/// 播放时间同步服务
/// 负责与SMTC同步播放时间，并提供平滑的时间更新
/// 
/// 设计说明：
/// 1. SMTC时间线可能有效也可能无效，有效时需要同步精准时间
/// 2. 当SMTC时间线无效时，根据播放状态本地推演时间线
/// 3. SMTC可能高频触发TimelinePropertiesChanged，但精度只到秒，需要平滑处理避免时间跳动
/// </summary>
public class SmtcService(AppSettingService appSettingService) : IHostedService
{
    private SmtcListener _smtcListener = null!;
    private readonly SettingsMgr<AppOption> appOption = appSettingService.GetConfigMgr<AppOption>();
    private readonly DispatcherTimer _playbackTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(100)
    };

    // 时间线同步相关
    private DateTime _lastTickTime;
    private double _smtcSyncPosition = 0;      // 上次SMTC同步的位置
    private DateTime _smtcSyncTime;             // 上次SMTC同步的时间
    private bool _hasValidTimeline = false;
    private bool _isPlaying = false;

    /// <summary>
    /// 同步阈值（秒）：当SMTC位置与本地推演位置偏差超过此值时才进行同步
    /// 设置为1.5秒可以容忍SMTC精度只到秒的情况
    /// </summary>
    private const double SyncThreshold = 1.5;

    /// <summary>
    /// 最小同步间隔（毫秒）：防止SMTC高频触发导致的抖动
    /// </summary>
    private const double MinSyncIntervalMs = 500;

    public SmtcListener SmtcListener => _smtcListener;
    public bool IsSessionValid => _smtcListener.HasValidSession;

    /// <summary>
    /// 当前播放位置（秒)
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
    /// 当播放位置更新时触发
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
        _isPlaying = false;
        _hasValidTimeline = false;
        _smtcSyncPosition = 0;
        _smtcSyncTime = DateTime.MinValue;
        _playbackTimer.Stop();
        PlayingStateChanged?.Invoke(_isPlaying);
        PositionChanged?.Invoke(Position);
        DurationChanged?.Invoke(Duration);
    }

    public void Dispose()
    {
        _playbackTimer.Stop();
        _playbackTimer.Tick -= PlaybackTimer_Tick;
        _smtcListener.MediaPropertiesChanged -= OnSessionChanged;
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

        // 获取并验证时间线
        var timeline = _smtcListener.GetTimeline();
        _hasValidTimeline = timeline != null && timeline.EndTime > timeline.StartTime;

        if (_hasValidTimeline)
        {
            // SMTC提供了有效Timeline，强制同步（状态变化时需要精确同步）
            var duration = timeline!.EndTime;
            UpdateDuration(duration.TotalSeconds);

            var position = timeline.Position.TotalSeconds;
            ForceUpdatePosition(position);
        }

        if (_isPlaying)
        {
            _lastTickTime = DateTime.Now;
            _playbackTimer.Start();
        }
        else
        {
            _playbackTimer.Stop();
        }
    }

    /// <summary>
    /// 从SMTC同步Timeline数据
    /// 采用智能同步策略：只有当偏差超过阈值时才进行同步，避免高频抖动
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
            var now = DateTime.Now;

            // 更新时长
            var duration = timeline.EndTime.TotalSeconds;
            UpdateDuration(duration);

            // 获取SMTC报告的位置
            var smtcPosition = timeline.Position.TotalSeconds;

            // 计算本地推演的位置（基于上次同步点)
            double expectedPosition;
            if (_smtcSyncTime != DateTime.MinValue && _isPlaying)
            {
                var elapsedSinceSync = (now - _smtcSyncTime).TotalSeconds;
                expectedPosition = _smtcSyncPosition + elapsedSinceSync;
            }
            else
            {
                expectedPosition = Position;
            }

            // 计算偏差
            var deviation = Math.Abs(smtcPosition - expectedPosition);

            // 检查是否需要同步
            var timeSinceLastSync = (now - _smtcSyncTime).TotalMilliseconds;
            bool shouldSync = false;

            if (deviation > SyncThreshold)
            {
                // 偏差超过阈值，需要同步（可能是用户拖动进度条或跳转）
                shouldSync = true;
                Debug.WriteLine($"SMTC sync: deviation {deviation:F2}s > threshold, forcing sync to {smtcPosition:F2}s");
            }
            else if (timeSinceLastSync > MinSyncIntervalMs && deviation > 0.1)
            {
                // 超过最小同步间隔且有明显偏差，进行微调同步
                // 但只在偏差方向一致时才同步（避免SMTC精度问题导致的来回跳动）
                if (smtcPosition > expectedPosition)
                {
                    // SMTC位置在本地推演之后，可能是播放速度差异，进行微调
                    shouldSync = true;
                    Debug.WriteLine($"SMTC sync: minor adjustment to {smtcPosition:F2}s (expected {expectedPosition:F2}s)");
                }
                // 如果SMTC位置在本地推演之前，不进行同步（可能是SMTC精度问题）
            }

            if (shouldSync)
            {
                _smtcSyncPosition = smtcPosition;
                _smtcSyncTime = now;
                UpdatePosition(smtcPosition);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SyncTimelineFromSmtc error: {ex.Message}");
        }
    }

    private void PlaybackTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isPlaying) return;

        var now = DateTime.Now;
        var elapsed = (now - _lastTickTime).TotalSeconds;
        _lastTickTime = now;

        // 本地平滑推进时间
        var newPosition = Position + elapsed;

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

    /// <summary>
    /// 强制更新位置（用于状态变化等需要精确同步的场景）
    /// </summary>
    private void ForceUpdatePosition(double positionSeconds)
    {
        _smtcSyncPosition = positionSeconds;
        _smtcSyncTime = DateTime.Now;
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

    /// <summary>
    /// 设置备用时长（当SMTC未提供有效时间线时使用）
    /// </summary>
    /// <param name="durationSeconds">时长（秒）</param>
    public void SetFallbackDuration(double durationSeconds)
    {
        if (!_hasValidTimeline && durationSeconds > 0)
        {
            UpdateDuration(durationSeconds);
        }
    }

    private bool ValidSmtcSessionChecker(string? id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return appOption.Data.SmtcMediaIds.Contains(id.ToLower());
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _smtcListener = SmtcListener.CreateInstance(ValidSmtcSessionChecker).GetAwaiter().GetResult();
        _playbackTimer.Tick += PlaybackTimer_Tick;
        _smtcListener.MediaPropertiesChanged += OnSessionChanged;
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
