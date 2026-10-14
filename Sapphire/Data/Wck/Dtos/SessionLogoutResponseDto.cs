using System.Text.Json.Serialization;

namespace Sapphire.Data.Wck.Dtos;

public record SessionLoginResponseDto
{
    [JsonPropertyName("Code")] public int Code { get; set; }
}