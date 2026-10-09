using System.Text.Json.Serialization;

namespace Sapphire.Data.Mai.Dtos.DivingFish;

public record AccessTokenDto
{
    [JsonPropertyName("access_token")] public required string AccessToken { get; init; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
}