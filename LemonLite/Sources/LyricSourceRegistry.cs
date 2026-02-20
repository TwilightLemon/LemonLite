using System.Collections.Generic;
using System.Linq;

namespace LemonLite.Sources;

/// <summary>
/// Central registry of all available lyric sources.
/// New sources can be added at startup via <see cref="Register"/>.
/// </summary>
public static class LyricSourceRegistry
{
    private static readonly Dictionary<string, ILyricSource> _registry =
        new(System.StringComparer.OrdinalIgnoreCase);

    static LyricSourceRegistry()
    {
        Register(new QQMusicSource());
        Register(new NeteaseSource());
    }

    /// <summary>
    /// Registers a lyric source. Overwrites any existing registration with the same <see cref="ILyricSource.Id"/>.
    /// </summary>
    public static void Register(ILyricSource source) => _registry[source.Id] = source;

    /// <summary>
    /// Returns the source registered under <paramref name="id"/>, or <c>null</c> if not found.
    /// The lookup is case-insensitive.
    /// </summary>
    public static ILyricSource? Get(string id) =>
        _registry.TryGetValue(id, out var s) ? s : null;

    /// <summary>
    /// All registered sources in registration order.
    /// </summary>
    public static IReadOnlyCollection<ILyricSource> All => _registry.Values.ToList();

    /// <summary>
    /// Ordered list of all registered source IDs — used as the fallback search order.
    /// </summary>
    public static IReadOnlyList<string> DefaultSourceIds => _registry.Keys.ToList();
}
