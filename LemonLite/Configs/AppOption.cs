using System.Collections.Generic;

namespace LemonLite.Configs;
public class AppOption
{
    public bool StartWithMainWindow { get; set; } = false;
    public bool StartWithDesktopLyric{ get; set; } = false;
    public string LiteLyricServerHost { get; set; } = "https://lemonlite.azurewebsites.net/";

    public List<string> SmtcMediaIds { get; set; } = ["lemonapp.exe","qqmusic.exe","cloudmusic.exe"];
}
