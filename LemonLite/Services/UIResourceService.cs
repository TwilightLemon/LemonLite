using LemonLite.Configs;
using LemonLite.Utils;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace LemonLite.Services;
/// <summary>
/// 设置全局UI资源
/// </summary>
public class UIResourceService
{
    public event Action? OnColorModeChanged;
    private readonly SettingsMgr<Appearance> _settingsMgr;
    public bool GetIsDarkMode() => _settingsMgr.Data?.GetIsDarkMode() == true;
    private bool _appCurrentDarkMode= false;
    public SettingsMgr<Appearance> SettingsMgr=> _settingsMgr;
    public UIResourceService(AppSettingService appSettingsService)
    {
        _settingsMgr = appSettingsService.GetConfigMgr<Appearance>();
        _settingsMgr.OnDataChanged += SettingsMgr_OnDataChanged;

        UpdateAppFontFamily();
        UpdateColorMode();
    }

    public void UpdateAppFontFamily()
    {
        var fontFamily = _settingsMgr.Data?.DefaultFontFamily;
        if (string.IsNullOrEmpty(fontFamily)) return; 
        App.Current.Resources["DefaultFontFamily"] = new FontFamily(fontFamily);
    }

    private void SettingsMgr_OnDataChanged()
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            UpdateColorMode();
        });
    }

    public void UpdateColorMode()
    {
        bool IsDarkMode = GetIsDarkMode();
        if(IsDarkMode==_appCurrentDarkMode) return;
        _appCurrentDarkMode = IsDarkMode;

        string uri = $"pack://application:,,,/LemonLite;component/Resources/ThemeColor_{IsDarkMode switch
        {
            true => "Dark",
            false => "Light",
        }}.xaml";
        // 移除当前主题资源字典（如果存在）
        var oldDict=App.Current.Resources.MergedDictionaries.FirstOrDefault(d=>d.Source!=null&&d.Source.OriginalString.Contains("Resources/ThemeColor"));
        if(oldDict!=null)
            App.Current.Resources.MergedDictionaries.Remove(oldDict);
        // 添加新的主题资源字典
        App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary(){Source=new Uri(uri,UriKind.Absolute)});

        OnColorModeChanged?.Invoke();
    }

    public static void UpdateAccentColor(Color accentColor, Color focusColor){
        App.Current.Resources["AccentColor"] = App.Current.Resources["HighlightThemeColor"] = new SolidColorBrush(accentColor);
        App.Current.Resources["AccentColorKey"] = accentColor;
        App.Current.Resources["FocusAccentColor"] = new SolidColorBrush(focusColor);
    }

}