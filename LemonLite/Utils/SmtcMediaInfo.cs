using Windows.Media;

namespace LemonLite.Utils;

/// <summary>
/// Normalized SMTC media metadata produced by <see cref="SmtcMetadataProcessorPipeline"/>.
/// All string fields are guaranteed non-null; processors may freely mutate them.
/// </summary>
public class SmtcMediaInfo
{
    public MediaPlaybackType? PlaybackType { get; set; }
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public string Album { get; set; } = "";
}
