using LemonLite.Utils;
using LemonLite.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace LemonLite.Views.Windows
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : FluentWindowBase
    {
        private readonly SettingsWindowViewModel vm;
        public SettingsWindow(SettingsWindowViewModel vm)
        {
            InitializeComponent();
            this.DataContext = this.vm = vm;
            Loaded += SettingsWindow_Loaded;
        }
        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            vm.SelectedMenu = vm.SettingsMenus.FirstOrDefault();
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
