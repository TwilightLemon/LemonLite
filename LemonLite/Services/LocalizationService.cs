using LemonLite.Configs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.IO;
using System.Runtime.CompilerServices;

namespace LemonLite.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        private ResourceManager _resourceManager;
        private string _currentLanguage = "en";

        public event Action? LanguageChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        private LocalizationService()
        {
            // 直接指定资源文件的命名空间和名称
            _resourceManager = new ResourceManager("LemonLite.Properties.Resources", typeof(LocalizationService).Assembly);
            LoadCurrentLanguage();
        }

        private void LoadCurrentLanguage()
        {
            var settings = App.Services.GetRequiredService<AppSettingService>().GetConfigMgr<AppOption>();
            _currentLanguage = settings.Data.Language ?? "en";
            SetCurrentCulture();
        }

        private void SetCurrentCulture()
        {
            CultureInfo culture;
            if (_currentLanguage == "zh")
            {
                culture = new CultureInfo("zh-CN");
            }
            else
            {
                culture = new CultureInfo("en-US");
            }
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        public string this[string key]
        {
            get
            {
                try
                {
                    // 转换键名，将空格替换为驼峰命名
                    var resourceKey = ConvertToResourceKey(key);
                    // 获取当前语言对应的CultureInfo
                    CultureInfo culture;
                    if (_currentLanguage == "zh")
                    {
                        culture = new CultureInfo("zh-CN");
                    }
                    else
                    {
                        culture = new CultureInfo("en-US");
                    }
                    // 指定CultureInfo获取资源字符串
                    var value = _resourceManager.GetString(resourceKey, culture);
                    return value ?? key;
                }
                catch
                {
                    return key;
                }
            }
        }

        private string ConvertToResourceKey(string key)
        {
            // 将空格替换为驼峰命名，并移除特殊字符
            var parts = key.Split(' ');
            var result = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    result += char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                }
            }
            // 特殊处理
            if (key == "Welcome~")
                return "Welcome";
            if (key == "SMTC Apps")
                return "SmtcApps";
            if (key == "Configure which apps are monitored. Click an app to configure its sources and aliases.")
                return "SmtcAppsDescription";
            if (key == "Play the last one")
                return "PlayTheLastOne";
            if (key == "Play or Pause")
                return "PlayOrPause";
            if (key == "Play the next one")
                return "PlayTheNextOne";
            if (key == "Create Metadata Alias")
                return "CreateMetadataAlias";
            if (key == "Show Translation and Romaji")
                return "ShowTranslationAndRomaji";
            if (key == "Show Main Window")
                return "ShowMainWindow";
            if (key == "Close desktop lyric viewer")
                return "CloseDesktopLyricViewer";
            if (key == "Language")
                return "Language";
            if (key == "English")
                return "English";
            if (key == "中文")
                return "Chinese";
            if (key == "Font size ++")
                return "FontSizeUp";
            if (key == "Font size --")
                return "FontSizeDown";
            return result;
        }

        public void SetLanguage(string language)
        {
            if ((language == "en" || language == "zh") && _currentLanguage != language)
            {
                _currentLanguage = language;
                var settings = App.Services.GetRequiredService<AppSettingService>().GetConfigMgr<AppOption>();
                settings.Data.Language = language;
                SetCurrentCulture();
                LanguageChanged?.Invoke();
                OnPropertyChanged("Item[]");
                OnPropertyChanged(string.Empty);
            }
        }

        public string CurrentLanguage => _currentLanguage;

        public IEnumerable<string> AvailableLanguages => new List<string> { "en", "zh" };
    }
}
