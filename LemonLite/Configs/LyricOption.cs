namespace LemonLite.Configs;

public class LyricOption
{
    public bool ShowTranslation { get; set; } = true;
    public bool ShowRomaji { get; set; } = true;
    public int FontSize { get; set; } = 24;
    /// <summary>
    /// LyricView的字体选项
    /// </summary>
    public string FontFamily { get; set; } = "Segoe UI"; 
}
