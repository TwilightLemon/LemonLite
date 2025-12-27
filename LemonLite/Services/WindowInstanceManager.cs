using H.NotifyIcon.EfficiencyMode;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Windows;

namespace LemonLite.Services;

/// <summary>
/// 通用窗口实例管理器，用于管理单例窗口的创建和销毁
/// </summary>
public class WindowInstanceManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, Window?> _instances = [];
    private readonly object _lock = new();

    public WindowInstanceManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 获取指定类型的窗口实例
    /// </summary>
    public TWindow? GetInstance<TWindow>() where TWindow : Window
    {
        lock (_lock)
        {
            return _instances.TryGetValue(typeof(TWindow), out var window) ? window as TWindow : null;
        }
    }

    /// <summary>
    /// 创建或激活指定类型的窗口
    /// </summary>
    public TWindow CreateOrActivate<TWindow>() where TWindow : Window
    {
        lock (_lock)
        {
            if (_instances.TryGetValue(typeof(TWindow), out var existing) && existing is { IsLoaded: true })
            {
                existing.Activate();
                return (TWindow)existing;
            }

            var window = _serviceProvider.GetRequiredService<TWindow>();
            window.Closed += (_, _) => OnWindowClosed<TWindow>();
            window.Show();
            _instances[typeof(TWindow)] = window;
            return window;
        }
    }

    /// <summary>
    /// 销毁指定类型的窗口
    /// </summary>
    public void Destroy<TWindow>() where TWindow : Window
    {
        lock (_lock)
        {
            if (_instances.TryGetValue(typeof(TWindow), out var window) && window is not null)
            {
                window.Close();
                _instances[typeof(TWindow)] = null;
            }
        }
    }

    /// <summary>
    /// 根据条件创建或销毁窗口
    /// </summary>
    public void SetWindowState<TWindow>(bool shouldExist) where TWindow : Window
    {
        lock (_lock)
        {
            var exists = _instances.TryGetValue(typeof(TWindow), out var window) && window is not null;

            if (shouldExist && !exists)
            {
                CreateOrActivate<TWindow>();
            }
            else if (!shouldExist && exists)
            {
                Destroy<TWindow>();
            }
        }
    }

    private void OnWindowClosed<TWindow>() where TWindow : Window
    {
        lock (_lock)
        {
            _instances[typeof(TWindow)] = null;
        }
    }
}
