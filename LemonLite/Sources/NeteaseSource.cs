using LemonLite.Entities;
using Lyricify.Lyrics.Searchers;
using System.Threading;
using System.Threading.Tasks;

namespace LemonLite.Sources;

public class NeteaseSource : ILyricSource
{
    public string Id => "netease";
    public string DisplayName => "Netease Music";

    public ISearcher CreateSearcher() => new NeteaseSearcher();

    public MusicMetaData? MapSearchResult(ISearchResult result)
    {
        if (result is not NeteaseSearchResult r) return null;
        return new MusicMetaData
        {
            Id = r.Id,
            Searcher = r.Searcher,
            Title = r.Title,
            Artists = r.Artists,
            Album = r.Album,
            DurationMs = r.DurationMs ?? 0
        };
    }

    public async Task<LyricData?> GetLyricAsync(string id, CancellationToken cancellationToken = default)
    {
        var api = new Lyricify.Lyrics.Providers.Web.Netease.Api();
        var data = await api.GetLyricNew(id);
        if (data == null) return null;

        var result = new LyricData
        {
            Lyric = data.Yrc?.Lyric ?? data.Lrc.Lyric,
            Trans = data.Tlyric?.Lyric,
            Romaji = data.Romalrc?.Lyric
        };

        if (data.Yrc?.Lyric == null && data.Lrc.Lyric != null)
            result.Type = LyricType.PureLrc;

        return result;
    }
}
