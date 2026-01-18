using System.Collections.Generic;

namespace LemonLite.Configs;
public class AppOption
{
    public bool StartWithMainWindow { get; set; } = true;
    public bool StartWithDesktopLyric{ get; set; } = true;
    public bool EnableAudioVisualizer { get; set; } = false;

    public List<string> SmtcMediaIds { get; set; } =
    [
        "lemonapp.exe",
        "qqmusic.exe",
        "cloudmusic.exe",
        "appleinc.applemusicwin_nzyj5cx40ttqa!app"
    ];
}
