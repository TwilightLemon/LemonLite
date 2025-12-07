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

    /// <summary>
    /// 应用内默认字体
    /// </summary>
    public string DefaultFontFamily { get; set; } = ".PingFang SC,Segoe UI";

    //以下是新增的设置项
    public enum BackgroundType { None,Acrylic,Image}
    public BackgroundType Background { get; set; } = BackgroundType.Acrylic;
    public double AcylicOpacity { get; set; } = 0.86d;

    public string? BackgroundImagePath { get; set; } = string.Empty;
    public double BackgroundOpacity { get; set; }
    }
