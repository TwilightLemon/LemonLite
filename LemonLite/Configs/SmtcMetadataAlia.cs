using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonLite.Configs;

public enum SmtcMetadataAliaType
{
    Artist,
    Name,
    Album,
    All
}
public class SmtcMetadataAliaItem
{
    public required string AppId { get; set; }
    public required SmtcMetadataAliaType Type { get; set; }
    public required string Target { get; set; }
    public required string Name { get; set; }
}

public class SmtcMetadataAliaConfig:Dictionary<string, List<SmtcMetadataAliaItem>>
{
    public SmtcMetadataAliaConfig() : base(StringComparer.OrdinalIgnoreCase) { }
}