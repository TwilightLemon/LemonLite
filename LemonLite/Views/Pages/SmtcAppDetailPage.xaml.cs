using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonLite.Sources;
using System.Collections.Generic;
using System.Windows.Controls;

namespace LemonLite.Views.Pages;

/// <summary>
/// Detail page for a single SMTC app — shows Sources and Metadata Alias configuration.
/// Navigated to from <see cref="SmtcAppsPage"/> via the settings Frame.
/// </summary>
[ObservableObject]
public partial class SmtcAppDetailPage : Page
{
    public SmtcAppItemViewModel App { get; }

    /// <summary>
    /// All sources known to the registry — bound to the "Available Sources" reference section.
    /// </summary>
    public IReadOnlyCollection<ILyricSource> AvailableSources => LyricSourceRegistry.All;

    public SmtcAppDetailPage(SmtcAppItemViewModel app)
    {
        App = app;
        InitializeComponent();
        DataContext = this;
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigationService?.GoBack();
    }
}
