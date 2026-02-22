using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Sources;
using LemonLite.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LemonLite.Views.Pages;

public partial class SmtcSourceEntryViewModel : ObservableObject
{
    private readonly SmtcAppItemViewModel _owner;
    public string SourceId { get; }
    public string DisplayName { get; }

    public SmtcSourceEntryViewModel(string sourceId, SmtcAppItemViewModel owner)
    {
        SourceId = sourceId;
        _owner = owner;
        DisplayName = LyricSourceRegistry.Get(sourceId)?.DisplayName ?? sourceId;
    }

    [RelayCommand]
    private void MoveUp() => _owner.MoveSourceUp(this);

    [RelayCommand]
    private void MoveDown() => _owner.MoveSourceDown(this);

    [RelayCommand]
    private void Remove() => _owner.RemoveSource(this);
}

public partial class SmtcAppItemViewModel : ObservableObject
{
    internal readonly SmtcAppConfig Config;
    private readonly SmtcMetadataAliaConfig _aliaConfig;

    public string AppId => Config.AppId;
    public ObservableCollection<SmtcSourceEntryViewModel> Sources { get; }
    public ObservableCollection<SmtcMetadataAliaItem> Aliases { get; }
    public IReadOnlyList<SmtcMetadataAliaType> AvailableTypes { get; } = Enum.GetValues<SmtcMetadataAliaType>();

    public SmtcAppItemViewModel(SmtcAppConfig config, SmtcMetadataAliaConfig aliaConfig)
    {
        Config = config;
        _aliaConfig = aliaConfig;

        Sources = new ObservableCollection<SmtcSourceEntryViewModel>(
            config.SearchSources.Select(s => new SmtcSourceEntryViewModel(s, this)));
        Sources.CollectionChanged += SyncSourcesToConfig;

        if (!_aliaConfig.TryGetValue(config.AppId, out var aliaList))
            aliaList = [];
        Aliases = new ObservableCollection<SmtcMetadataAliaItem>(aliaList);
        Aliases.CollectionChanged += SyncAliasesToConfig;
    }

    /// <summary>
    /// Sources from the registry that are not yet present in this app's list.
    /// </summary>
    public IEnumerable<ILyricSource> InactiveSources =>
        LyricSourceRegistry.All.Where(s => Sources.All(e => e.SourceId != s.Id));

    internal void MoveSourceUp(SmtcSourceEntryViewModel entry)
    {
        var idx = Sources.IndexOf(entry);
        if (idx > 0)
            Sources.Move(idx, idx - 1);
    }

    internal void MoveSourceDown(SmtcSourceEntryViewModel entry)
    {
        var idx = Sources.IndexOf(entry);
        if (idx >= 0 && idx < Sources.Count - 1)
            Sources.Move(idx, idx + 1);
    }

    internal void RemoveSource(SmtcSourceEntryViewModel entry)
    {
        Sources.Remove(entry);
    }

    [RelayCommand]
    private void AddSource(string sourceId)
    {
        if (Sources.Any(e => e.SourceId == sourceId)) return;
        Sources.Add(new SmtcSourceEntryViewModel(sourceId, this));
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

    private void SyncSourcesToConfig(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Config.SearchSources.Clear();
        Config.SearchSources.AddRange(Sources.Select(s => s.SourceId));
        OnPropertyChanged(nameof(InactiveSources));
    }

    private void SyncAliasesToConfig(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_aliaConfig.TryGetValue(AppId, out var list))
        {
            list = [];
            _aliaConfig[AppId] = list;
        }
        list.Clear();
        list.AddRange(Aliases);
    }
}

/// <summary>
/// Interaction logic for SmtcAppsPage.xaml
/// </summary>
[ObservableObject]
public partial class SmtcAppsPage : Page
{
    private readonly SettingsMgr<AppOption> _settings;
    private readonly SettingsMgr<SmtcMetadataAliaConfig> _aliasSettings;
    private readonly SmtcService _smtc;

    public ObservableCollection<SmtcAppItemViewModel> SmtcApps { get; } = [];

    public SmtcAppsPage(AppSettingService appSettingService, SmtcService smtcService)
    {
        InitializeComponent();
        DataContext = this;
        Loaded += SmtcAppsPage_Loaded;
        _settings = appSettingService.GetConfigMgr<AppOption>();
        _aliasSettings = appSettingService.GetConfigMgr<SmtcMetadataAliaConfig>();
        _smtc = smtcService;
    }

    private void SmtcAppsPage_Loaded(object sender, RoutedEventArgs e)
    {
        SmtcApps.Clear();
        foreach (var app in _settings.Data.SmtcApps)
            SmtcApps.Add(new SmtcAppItemViewModel(app, _aliasSettings.Data));
    }

    [RelayCommand]
    private void OpenApp(SmtcAppItemViewModel app)
    {
        NavigationService?.Navigate(new SmtcAppDetailPage(app));
    }

    [RelayCommand]
    private void AddApp()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Add SMTC App",
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
            FileName = "example.exe",
            CheckFileExists = false,
            CheckPathExists = false,
            OverwritePrompt = false
        };

        if (dialog.ShowDialog() == true)
        {
            var fileName = System.IO.Path.GetFileName(dialog.FileName).ToLower();
            AddApp(fileName);
        }
    }

    [RelayCommand]
    private void AddCurrentApp()
    {
        var cur = _smtc.SmtcListener.SessionManager.GetCurrentSession();
        if (cur == null) return;
        var mediaId = cur.SourceAppUserModelId.ToLower();
        AddApp(mediaId);
    }

    private void AddApp(string appId)
    {
        if (string.IsNullOrWhiteSpace(appId)) return;
        if (_settings.Data.SmtcApps.Any(a => a.AppId == appId)) return;
        // Initialize with registry default order so new apps work out of the box
        var config = new SmtcAppConfig
        {
            AppId = appId,
            SearchSources = LyricSourceRegistry.DefaultSourceIds.ToList()
        };
        _settings.Data.SmtcApps.Add(config);
        SmtcApps.Add(new SmtcAppItemViewModel(config, _aliasSettings.Data));
        _smtc.SmtcListener.RefreshCurrentSession();
    }

    [RelayCommand]
    private void RemoveApp(SmtcAppItemViewModel app)
    {
        SmtcApps.Remove(app);
        _settings.Data.SmtcApps.Remove(app.Config);
        _smtc.SmtcListener.RefreshCurrentSession();
    }
}
