using LemonLite.Entities;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace LemonLite.Utils;

public static class LyricHelper
{
    public static string EndPoint { get; set; } = "http://localhost:5000";
    public static async Task<LyricData?> GetLyricByQid(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var hc = App.Services.GetRequiredService<IHttpClientFactory>().CreateClient(App.AzureLiteHttpClientFlag);
            hc.BaseAddress = new Uri(EndPoint);
            var data = await hc.GetStringAsync($"/lrc?id={id}", cancellationToken);
            if (JsonNode.Parse(data) is { } json)
            {
                return new() { Id = id, Lyric = json["lyrics"]?.ToString(), Romaji = json["romaji"]?.ToString(), Trans = json["trans"]?.ToString(), Type = LyricType.QQ };
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch { }
        return null;
    }

    public static async Task<MusicMetaData?> SearchMusicAsync(string title, string artist, string album, int durationMs, CancellationToken cancellationToken = default)
    {
        try
        {
            var hc = App.Services.GetRequiredService<IHttpClientFactory>().CreateClient(App.AzureLiteHttpClientFlag);
            hc.BaseAddress = new Uri(EndPoint);
            var data = await hc.GetStringAsync($"/search?title={HttpUtility.UrlEncode(title)}&artist={HttpUtility.UrlEncode(artist)}&album={HttpUtility.UrlEncode(album)}&ms={durationMs}", cancellationToken);
            if (!string.IsNullOrEmpty(data))
            {
                return JsonSerializer.Deserialize<MusicMetaData>(data);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch { }
        return null;
    }

    public static async Task<LyricData?> GetLyricByQmId(string id, CancellationToken cancellationToken = default)
    {
        var path = Settings.CachePath;
        path = System.IO.Path.Combine(path, id + ".lmrc");
        if (await Settings.LoadFromJsonAsync<LyricData>(path, false) is { } local)
        {
            return local;
        }
        else
        {
            if (await GetLyricByQid(id, cancellationToken) is { } ly)
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
            LyricType.Wyy => LyricsRawTypes.Yrc,
            LyricType.PureWyy => LyricsRawTypes.Lrc,
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
            return (lrc, trans, romaji, dt.Type == LyricType.PureWyy);
        return (null, null, null, false);
    }
}