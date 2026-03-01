using CommunityToolkit.Mvvm.ComponentModel;
using LemonLite.Services;
using LemonLite.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace LemonLite.ViewModels;

public partial class SettingsWindowViewModel:ObservableObject
{
    public record class SettingsMenuItem(string Title, Geometry Icon, Type PageType);
    public ObservableCollection<SettingsMenuItem> SettingsMenus{ get; set; } = [
        new SettingsMenuItem(LocalizationService.Instance["General"],(Geometry)App.Current.FindResource("Icon_Settings"),typeof(AppSettingsPage)),
        new SettingsMenuItem(LocalizationService.Instance["SmtcApps"],null!,typeof(SmtcAppsPage)),
        new SettingsMenuItem(LocalizationService.Instance["LyricView"],null!,typeof(LyricSettingsPage)),
        new SettingsMenuItem(LocalizationService.Instance["DesktopLyrics"],null!,typeof(DesktopLyricSettingsPage)),
        new SettingsMenuItem(LocalizationService.Instance["AudioVisualizer"],null!,typeof(AudioVisualizerSettingsPage)),
        new SettingsMenuItem(LocalizationService.Instance["About"],null!,typeof(AboutPage))
        ];

    public SettingsWindowViewModel()
    {
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        SettingsMenus.Clear();
        SettingsMenus.Add(new SettingsMenuItem(LocalizationService.Instance["General"],(Geometry)App.Current.FindResource("Icon_Settings"),typeof(AppSettingsPage)));
        SettingsMenus.Add(new SettingsMenuItem(LocalizationService.Instance["SmtcApps"],null!,typeof(SmtcAppsPage)));
        SettingsMenus.Add(new SettingsMenuItem(LocalizationService.Instance["LyricView"],null!,typeof(LyricSettingsPage)));
        SettingsMenus.Add(new SettingsMenuItem(LocalizationService.Instance["DesktopLyrics"],null!,typeof(DesktopLyricSettingsPage)));
        SettingsMenus.Add(new SettingsMenuItem(LocalizationService.Instance["AudioVisualizer"],null!,typeof(AudioVisualizerSettingsPage)));
        SettingsMenus.Add(new SettingsMenuItem(LocalizationService.Instance["About"],null!,typeof(AboutPage)));
    }

    [ObservableProperty]
    public SettingsMenuItem? _selectedMenu;

    [ObservableProperty]
    private object? _currentPageContent;

    partial void OnSelectedMenuChanged(SettingsMenuItem? value)
    {
        if (value == null) return;

        var scope = App.Services.CreateScope();
        var pageContent = scope.ServiceProvider.GetRequiredService(value.PageType);

        CurrentPageContent = pageContent;
    }
}
