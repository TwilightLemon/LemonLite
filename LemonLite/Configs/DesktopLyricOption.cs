using System.Windows;

namespace LemonLite.Configs;

public class DesktopLyricOption
{
    public Size WindowSize { get; set; } = new(0, 0);
    public bool ShowTranslation { get; set; } = true;
    public bool ShowRomaji { get; set; } = true;
    public double LrcFontSize { get; set; } = 32d;
    public string FontFamily { get; set; } = "Segoe UI";
    public bool EnableBackground { get; set; } = true;
    public bool UseHighlightLyricEffect { get; set; } = true;

    public bool IsIslandMode { get; set; } = false;

    /// <summary>
    /// Island 模式固定宽度（0 = 使用默认 480）
    /// </summary>
    public double IslandWidth { get; set; } = 480d;
}
