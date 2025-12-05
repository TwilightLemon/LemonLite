using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace LemonLite.Views.Pages
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string AppName => "LemonLite";

        public string AppVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return $"Version {version?.Major}.{version?.Minor}.{version?.Build}";
            }
        }

        public string Publisher => "TwilightLemon";

        public string Description => "A lyric viewer powered by Lemon App, integrated with SMTC.";

        public string CopyrightYear => $"© {DateTime.Now.Year} TwilightLemon. All rights reserved.";

        public ObservableCollection<OpenSourceComponent> OpenSourceComponents { get; } = new()
        {
            new OpenSourceComponent
            {
                Name = "CommunityToolkit.Mvvm",
                Version = "8.4.0",
                License = "MIT",
                Url = "https://github.com/CommunityToolkit/dotnet"
            },
            new OpenSourceComponent
            {
                Name = "EleCho.WpfSuite",
                Version = "0.8.1",
                License = "MIT",
                Url = "https://github.com/OrgEleCho/EleCho.WpfSuite"
            },
            new OpenSourceComponent
            {
                Name = "FluentWpfCore",
                Version = "1.0.0.4",
                License = "MIT",
                Url = "https://github.com/TwilightLemon/FluentWpfCore"
            },
            new OpenSourceComponent
            {
                Name = "H.NotifyIcon.Wpf",
                Version = "2.1.4",
                License = "MIT",
                Url = "https://github.com/HavenDV/H.NotifyIcon"
            },
            new OpenSourceComponent
            {
                Name = "Lyricify.Lyrics.Helper",
                Version = "0.1.4",
                License = "MIT",
                Url = "https://github.com/WXRIW/Lyricify-Lyrics-Helper"
            },
            new OpenSourceComponent
            {
                Name = "Microsoft.Xaml.Behaviors.Wpf",
                Version = "1.1.135",
                License = "MIT",
                Url = "https://github.com/microsoft/XamlBehaviorsWpf"
            }
        };

        [RelayCommand]
        private void OpenUrl(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        [RelayCommand]
        private void OpenGitHub()
        {
            OpenUrl("https://github.com/TwilightLemon/LemonLite");
        }
    }

    public class OpenSourceComponent
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string License { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
