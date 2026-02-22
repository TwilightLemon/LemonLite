using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LemonLite.Views.Pages;

public partial class SmtcMetadataAliaAppViewModel : ObservableObject
{
    private readonly SmtcMetadataAliaConfig _config;

    public string AppId { get; }
    public ObservableCollection<SmtcMetadataAliaItem> Aliases { get; }
    public IReadOnlyList<SmtcMetadataAliaType> AvailableTypes { get; } = Enum.GetValues<SmtcMetadataAliaType>();

    public SmtcMetadataAliaAppViewModel(string appId, SmtcMetadataAliaConfig config)
    {
        AppId = appId;
        _config = config;
        if (!_config.TryGetValue(appId, out var list))
        {
            list = [];
            _config[appId] = list;
        }

        Aliases = new ObservableCollection<SmtcMetadataAliaItem>(list);
        Aliases.CollectionChanged += SyncToConfig;
    }

    [RelayCommand]
    private void AddAlias()
    {
        Aliases.Add(new SmtcMetadataAliaItem
        {
            AppId = AppId,
            Type = SmtcMetadataAliaType.Name,
            Target = string.Empty,
            Name = string.Empty
        });
    }

    [RelayCommand]
    private void RemoveAlias(SmtcMetadataAliaItem item)
    {
        if (item == null) return;
        Aliases.Remove(item);
    }

    private void SyncToConfig(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_config.TryGetValue(AppId, out var list))
        {
            list = [];
            _config[AppId] = list;
        }

        list.Clear();
        list.AddRange(Aliases);
    }
}

/// <summary>
/// Interaction logic for SmtcMetadataAliaPage.xaml
/// </summary>
[ObservableObject]
public partial class SmtcMetadataAliaPage : Page
{
    private readonly SettingsMgr<AppOption> _appSettings;
    private readonly SettingsMgr<SmtcMetadataAliaConfig> _aliasSettings;

    public ObservableCollection<SmtcMetadataAliaAppViewModel> Apps { get; } = [];

    public SmtcMetadataAliaPage(AppSettingService appSettingService)
    {
        InitializeComponent();
        DataContext = this;
        Loaded += SmtcMetadataAliaPage_Loaded;
        _appSettings = appSettingService.GetConfigMgr<AppOption>();
        _aliasSettings = appSettingService.GetConfigMgr<SmtcMetadataAliaConfig>();
    }

    private void SmtcMetadataAliaPage_Loaded(object sender, RoutedEventArgs e)
    {
        Apps.Clear();
        foreach (var app in _appSettings.Data.SmtcApps)
        {
            var appId = app.AppId.ToLower();
            Apps.Add(new SmtcMetadataAliaAppViewModel(appId, _aliasSettings.Data));
        }
    }
}
