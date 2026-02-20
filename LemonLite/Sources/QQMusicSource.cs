using LemonLite.Entities;
using LemonLite.Utils;
using Lyricify.Lyrics.Searchers;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LemonLite.Sources;

public class QQMusicSource : ILyricSource
{
    public string Id => "qq music";
    public string DisplayName => "QQ Music";

    public ISearcher CreateSearcher() => new QQMusicSearcher();

    public MusicMetaData? MapSearchResult(ISearchResult result)
    {
        if (result is not QQMusicSearchResult r) return null;
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

    public Task<LyricData?> GetLyricAsync(string id, CancellationToken cancellationToken = default)
    {
        var hc = App.Services
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(App.DefaultHttpClientFlag);
        return TencGetLyric.GetLyricsAsync(hc, id);
    }
}
