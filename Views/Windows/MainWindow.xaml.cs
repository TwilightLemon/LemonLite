using LemonApp.Common.Funcs;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.ViewModels;
using System.Windows;

namespace LemonLite.Views.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel vm;
    private readonly SettingsMgr<Appearance> _mgr;

    public MainWindow(MainWindowViewModel vm,AppSettingService appSettingService)
    {
        InitializeComponent();
        this.DataContext = vm;
        _mgr = appSettingService.GetConfigMgr<Appearance>();
        if(_mgr.Data.WindowSize.Width > 0 && _mgr.Data.WindowSize.Height > 0)
        {
            this.Width = _mgr.Data.WindowSize.Width;
            this.Height = _mgr.Data.WindowSize.Height;
        }
        LyricViewHost.Child = vm.LyricView;
        Closing += delegate {
            vm.Dispose();
            _mgr.Data.WindowSize = new(this.ActualWidth, this.ActualHeight);
        };
        this.vm = vm;
    }

}