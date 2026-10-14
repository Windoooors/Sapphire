using System.Text.Json.Serialization;

namespace Sapphire.Data.Wck.Dtos;

public record SessionLogoutResponseDto
{
    [JsonPropertyName("Code")] public int Code { get; set; }
}