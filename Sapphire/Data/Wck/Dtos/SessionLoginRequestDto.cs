using System.Text.Json.Serialization;

namespace Sapphire.Data.Wck.Dtos;

public record SessionLoginRequestDto
{
    [JsonPropertyName("WechatQrCode")] public required string WechatQrCode { get; set; }
    [JsonPropertyName("QqId")] public long QqId { get; set; }
}