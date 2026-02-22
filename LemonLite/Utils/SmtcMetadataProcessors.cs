using LemonLite.Configs;
using System;
using System.Collections.Generic;

namespace LemonLite.Utils;

/// <summary>
/// Normalizes raw SMTC metadata for a specific app.
/// Implement this interface and call <see cref="SmtcMetadataProcessorPipeline.Register"/> to add support for a new app.
/// </summary>
public interface ISmtcMetadataProcessor
{
    /// <summary>Returns <c>true</c> when this processor should run for the given app ID (lowercase).</summary>
    bool CanProcess(string appId);

    /// <summary>Mutates <paramref name="info"/> in-place to normalize the metadata.</summary>
    void Process(SmtcMediaInfo info, string appId);
}

/// <summary>
/// Central registry and runner for <see cref="ISmtcMetadataProcessor"/> instances.
/// Processors are applied in registration order by <see cref="SmtcListener.GetNormalizedMediaInfoAsync"/>.
/// </summary>
public static class SmtcMetadataProcessorPipeline
{
    private static readonly List<ISmtcMetadataProcessor> _processors =
    [
        new AppleMusicMetadataProcessor(),
    ];

    /// <summary>Appends a processor to the pipeline.</summary>
    public static void Register(ISmtcMetadataProcessor processor) => _processors.Add(processor);

    internal static IReadOnlyList<ISmtcMetadataProcessor> All => _processors;
}

/// <summary>
/// Apple Music reports metadata as "Artist — Album" inside the Artist field.
/// This processor splits the two values into their correct fields.
/// </summary>
internal sealed class AppleMusicMetadataProcessor : ISmtcMetadataProcessor
{
    public bool CanProcess(string appId) =>
        appId.Contains("applemusic", StringComparison.OrdinalIgnoreCase);

    public void Process(SmtcMediaInfo info, string appId)
    {
        if (string.IsNullOrEmpty(info.Artist)) return;

        var parts = info.Artist.Split('—', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
        {
            info.Artist = parts[0];
            info.Album  = parts[1];
        }
    }
}
internal sealed class NameAliaMetadataProcessor(SettingsMgr<SmtcMetadataAliaConfig> alias) : ISmtcMetadataProcessor
{
    public bool CanProcess(string appId)
    {
        return alias.Data.ContainsKey(appId);
    }

    public void Process(SmtcMediaInfo info, string appId)
    {
        foreach(var alia in alias.Data[appId])
        {
            if (string.IsNullOrEmpty(alia.Target)) continue;
            switch (alia.Type)
            {
                case SmtcMetadataAliaType.Artist:
                    info.Artist = info.Artist.Replace(alia.Target, alia.Name);
                    break;
                case SmtcMetadataAliaType.Album:
                    info.Album = info.Album.Replace(alia.Target, alia.Name);
                    break;
                case SmtcMetadataAliaType.Name:
                    info.Title = info.Title.Replace(alia.Target, alia.Name);
                    break;
                case SmtcMetadataAliaType.All:
                    info.Artist = info.Artist.Replace(alia.Target, alia.Name);
                    info.Album = info.Album.Replace(alia.Target, alia.Name);
                    info.Title = info.Title.Replace(alia.Target, alia.Name);
                    break;
            }
        }
    }
}
