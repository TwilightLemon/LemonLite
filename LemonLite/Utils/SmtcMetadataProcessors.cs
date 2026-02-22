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
internal sealed class NameAliaMetadataProcessor(SettingsMgr<SmtcMetadataAliasConfig> aliases) : ISmtcMetadataProcessor
{
    public bool CanProcess(string appId)
    {
        return aliases.Data.ContainsKey(appId);
    }

    public void Process(SmtcMediaInfo info, string appId)
    {
        foreach(var alias in aliases.Data[appId])
        {
            if (string.IsNullOrEmpty(alias.Target)) continue;
            switch (alias.Type)
            {
                case SmtcMetadataAliasType.Artist:
                    info.Artist = info.Artist.Replace(alias.Target, alias.Name);
                    break;
                case SmtcMetadataAliasType.Album:
                    info.Album = info.Album.Replace(alias.Target, alias.Name);
                    break;
                case SmtcMetadataAliasType.Name:
                    if (alias.VerifyCondition(info.Artist,info.Title,info.Album))
                        info.Title = info.Title.Replace(alias.Target, alias.Name);
                    break;
                case SmtcMetadataAliasType.All:
                    info.Artist = info.Artist.Replace(alias.Target, alias.Name);
                    info.Album = info.Album.Replace(alias.Target, alias.Name);
                    info.Title = info.Title.Replace(alias.Target, alias.Name);
                    break;
            }
        }
    }
}
