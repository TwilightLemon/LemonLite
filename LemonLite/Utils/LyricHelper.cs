using LemonLite.Entities;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using Lyricify.Lyrics.Searchers;
using Lyricify.Lyrics.Searchers.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LemonLite.Utils;
//先丑陋地把服务器去掉了... 日后再优化
public static class LyricHelper
{
    private static async Task<LyricData?> GetLyricOnline(string id,string source, CancellationToken cancellationToken = default)
    {
        try
        {
            if (source == "qq music" || string.IsNullOrEmpty(source))
            {
                var hc = App.Services.GetRequiredService<IHttpClientFactory>().CreateClient(App.DefaultHttpClientFlag);
                return await TencGetLyric.GetLyricsAsync(hc, id);
            }
            else if (source is "cloudmusic" or "netease")
            {
                var api = new Lyricify.Lyrics.Providers.Web.Netease.Api();
                var data = await api.GetLyricNew(id);
                if (data == null) return null;
                var result = new LyricData()
                {
                    Lyric = data.Yrc?.Lyric ?? data.Lrc.Lyric,
                    Trans = data.Tlyric?.Lyric,
                    Romaji = data.Romalrc?.Lyric
                };
                if (data.Yrc?.Lyric == null && data.Lrc.Lyric != null)
                {
                    //没有单句分词
                    result.Type = LyricType.PureLrc;
                }
                return result;
            }
        }
        catch { throw; }
        return null;
    }

    public static async Task<MusicMetaData?> SearchMusicAsync(string title, string artist, string album, int durationMs, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(album)) album = null;
            var defaultSources = new[] { "qq music", "netease" };
            var artists = artist.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var metadata = new TrackMetadata() { Title = title, Artist = artist, Album = album, DurationMs = durationMs };

            foreach (var src in defaultSources)
            {
                ISearcher? searcher = src switch
                {
                    "netease" or "cloudmusic" => new NeteaseSearcher(),
                    // "kugou" => new Lyricify.Lyrics.Searchers.KugouSearcher(),
                    "qq music" => new QQMusicSearcher(),
                    _ => null
                };

                if (searcher is null)
                {
                    continue;
                }

                var result = await searcher.SearchForResult(metadata);
                if (result is not null && (result.MatchType >= CompareHelper.MatchType.Medium ))
                {
                    return result switch
                    {
                        NeteaseSearchResult nrs => new MusicMetaData()
                        {
                            Id = nrs.Id,
                            Searcher = nrs.Searcher,
                            Title = nrs.Title,
                            Artists = nrs.Artists,
                            Album = nrs.Album,
                            DurationMs = nrs.DurationMs ?? 0
                        },
                        QQMusicSearchResult qqrs => new MusicMetaData()
                        {
                            Id = qqrs.Id,
                            Searcher = qqrs.Searcher,
                            Title = qqrs.Title,
                            Artists = qqrs.Artists,
                            Album = qqrs.Album,
                            DurationMs = qqrs.DurationMs ?? 0
                        },
                        _ => null
                    };
                }
            }
        }
        catch { throw; }
        return null;
    }

    public static async Task<LyricData?> GetLyricById(string id,string source, CancellationToken cancellationToken = default)
    {
        var path = Settings.CachePath;
        path = System.IO.Path.Combine(path, id + ".lmrc");
        if (await Settings.LoadFromJsonAsync<LyricData>(path, false) is { } local)
        {
            return local;
        }
        else
        {
            if (await GetLyricOnline(id,source, cancellationToken) is { } ly)
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