using System.Text.Json.Serialization;

namespace Sapphire.Data.Wck.Dtos;

public record SessionDto
{
    [JsonPropertyName("Code")] public int Code { get; set; }
    [JsonPropertyName("SessionId")] public string? SessionId { get; set; }
}