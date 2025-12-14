using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace LemonLite.Utils;

public class SmtcListener(GlobalSystemMediaTransportControlsSessionManager mgr)
{
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private readonly object _sessionLock = new();
    private string? _currentSessionId;
    
    // 跟踪所有受支持的session
    private readonly ConcurrentDictionary<string, GlobalSystemMediaTransportControlsSession> _trackedSessions = new();
    
    public GlobalSystemMediaTransportControlsSessionManager SessionManager { get; } = mgr;

    public static async Task<SmtcListener> CreateInstance(Func<string?, bool> sessionIdFilter)
    {
        var gsmtcsm = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        var smtcHelper = new SmtcListener(gsmtcsm) { SessionIdFilter = sessionIdFilter };
        
        // 订阅会话管理器的会话列表变化事件（监听所有session的增减）
        gsmtcsm.SessionsChanged += (s, e) =>
        {
            smtcHelper.OnSessionsChanged();
        };
        
        // 订阅当前会话变化事件（用于响应系统切换当前session）
        gsmtcsm.CurrentSessionChanged += (s, e) =>
        {
            smtcHelper.OnCurrentSessionChanged();
        };
        
        // 初始化
        smtcHelper.OnSessionsChanged();
        
        return smtcHelper;
    }
    
    public bool HasValidSession => _currentSession != null && _currentSessionId != null;
    
    /// <summary>
    /// 当媒体信息发生变化时触发 例如Title ,Artist,Album等信息变更
    /// </summary>
    public event EventHandler? MediaPropertiesChanged;
    
    /// <summary>
    /// 当媒体播放状态发生变化时触发 例如播放，暂停，停止等状态变更
    /// </summary>
    public event EventHandler? PlaybackInfoChanged;
    
    /// <summary>
    /// 当所有受支持的媒体会话都退出时触发
    /// </summary>
    public event EventHandler? SessionExited;
    
    /// <summary>
    /// 当切换到新的媒体会话时触发（包括从无会话到有会话，或从一个应用切换到另一个应用）
    /// </summary>
    public event EventHandler? SessionChanged;
    
    public event EventHandler? TimelinePropertiesChanged;

    public Func<string?, bool> SessionIdFilter { get; set; } = (_) => true;
    
    public void RefreshCurrentSession() => OnSessionsChanged();

    /// <summary>
    /// 当session列表发生变化时调用（session增加或减少）
    /// </summary>
    private void OnSessionsChanged()
    {
        List<GlobalSystemMediaTransportControlsSession> currentSessions;
        try
        {
            currentSessions = SessionManager.GetSessions().ToList();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            currentSessions = [];
        }

        bool sessionChanged = false;
        bool shouldFireSessionExited = false;
        
        lock (_sessionLock)
        {
            // 获取当前所有session的ID
            var currentSessionIds = new HashSet<string>();
            foreach (var session in currentSessions)
            {
                try
                {
                    var id = session.SourceAppUserModelId;
                    if (!string.IsNullOrEmpty(id))
                    {
                        currentSessionIds.Add(id);
                    }
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // 忽略无效session
                }
            }

            // 找出已退出的session（在跟踪列表中但不在当前列表中）
            var exitedSessionIds = _trackedSessions.Keys.Where(id => !currentSessionIds.Contains(id)).ToList();
            
            // 取消订阅并移除已退出的session
            foreach (var exitedId in exitedSessionIds)
            {
                if (_trackedSessions.TryGetValue(exitedId, out var exitedSession))
                {
                    UnsubscribeSessionEvents(exitedSession);
                    _trackedSessions.Remove(exitedId, out _);

                    // 如果退出的是当前选定的session
                    if (_currentSessionId == exitedId)
                    {
                        _currentSession = null;
                        _currentSessionId = null;
                        sessionChanged = true;
                    }
                }
            }

            // 找出新增的受支持session
            foreach (var session in currentSessions)
            {
                try
                {
                    var id = session.SourceAppUserModelId;
                    if (!string.IsNullOrEmpty(id) && SessionIdFilter(id) && !_trackedSessions.ContainsKey(id))
                    {
                        // 新增受支持的session，开始跟踪
                        _trackedSessions[id] = session;
                        SubscribeSessionEvents(session);
                    }
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // 忽略无效session
                }
            }

            // 如果当前没有选定的session，尝试选择一个受支持的session
            if (_currentSession == null && _trackedSessions.Count > 0)
            {
                // 优先选择系统当前session（如果它是受支持的）
                GlobalSystemMediaTransportControlsSession? preferredSession = null;
                try
                {
                    var systemCurrentSession = SessionManager.GetCurrentSession();
                    var systemCurrentId = systemCurrentSession?.SourceAppUserModelId;
                    if (systemCurrentId != null && _trackedSessions.ContainsKey(systemCurrentId))
                    {
                        preferredSession = _trackedSessions[systemCurrentId];
                    }
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // 忽略
                }

                // 如果系统当前session不是受支持的，选择第一个受支持的session
                preferredSession ??= _trackedSessions.Values.FirstOrDefault();

                if (preferredSession != null)
                {
                    try
                    {
                        _currentSession = preferredSession;
                        _currentSessionId = preferredSession.SourceAppUserModelId;
                        sessionChanged = true;
                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {
                        // 忽略
                    }
                }
            }

            // 检查是否所有受支持的session都已退出
            if (_trackedSessions.IsEmpty && sessionChanged)
            {
                shouldFireSessionExited = true;
            }
        }

        // 在锁外触发事件
        if (sessionChanged)
        {
            if (shouldFireSessionExited)
            {
                SessionExited?.Invoke(this, EventArgs.Empty);
            }
            else if (_currentSession != null)
            {
                SessionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 当系统当前session变化时调用（用于切换到新的受支持session）
    /// </summary>
    private void OnCurrentSessionChanged()
    {
        GlobalSystemMediaTransportControlsSession? newSystemSession = null;
        string? newSystemSessionId = null;
        
        try
        {
            newSystemSession = SessionManager.GetCurrentSession();
            newSystemSessionId = newSystemSession?.SourceAppUserModelId;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return;
        }

        // 如果新的系统当前session是受支持的，切换到它
        if (newSystemSessionId != null && SessionIdFilter(newSystemSessionId))
        {
            bool sessionChanged = false;
            
            lock (_sessionLock)
            {
                if (_currentSessionId != newSystemSessionId)
                {
                    // 确保这个session在跟踪列表中
                    if (!_trackedSessions.ContainsKey(newSystemSessionId) && newSystemSession != null)
                    {
                        _trackedSessions[newSystemSessionId] = newSystemSession;
                        SubscribeSessionEvents(newSystemSession);
                    }
                    
                    if (_trackedSessions.TryGetValue(newSystemSessionId, out var trackedSession))
                    {
                        _currentSession = trackedSession;
                        _currentSessionId = newSystemSessionId;
                        sessionChanged = true;
                    }
                }
            }

            if (sessionChanged)
            {
                SessionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void SubscribeSessionEvents(GlobalSystemMediaTransportControlsSession session)
    {
        session.MediaPropertiesChanged += OnSessionMediaPropertiesChanged;
        session.PlaybackInfoChanged += OnSessionPlaybackInfoChanged;
        session.TimelinePropertiesChanged += OnSessionTimelinePropertiesChanged;
    }

    private void UnsubscribeSessionEvents(GlobalSystemMediaTransportControlsSession session)
    {
        session.MediaPropertiesChanged -= OnSessionMediaPropertiesChanged;
        session.PlaybackInfoChanged -= OnSessionPlaybackInfoChanged;
        session.TimelinePropertiesChanged -= OnSessionTimelinePropertiesChanged;
    }

    private void OnSessionMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        // 只有当前选定的session的事件才会触发外部事件
        lock (_sessionLock)
        {
            if (_currentSession != sender) return;
        }
        MediaPropertiesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSessionPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        lock (_sessionLock)
        {
            if (_currentSession != sender) return;
        }
        PlaybackInfoChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSessionTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        lock (_sessionLock)
        {
            if (_currentSession != sender) return;
        }
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
            return _currentSession;
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
