namespace LemonLite.Entities;

public enum LyricType
{
    QQ,Netease,PureLrc
}
public class LyricData
{
    public LyricType Type { get; set; }
    public string? Lyric { get; set; }
    public string? Id { get; set; }
    public string? Trans { get; set; }
    public string? Romaji { get; set; }
}