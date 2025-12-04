using System;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace LemonLite.Utils;
public class SmtcListener
{
    private GlobalSystemMediaTransportControlsSession? _globalSMTCSession;
    private readonly object _sessionLock = new();
    private string? _currentSessionId;

    public static async Task<SmtcListener> CreateInstance(Func<string?, bool> sessionIdFlitter)
    {
        var gsmtcsm = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        var smtcHelper = new SmtcListener() { SessionIdFlitter = sessionIdFlitter };
        
        // 订阅会话管理器的会话变化事件
        gsmtcsm.CurrentSessionChanged += (s, e) =>
        {
            smtcHelper.OnCurrentSessionChanged(gsmtcsm);
        };
        
        // 初始化当前会话
        smtcHelper.OnCurrentSessionChanged(gsmtcsm);
        
        return smtcHelper;
    }
    public bool HasValidSession => _globalSMTCSession != null && _currentSessionId != null;
    /// <summary>
    /// 当媒体信息发生变化时触发 例如Title ,Artist,Album等信息变更
    /// </summary>
    public event EventHandler? MediaPropertiesChanged;
    
    /// <summary>
    /// 当媒体播放状态发生变化时触发 例如播放，暂停，停止等状态变更
    /// </summary>
    public event EventHandler? PlaybackInfoChanged;
    
    /// <summary>
    /// 当所有媒体会话都退出时触发（没有任何活动会话）
    /// </summary>
    public event EventHandler? SessionExited;
    
    /// <summary>
    /// 当切换到新的媒体会话时触发（包括从无会话到有会话，或从一个应用切换到另一个应用）
    /// </summary>
    public event EventHandler? SessionChanged;
    
    public event EventHandler? TimelinePropertiesChanged;

    public Func<string?,bool> SessionIdFlitter { get; set; } = (_) => true;

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager mgr)
    {
        GlobalSystemMediaTransportControlsSession? newSession = null;
        GlobalSystemMediaTransportControlsSession? oldSession = null;
        string? newSessionId = null;
        string? oldSessionId = null;
        bool sessionChanged = false;

        try
        {
            newSession = mgr.GetCurrentSession();
            newSessionId = newSession?.SourceAppUserModelId;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // 获取会话失败，视为无会话
            newSession = null;
            newSessionId = null;
        }

        lock (_sessionLock)
        {
            oldSession = _globalSMTCSession;
            oldSessionId = _currentSessionId;
            
            sessionChanged = oldSessionId != newSessionId;
            
            if (sessionChanged&&SessionIdFlitter(newSessionId))
            {
                // 取消旧会话的事件订阅
                if (oldSession != null)
                {
                    UnsubscribeSessionEvents(oldSession);
                }
                
                // 更新会话引用
                _globalSMTCSession = newSession;
                _currentSessionId = newSessionId;
                
                // 订阅新会话的事件
                if (newSession != null)
                {
                    SubscribeSessionEvents(newSession);
                }
            }
            if (newSession == null&&SessionIdFlitter(oldSessionId))
            {
                if (oldSession != null)
                {
                    UnsubscribeSessionEvents(oldSession);
                }
                _globalSMTCSession = null;
                _currentSessionId = null;
            }
        }

        // 在锁外触发事件，避免死锁
        if (sessionChanged)
        {
            if (newSession == null)
            {
                // 所有会话都退出了
                SessionExited?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                // 切换到新会话（包括从无到有，或从一个应用切换到另一个）
                SessionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void SubscribeSessionEvents(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            session.MediaPropertiesChanged += OnSessionMediaPropertiesChanged;
            session.PlaybackInfoChanged += OnSessionPlaybackInfoChanged;
            session.TimelinePropertiesChanged += OnSessionTimelinePropertiesChanged;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // 订阅失败，忽略
        }
    }

    private void UnsubscribeSessionEvents(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            session.MediaPropertiesChanged -= OnSessionMediaPropertiesChanged;
            session.PlaybackInfoChanged -= OnSessionPlaybackInfoChanged;
            session.TimelinePropertiesChanged -= OnSessionTimelinePropertiesChanged;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // 取消订阅失败，忽略
        }
    }

    private void OnSessionMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        MediaPropertiesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSessionPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSessionTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        TimelinePropertiesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 线程安全地获取当前会话的快照
    /// 注意：返回的会话对象可能在使用时已失效，调用方需要捕获 COMException
    /// </summary>
    private GlobalSystemMediaTransportControlsSession? GetSessionSnapshot()
    {
        lock (_sessionLock)
        {
            return _globalSMTCSession;
        }
    }

    public async Task<GlobalSystemMediaTransportControlsSessionMediaProperties?> GetMediaInfoAsync()
    {
        var session = GetSessionSnapshot();
        if (session == null) return null;
        
        try
        {
            return await session.TryGetMediaPropertiesAsync();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Session was invalidated between the lock and the call
            return null;
        }
    }

    public GlobalSystemMediaTransportControlsSessionPlaybackStatus? GetPlaybackStatus()
    {
        var session = GetSessionSnapshot();
        if (session == null) return null;
        
        try
        {
            return session.GetPlaybackInfo().PlaybackStatus;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    public GlobalSystemMediaTransportControlsSessionTimelineProperties? GetTimeline()
    {
        var session = GetSessionSnapshot();
        if (session == null) return null;
        
        try
        {
            return session.GetTimelineProperties();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }

    public string GetAppMediaId()
    {
        var session = GetSessionSnapshot();
        if (session == null) return string.Empty;
        
        try
        {
            return session.SourceAppUserModelId;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return string.Empty;
        }
    }

    public async Task<bool> PlayOrPause()
    {
        var session = GetSessionSnapshot();
        if (session == null) return false;
        
        try
        {
            if (session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
            {
                await session.TryPauseAsync();
                return false;
            }
            else
            {
                await session.TryPlayAsync();
                return true;
            }
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return false;
        }
    }
    public async Task<bool> Previous()
    {
        var session = GetSessionSnapshot();
        if (session == null) return false;
        
        try
        {
            return await session.TrySkipPreviousAsync();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return false;
        }
    }
    public async Task<bool> Next()
    {
        var session = GetSessionSnapshot();
        if (session == null) return false;
        
        try
        {
            return await session.TrySkipNextAsync();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return false;
        }
    }

    public async Task<bool> SetPosition(TimeSpan position)
    {
        var session = GetSessionSnapshot();
        if (session == null) return false;
        
        try
        {
            return await session.TryChangePlaybackPositionAsync(position.Ticks);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return false;
        }
    }

}
