using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using System.Threading.Tasks;

namespace LemonLite.Views.Windows;

[ObservableObject]
public partial class MetadataAliasCreatorWindow : FluentWindowBase
{
    private readonly SmtcListener _smtcListener;
    private readonly SettingsMgr<SmtcMetadataAliasConfig> _aliasMgr;

    public MetadataAliasCreatorWindow(SmtcService smtcService, AppSettingService appSettingService)
    {
        _smtcListener = smtcService.SmtcListener;
        _aliasMgr = appSettingService.GetConfigMgr<SmtcMetadataAliasConfig>();
        DataContext = this;
        InitializeComponent();
        Loaded += async (_, _) => await LoadMetadataAsync();
    }

    [ObservableProperty] private string _appId = "";
    [ObservableProperty] private string _currentTitle = "";
    [ObservableProperty] private string _currentArtist = "";
    [ObservableProperty] private string _currentAlbum = "";
    [ObservableProperty] private string _newTitle = "";
    [ObservableProperty] private string _newArtist = "";
    [ObservableProperty] private string _newAlbum = "";
    [ObservableProperty] private string _statusText = "";

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadMetadataAsync();
    }

    private async Task LoadMetadataAsync()
    {
        var appId = _smtcListener.GetAppMediaId();
        if (string.IsNullOrEmpty(appId))
        {
            StatusText = "No active media session.";
            return;
        }

        AppId = appId;
        var info = await _smtcListener.GetNormalizedMediaInfoAsync();
        if (info == null)
        {
            StatusText = "Failed to read metadata.";
            return;
        }

        CurrentTitle = info.Title;
        CurrentArtist = info.Artist;
        CurrentAlbum = info.Album;
        NewTitle = info.Title;
        NewArtist = info.Artist;
        NewAlbum = info.Album;
        StatusText = "";
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrEmpty(AppId))
        {
            StatusText = "No active media session.";
            return;
        }

        if (!_aliasMgr.Data.TryGetValue(AppId, out var aliases))
        {
            aliases = [];
            _aliasMgr.Data[AppId] = aliases;
        }

        int count = 0;

        if (CurrentTitle != NewTitle && !string.IsNullOrEmpty(CurrentTitle))
        {
            var newItem = new SmtcMetadataAliasItem
            {
                AppId = AppId,
                Type = SmtcMetadataAliasType.Name,
                Target = CurrentTitle,
                Name = NewTitle
            };
            newItem.SetConditionWithMetadata(CurrentArtist, CurrentTitle, CurrentAlbum);
            aliases.Add(newItem);
            count++;
        }

        if (CurrentArtist != NewArtist && !string.IsNullOrEmpty(CurrentArtist))
        {
            aliases.Add(new SmtcMetadataAliasItem
            {
                AppId = AppId,
                Type = SmtcMetadataAliasType.Artist,
                Target = CurrentArtist,
                Name = NewArtist
            });
            count++;
        }

        if (CurrentAlbum != NewAlbum && !string.IsNullOrEmpty(CurrentAlbum))
        {
            aliases.Add(new SmtcMetadataAliasItem
            {
                AppId = AppId,
                Type = SmtcMetadataAliasType.Album,
                Target = CurrentAlbum,
                Name = NewAlbum
            });
            count++;
        }

        if (count > 0)
        {
            _aliasMgr.Save();
            StatusText = $"Saved {count} alias(es).";
        }
        else
        {
            StatusText = "No changes to save.";
        }
    }
}
