using LemonLite.Entities;
using Lyricify.Lyrics.Searchers;
using System.Threading;
using System.Threading.Tasks;

namespace LemonLite.Sources;

/// <summary>
/// Defines the contract for a lyric source that provides both music search and lyric retrieval.
/// </summary>
public interface ILyricSource
{
    /// <summary>
    /// Unique identifier for this source (lowercase, used in config storage).
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable display name shown in the UI.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Creates the Lyricify <see cref="ISearcher"/> used to search for music metadata.
    /// </summary>
    ISearcher CreateSearcher();

    /// <summary>
    /// Maps an <see cref="ISearchResult"/> returned by <see cref="CreateSearcher"/> to a
    /// <see cref="MusicMetaData"/> instance. Returns <c>null</c> if the result type is
    /// incompatible with this source.
    /// </summary>
    MusicMetaData? MapSearchResult(ISearchResult result);

    /// <summary>
    /// Fetches lyric data for the given track ID from the remote provider.
    /// </summary>
    Task<LyricData?> GetLyricAsync(string id, CancellationToken cancellationToken = default);
}
