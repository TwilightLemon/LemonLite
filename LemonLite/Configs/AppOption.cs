using LemonLite.Sources;
using System.Collections.Generic;
using System.Linq;

namespace LemonLite.Configs;

public class SmtcAppConfig
{
    public string AppId { get; set; } = "";
    /// <summary>
    /// Ordered list of source IDs to search. See <see cref="LyricSourceRegistry"/> for registered IDs.
    /// An empty list means use the registry default order.
    /// </summary>
    public List<string> SearchSources { get; set; } = [];
}

public class AppOption
{
    public bool StartWithMainWindow { get; set; } = true;
    public bool StartWithDesktopLyric{ get; set; } = true;
    public bool EnableAudioVisualizer { get; set; } = false;

    public List<SmtcAppConfig> SmtcApps { get; set; } =
    [
        new SmtcAppConfig { AppId = "lemonapp.exe" },
        new SmtcAppConfig { AppId = "qqmusic.exe" },
        new SmtcAppConfig { AppId = "cloudmusic.exe", SearchSources = ["netease", "qq music"] },
        new SmtcAppConfig { AppId = "applemusic.exe" }
    ];

    public IReadOnlyList<string> GetSearchSources(string? appId)
    {
        if (!string.IsNullOrEmpty(appId))
        {
            var config = SmtcApps.FirstOrDefault(a => a.AppId == appId.ToLower());
            if (config != null && config.SearchSources.Count > 0)
                return config.SearchSources;
        }
        return LyricSourceRegistry.DefaultSourceIds;
    }
}
