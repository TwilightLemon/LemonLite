using LemonLite.Configs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
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
                    var value = _resourceManager.GetString(key, culture);
                    return value ?? key;
                }
                catch
                {
                    return key;
                }
            }
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
