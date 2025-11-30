using LemonLite.Utils;
using System.Windows;

namespace LemonLite.Configs;

public class Appearance
{
    /// <summary>
    /// 窗口大小
    /// </summary>
    public Size WindowSize { get; set; } = new(0, 0);
    public enum ColorModeType { Auto, Dark, Light }

    /// <summary>
    /// 全局暗亮色模式
    /// </summary>
    public ColorModeType ColorMode { get; set; }
    public bool GetIsDarkMode() => ColorMode switch
    {
        ColorModeType.Dark => true,
        ColorModeType.Light => false,
        ColorModeType.Auto => !SystemThemeAPI.GetIsLightTheme(),
        _ => true //default to dark
    };
}
