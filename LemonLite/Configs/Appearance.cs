using LemonLite.Utils;
using System.Windows;

namespace LemonLite.Configs;

public class Appearance
{
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

    /// <summary>
    /// 主窗口置顶
    /// </summary>
    public bool TopMost { get; set; }

    /// <summary>
    /// 主窗口大小和位置
    /// </summary>
    public Rect Window { get; set; }
}
