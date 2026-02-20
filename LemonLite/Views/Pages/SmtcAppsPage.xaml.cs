using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
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
        DisplayName = sourceId switch
        {
            "qq music" => "QQ Music",
            "netease" or "cloudmusic" => "Netease",
            _ => sourceId
        };
    }

    [RelayCommand]
    private void MoveUp() => _owner.MoveSourceUp(this);

    [RelayCommand]
    private void MoveDown() => _owner.MoveSourceDown(this);
}

public partial class SmtcAppItemViewModel : ObservableObject
{
    internal readonly SmtcAppConfig Config;

    public string AppId => Config.AppId;
    public ObservableCollection<SmtcSourceEntryViewModel> Sources { get; }

    public SmtcAppItemViewModel(SmtcAppConfig config)
    {
        Config = config;
        Sources = new ObservableCollection<SmtcSourceEntryViewModel>(
            config.SearchSources.Select(s => new SmtcSourceEntryViewModel(s, this)));
        Sources.CollectionChanged += SyncToConfig;
    }

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

    private void SyncToConfig(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Config.SearchSources.Clear();
        Config.SearchSources.AddRange(Sources.Select(s => s.SourceId));
    }
}

/// <summary>
/// Interaction logic for SmtcAppsPage.xaml
/// </summary>
[ObservableObject]
public partial class SmtcAppsPage : Page
{
    private readonly SettingsMgr<AppOption> _settings;
    private readonly SmtcService _smtc;

    public ObservableCollection<SmtcAppItemViewModel> SmtcApps { get; } = [];

    public SmtcAppsPage(AppSettingService appSettingService, SmtcService smtcService)
    {
        InitializeComponent();
        DataContext = this;
        Loaded += SmtcAppsPage_Loaded;
        _settings = appSettingService.GetConfigMgr<AppOption>();
        _smtc = smtcService;
    }

    private void SmtcAppsPage_Loaded(object sender, RoutedEventArgs e)
    {
        SmtcApps.Clear();
        foreach (var app in _settings.Data.SmtcApps)
            SmtcApps.Add(new SmtcAppItemViewModel(app));
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
        var config = new SmtcAppConfig { AppId = appId };
        _settings.Data.SmtcApps.Add(config);
        SmtcApps.Add(new SmtcAppItemViewModel(config));
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
