using LemonLite.Services;
using LemonLite.ViewModels;
using System;
using System.Windows;
using System.Windows.Media;

namespace LemonLite.Views.Windows;

//TODO:  EfficiencyMode管理；个性化独立设置

/// <summary>
/// Interaction logic for EmbeddedWindow.xaml
/// </summary>
public partial class EmbeddedWindow : Window
{
    private const string WindowName = nameof(EmbeddedWindow);
    private readonly MainWindowViewModel vm;
    private readonly UIResourceService ui;
    private readonly EfficiencyModeService efficiencyModeService;

    private void OpenAliasCreator_Click(object sender, RoutedEventArgs e)
    {
        App.WindowManager.CreateOrActivate<MetadataAliasCreatorWindow>();
    }

    private static readonly SolidColorBrush NormalLrcColor_Light = new(Color.FromArgb(0x99, 255, 255, 255));
    private static readonly SolidColorBrush NormalLrcColor_Dark = new(Color.FromArgb(0x99, 0, 0, 0));

    public EmbeddedWindow(MainWindowViewModel vm,
        UIResourceService uiResourceService,
        EfficiencyModeService efficiencyModeService)
    {
        InitializeComponent();
        this.DataContext = vm;
        LyricViewHost.Child = vm.LyricView;
        this.vm = vm;
        this.ui = uiResourceService;
        this.efficiencyModeService = efficiencyModeService;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;

        ui.OnColorModeChanged += Ui_OnColorModeChanged;
    }

    private void Ui_OnColorModeChanged()
    {
        bool isDarkMode = ui.GetIsDarkMode();
        Resources["InActiveLrcForeground"] = isDarkMode ? NormalLrcColor_Light : NormalLrcColor_Dark;
        SongArtistBlock.UseAdditive = SongTitleBlock.UseAdditive = isDarkMode;
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        efficiencyModeService.NotifyWindowClosed(WindowName);
        vm.Dispose();
        ui.OnColorModeChanged -= Ui_OnColorModeChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        efficiencyModeService.NotifyWindowOpened(WindowName);
        var sc = SystemParameters.WorkArea;
        Width = sc.Width * 2 / 3;
        Height = sc.Height * 2 / 3;
        Left = (sc.Right - Width) / 2;
        Top = sc.Height - Height - 80;
        Ui_OnColorModeChanged();
    }

}