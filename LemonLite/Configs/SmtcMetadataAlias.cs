using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonLite.Configs;

public enum SmtcMetadataAliasType
{
    Artist,
    Name,
    Album,
    All
}
public class SmtcMetadataAliasItem
{
    public required string AppId { get; set; }
    public required SmtcMetadataAliasType Type { get; set; }
    public required string Target { get; set; }
    public required string Name { get; set; }
    public string? Condition { get; set; } = null;
    public void SetConditionWithMetadata(string artist, string title,string album)
    {
        Condition=$"{artist}|{title}|{album}";
    }
    public bool VerifyCondition(string artist, string title, string album)
    {
        if (string.IsNullOrEmpty(Condition)) return true;
        var parts = Condition.Split('|',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        if (parts.Length != 3) return true;
        return (parts[0] == artist || parts[0] == "*")
            && (parts[1] == title  || parts[1] == "*")
            && (parts[2] == album  || parts[2] == "*");
    }
}

public class SmtcMetadataAliasConfig:Dictionary<string, List<SmtcMetadataAliasItem>>
{
    public SmtcMetadataAliasConfig() : base(StringComparer.OrdinalIgnoreCase) { }
}