using CommunityToolkit.Mvvm.ComponentModel;
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
        new SettingsMenuItem("General",(Geometry)App.Current.FindResource("Icon_Settings"),typeof(AppSettingsPage)),
        new SettingsMenuItem("Lyric View",null!,typeof(LyricSettingsPage)),
        new SettingsMenuItem("Desktop Lyric",null!,typeof(DesktopLyricSettingsPage)),
        new SettingsMenuItem("Audio Visualizer",null!,typeof(AudioVisualizerSettingsPage)),
        new SettingsMenuItem("About",null!,typeof(AboutPage))
        ];

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
