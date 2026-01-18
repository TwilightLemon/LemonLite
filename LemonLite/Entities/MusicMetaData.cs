using Lyricify.Lyrics.Searchers;
using System.Text.Json.Serialization;

namespace LemonLite.Entities;

/// <summary>
/// 音乐元数据
/// </summary>
public class MusicMetaData
{
    [JsonPropertyName("searcher")]
    public ISearcher? Searcher { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("artists")]
    public string[]? Artists { get; set; }

    [JsonPropertyName("album")]
    public string? Album { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("mid")]
    public string? Mid { get; set; }

    [JsonPropertyName("albumArtists")]
    public string[]? AlbumArtists { get; set; }

    [JsonPropertyName("durationMs")]
    public int DurationMs { get; set; }

    [JsonPropertyName("matchType")]
    public int MatchType { get; set; }

    /// <summary>
    /// 获取格式化的歌手名称
    /// </summary>
    public string ArtistString => Artists != null ? string.Join("/ ", Artists) : string.Empty;
}
