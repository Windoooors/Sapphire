using System.Text.Json.Serialization;

namespace Sapphire.Data.Wck.Dtos;

public record WckMusicDataResponseDto
{
    [JsonPropertyName("Code")] public int Code { get; set; }

    [JsonPropertyName("MusicData")] public WckMusicDataResponseItemDto[]? MusicData { get; set; }
}