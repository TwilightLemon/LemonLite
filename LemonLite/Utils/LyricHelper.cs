using LemonLite.Entities;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace LemonLite.Utils;

public static class LyricHelper
{
    public static string EndPoint { get; set; } = "http://localhost:5000";
    private static async Task<LyricData?> GetLyricOnline(string id,string source, CancellationToken cancellationToken = default)
    {
        try
        {
            var hc = App.Services.GetRequiredService<IHttpClientFactory>().CreateClient(App.AzureLiteHttpClientFlag);
            hc.BaseAddress = new Uri(EndPoint);
            var data = await hc.GetStringAsync($"/lrc?id={id}&source={source}", cancellationToken);
            if (JsonNode.Parse(data) is { } json)
            {
                var result = new LyricData
                {
                    Id = id,
                    Lyric = json["lyrics"]?.ToString(),
                    Romaji = json["romaji"]?.ToString(),
                    Trans = json["trans"]?.ToString(),
                    Type = source.ToLower() switch
                    {
                        "qqmusic" => LyricType.QQ,
                        "netease" or "cloudmusic" => LyricType.Netease,
                        _ => throw new InvalidOperationException()
                    }
                };
                if (json["isPureTimeline"]?.GetValue<bool>() is true) result.Type = LyricType.PureLrc;
                return result;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch { }
        return null;
    }

    private static string GetSearchCacheKey(string title, string artist, string album, int durationMs)
    {
        var input = $"{title}|{artist}|{album}|{durationMs}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..16];
    }

    public static async Task<MusicMetaData?> SearchMusicAsync(string title, string artist, string album, int durationMs, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to load from cache first
            var cacheKey = GetSearchCacheKey(title, artist, album, durationMs);
            var cachePath = System.IO.Path.Combine(Settings.CachePath, $"{cacheKey}.meta");
            if (await Settings.LoadFromJsonAsync<MusicMetaData>(cachePath, false) is { Id: not null } cached)
            {
                return cached;
            }

            var hc = App.Services.GetRequiredService<IHttpClientFactory>().CreateClient(App.AzureLiteHttpClientFlag);
            hc.BaseAddress = new Uri(EndPoint);
            var data = await hc.GetStringAsync($"/search?title={HttpUtility.UrlEncode(title)}&artist={HttpUtility.UrlEncode(artist)}&album={HttpUtility.UrlEncode(album)}&ms={durationMs}", cancellationToken);
            if (!string.IsNullOrEmpty(data))
            {
                var result = JsonSerializer.Deserialize<MusicMetaData>(data);
                // Cache the result if valid
                if (result?.Id != null)
                {
                    await Settings.SaveAsJsonAsync(result, cachePath, false);
                }
                return result;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch { }
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