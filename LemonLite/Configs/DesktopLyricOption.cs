using System.Windows;

namespace LemonLite.Configs;

public class DesktopLyricOption
{
    public Size WindowSize { get; set; } = new(0, 0);
    public bool ShowTranslation { get; set; } = true;
    public double LrcFontSize { get; set; } = 32d;
    public string FontFamily { get; set; } = "Segoe UI";
}