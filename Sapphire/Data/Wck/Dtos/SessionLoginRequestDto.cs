using System.Text.Json.Serialization;

namespace Sapphire.Data.Wck.Dtos;

public record SessionLoginRequestDto
{
    [JsonPropertyName("WechatQrCode")] public required string WechatQrCode { get; set; }
    [JsonPropertyName("OpenId")] public required string OpenId { get; set; }
}