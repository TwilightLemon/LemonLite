﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using FluentWpfCore.Interop;
using LemonLite.Configs;
using LemonLite.Services;
using LemonLite.Utils;
using LemonLite.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace LemonLite.Views.Windows;

//TODO:  EfficiencyMode管理；个性化独立设置

/// <summary>
/// Interaction logic for EmbeddedWindow.xaml
/// </summary>
public partial class EmbeddedWindow : Window
{
    private const string WindowName = nameof(EmbeddedWindow);
    private readonly MainWindowViewModel vm;
    private readonly SmtcService smtcService;
    private readonly UIResourceService ui;
    private readonly EfficiencyModeService efficiencyModeService;
    private readonly SettingsMgr<Appearance> _mgr;

    private void OpenAliasCreator_Click(object sender, RoutedEventArgs e)
    {
        App.WindowManager.CreateOrActivate<MetadataAliasCreatorWindow>();
    }

    public EmbeddedWindow(MainWindowViewModel vm,
        AppSettingService appSettingService,
        SmtcService smtcService, 
        UIResourceService uiResourceService,
        EfficiencyModeService efficiencyModeService)
    {
        InitializeComponent();
        this.DataContext = vm;
        _mgr = appSettingService.GetConfigMgr<Appearance>();
        _mgr.OnDataChanged += Appearance_OnDataChanged;
        LyricViewHost.Child = vm.LyricView;
        this.vm = vm;
        this.smtcService = smtcService;
        this.ui = uiResourceService;
        this.efficiencyModeService = efficiencyModeService;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;

        ui.OnColorModeChanged += Ui_OnColorModeChanged;

        this.MouseEnter += MainWindow_MouseEnter;
        this.MouseLeave += MainWindow_MouseLeave;

        ApplySettings(isFirstCall: true);
    }

    private void Ui_OnColorModeChanged()
    {
        SongArtistBlock.UseAdditive = SongTitleBlock.UseAdditive = ui.GetIsDarkMode();
    }

    private void Appearance_OnDataChanged()
    {
        Dispatcher.BeginInvoke(() => ApplySettings());
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        efficiencyModeService.NotifyWindowClosed(WindowName);
        vm.Dispose();
        _mgr.OnDataChanged -= Appearance_OnDataChanged;
        ui.OnColorModeChanged -= Ui_OnColorModeChanged;
    }

    private void ApplySettings(bool isFirstCall = false)
    {
        ////窗口rect只允许在启动时更新（读取上次的位置）
        //if (isFirstCall && !_mgr.Data.Window.IsEmpty)
        //{
        //    if (_mgr.Data.Window.Width >= MinWidth && _mgr.Data.Window.Height >= MinHeight)
        //    {
        //        this.Width = _mgr.Data.Window.Width;
        //        this.Height = _mgr.Data.Window.Height;
        //        if (_mgr.Data.Window.X >= 0 && _mgr.Data.Window.Y >= 0)
        //        {
        //            this.Left = _mgr.Data.Window.X;
        //            this.Top = _mgr.Data.Window.Y;
        //        }
        //    }
        //}
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

    private void MainWindow_MouseLeave(object sender, MouseEventArgs e)
    {
       // LyricToolBar.Visibility = Visibility.Collapsed;
    }

    private void MainWindow_MouseEnter(object sender, MouseEventArgs e)
    {
        //LyricToolBar.Visibility = Visibility.Visible;
    }

}