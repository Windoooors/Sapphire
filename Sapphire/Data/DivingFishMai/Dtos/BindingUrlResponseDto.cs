using System.Text.Json.Serialization;

namespace Sapphire.Data.DivingFishMai.Dtos;

public record BindingUrlResponseDto
{
    [JsonPropertyName("device_code")] public required string DeviceCode { get; init; }
    [JsonPropertyName("user_code")] public required string UserCode { get; init; }
    [JsonPropertyName("verification_uri")] public required string Url { get; init; }

    [JsonPropertyName("verification_uri_complete")]
    public required string CompleteUrl { get; init; }

    [JsonPropertyName("expires_in")] public required uint ExpirationPeriod { get; init; }
}