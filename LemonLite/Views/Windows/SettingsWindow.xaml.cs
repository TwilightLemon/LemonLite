using LemonLite.Services;
using LemonLite.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LemonLite.Views.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsWindowViewModel vm;
        private readonly UIResourceService ui;

        public SettingsWindow(SettingsWindowViewModel vm, UIResourceService ui)
        {
            InitializeComponent();
            this.DataContext = this.vm = vm;
            Loaded += SettingsWindow_Loaded;
            this.ui = ui;
            ui.OnColorModeChanged += Ui_OnColorModeChanged;
            Closed += SettingsWindow_Closed;
        }

        private void SettingsWindow_Closed(object? sender, EventArgs e)
        {
            ui.OnColorModeChanged -= Ui_OnColorModeChanged;
        }

        private void Ui_OnColorModeChanged()
        {
            material.IsDarkMode = ui.GetIsDarkMode();
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            vm.SelectedMenu = vm.SettingsMenus.FirstOrDefault();
            material.IsDarkMode = ui.GetIsDarkMode();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WindowFrame.Navigate(vm.CurrentPageContent);

            // Clear history
            if (WindowFrame.CanGoBack || WindowFrame.CanGoForward)
            {
                JournalEntry? history;

                do
                {
                    history = WindowFrame.RemoveBackEntry();
                }
                while (history is not null);
            }
        }
    }
}
