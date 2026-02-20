using LemonLite.Entities;
using LemonLite.Sources;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using Lyricify.Lyrics.Searchers.Helpers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LemonLite.Utils;

public static class LyricHelper
{
    private static Task<LyricData?> GetLyricOnline(string id, string source, CancellationToken cancellationToken = default)
    {
        var src = LyricSourceRegistry.Get(source);
        if (src is null) return Task.FromResult<LyricData?>(null);
        return src.GetLyricAsync(id, cancellationToken);
    }

    public static async Task<MusicMetaData?> SearchMusicAsync(string title, string artist, string album, int durationMs, IReadOnlyList<string>? sources = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(album)) album = null;
            var sourcesToSearch = (sources != null && sources.Count > 0)
                ? sources
                : LyricSourceRegistry.DefaultSourceIds;

            var metadata = new TrackMetadata() { Title = title, Artist = artist, Album = album, DurationMs = durationMs };

            foreach (var srcId in sourcesToSearch)
            {
                var src = LyricSourceRegistry.Get(srcId);
                if (src is null) continue;

                var result = await src.CreateSearcher().SearchForResult(metadata);
                if (result is not null && result.MatchType >= CompareHelper.MatchType.Medium)
                {
                    var mapped = src.MapSearchResult(result);
                    if (mapped is not null) return mapped;
                }
            }
        }
        catch { throw; }
        return null;
    }

    public static async Task<LyricData?> GetLyricById(string id, string source, CancellationToken cancellationToken = default)
    {
        var path = Settings.CachePath;
        path = System.IO.Path.Combine(path, id + ".lmrc");
        if (await Settings.LoadFromJsonAsync<LyricData>(path, false) is { } local)
        {
            return local;
        }
        else
        {
            if (await GetLyricOnline(id, source, cancellationToken) is { } ly)
            {
                await Settings.SaveAsJsonAsync(ly, path, false);
                return ly;
            }
        }
        return null;
    }

    public static (LyricsData? lrc, LyricsData? trans, LyricsData? romaji, bool isPureLrc) LoadLrc(LyricData dt)
    {
        if (dt.Lyric == null) return (null, null, null, false);
        var rawType = dt.Type switch
        {
            LyricType.Netease => LyricsRawTypes.Yrc,
            LyricType.PureLrc => LyricsRawTypes.Lrc,
            LyricType.QQ => LyricsRawTypes.Qrc,
            _ => LyricsRawTypes.Lrc
        };
        var lrc = ParseHelper.ParseLyrics(dt.Lyric, rawType);
        LyricsData? trans = null, romaji = null;
        if (!string.IsNullOrEmpty(dt.Trans))
            trans = ParseHelper.ParseLyrics(dt.Trans, LyricsRawTypes.Lrc);

        if (!string.IsNullOrEmpty(dt.Romaji))
            romaji = ParseHelper.ParseLyrics(dt.Romaji, rawType);

        if (lrc != null)
            return (lrc, trans, romaji, dt.Type == LyricType.PureLrc);
        return (null, null, null, false);
    }
}