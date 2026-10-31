using LemonLite.Configs;
using LemonLite.Utils;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace LemonLite.Services;

public class GlobalHotkeyService : IHostedService
{
    private readonly SmtcService _smtcService;
    private readonly SettingsMgr<HotkeyConfig> _hotkeyConfig;
    private HwndSource? _hwndSource;
    private IntPtr _hwnd;
    private readonly Dictionary<int, HotkeyAction> _registeredHotkeys = [];
    private int _nextHotkeyId = 0xC000;
    private bool _isInitialized;
    private Dispatcher? _hotkeyDispatcher;

    private enum HotkeyAction { PlayPause, PlayNext, PlayPrevious }

    public enum HotkeyActionType { PlayPause, PlayNext, PlayPrevious }

    /// <summary>
    /// 热键动作触发后通知其它组件，参数为动作类型。
    /// </summary>
    public event Action<HotkeyActionType>? HotkeyActionTriggered;

    public bool IsPlayPauseRegistered { get; private set; }
    public bool IsPlayNextRegistered { get; private set; }
    public bool IsPlayPreviousRegistered { get; private set; }

    public string? PlayPauseConflictMessage { get; private set; }
    public string? PlayNextConflictMessage { get; private set; }
    public string? PlayPreviousConflictMessage { get; private set; }

    public event Action? HotkeyRegistrationChanged;

    public GlobalHotkeyService(AppSettingService appSettingService, SmtcService smtcService)
    {
        _smtcService = smtcService;
        _hotkeyConfig = appSettingService.GetConfigMgr<HotkeyConfig>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        //热键线程后台自启动，不阻塞 Host.Start；注册完成由 HotkeyRegistrationChanged 通知
        var thread = new Thread(() =>
        {
            _hotkeyDispatcher = Dispatcher.CurrentDispatcher;

            var parameters = new HwndSourceParameters("LemonLiteHotkeySink")
            {
                Width = 0,
                Height = 0,
                WindowStyle = 0x800000
            };
            _hwndSource = new HwndSource(parameters);
            _hwnd = _hwndSource.Handle;
            _hwndSource.AddHook(WndProc);
            _isInitialized = true;

            RegisterAllHotkeysCore();

            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "LemonLiteHotkeyThread"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized && _hotkeyDispatcher != null && !_hotkeyDispatcher.HasShutdownStarted)
        {
            try
            {
                _hotkeyDispatcher.Invoke(() =>
                {
                    UnregisterAllHotkeysCore();
                    _hwndSource?.RemoveHook(WndProc);
                    _hwndSource?.Dispose();
                });
                _hotkeyDispatcher.InvokeShutdown();
            }
            catch { }
        }
        return Task.CompletedTask;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_registeredHotkeys.TryGetValue(id, out var action))
            {
                try
                {
                    Application.Current?.Dispatcher?.InvokeAsync(() =>
                    {
                        switch (action)
                        {
                            case HotkeyAction.PlayPause:
                                _ = _smtcService.SmtcListener!.PlayOrPause();
                                HotkeyActionTriggered?.Invoke(HotkeyActionType.PlayPause);
                                break;
                            case HotkeyAction.PlayNext:
                                _ = _smtcService.SmtcListener!.Next();
                                HotkeyActionTriggered?.Invoke(HotkeyActionType.PlayNext);
                                break;
                            case HotkeyAction.PlayPrevious:
                                _ = _smtcService.SmtcListener!.Previous();
                                HotkeyActionTriggered?.Invoke(HotkeyActionType.PlayPrevious);
                                break;
                        }
                    });
                }
                catch { }
            }
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void RegisterAllHotkeys()
    {
        if (!_isInitialized || _hotkeyDispatcher == null) return;

        if (_hotkeyDispatcher.CheckAccess())
            RegisterAllHotkeysCore();
        else
            _hotkeyDispatcher.Invoke(RegisterAllHotkeysCore);
    }

    private void RegisterAllHotkeysCore()
    {
        UnregisterAllHotkeysCore();

        var config = _hotkeyConfig.Data;
        if (!config.EnableGlobalHotkeys) return;

        IsPlayPauseRegistered = TryRegisterHotkey(config.PlayPause, HotkeyAction.PlayPause);
        IsPlayNextRegistered = TryRegisterHotkey(config.PlayNext, HotkeyAction.PlayNext);
        IsPlayPreviousRegistered = TryRegisterHotkey(config.PlayPrevious, HotkeyAction.PlayPrevious);

        HotkeyRegistrationChanged?.Invoke();
    }

    private bool TryRegisterHotkey(HotkeyBinding binding, HotkeyAction action)
    {
        if (binding.IsEmpty) return false;

        var id = _nextHotkeyId++;
        var fsModifiers = ToWin32Modifiers(binding.Modifiers);
        var vk = (uint)binding.Key;

        if (RegisterHotKey(_hwnd, id, fsModifiers, vk))
        {
            _registeredHotkeys[id] = action;
            SetConflictMessage(action, null);
            return true;
        }
        else
        {
            var errorCode = Marshal.GetLastWin32Error();
            var conflictMsg = errorCode == ERROR_HOTKEY_ALREADY_REGISTERED
                ? $"Hotkey conflict: {binding.ToDisplayString()} is already registered by another application"
                : $"Failed to register hotkey {binding.ToDisplayString()} (error: {errorCode})";
            SetConflictMessage(action, conflictMsg);
            Debug.WriteLine($"[GlobalHotkeyService] {conflictMsg}");
            return false;
        }
    }

    private void SetConflictMessage(HotkeyAction action, string? message)
    {
        switch (action)
        {
            case HotkeyAction.PlayPause: PlayPauseConflictMessage = message; break;
            case HotkeyAction.PlayNext: PlayNextConflictMessage = message; break;
            case HotkeyAction.PlayPrevious: PlayPreviousConflictMessage = message; break;
        }
    }

    private void UnregisterAllHotkeysCore()
    {
        foreach (var id in _registeredHotkeys.Keys)
        {
            UnregisterHotKey(_hwnd, id);
        }
        _registeredHotkeys.Clear();
        IsPlayPauseRegistered = false;
        IsPlayNextRegistered = false;
        IsPlayPreviousRegistered = false;
        PlayPauseConflictMessage = null;
        PlayNextConflictMessage = null;
        PlayPreviousConflictMessage = null;
    }

    public bool TestHotkeyRegistration(HotkeyBinding binding)
    {
        if (!_isInitialized || binding.IsEmpty) return true;

        var tempId = _nextHotkeyId++;
        var fsModifiers = ToWin32Modifiers(binding.Modifiers);
        var vk = (uint)binding.Key;

        if (RegisterHotKey(_hwnd, tempId, fsModifiers, vk))
        {
            UnregisterHotKey(_hwnd, tempId);
            return true;
        }
        return false;
    }

    private static uint ToWin32Modifiers(HotkeyModifiers modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(HotkeyModifiers.Alt)) result |= MOD_ALT;
        if (modifiers.HasFlag(HotkeyModifiers.Ctrl)) result |= MOD_CONTROL;
        if (modifiers.HasFlag(HotkeyModifiers.Shift)) result |= MOD_SHIFT;
        if (modifiers.HasFlag(HotkeyModifiers.Win)) result |= MOD_WIN;
        return result;
    }

    #region Win32 P/Invoke

    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    #endregion
}
