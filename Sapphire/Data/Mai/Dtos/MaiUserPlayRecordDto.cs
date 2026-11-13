using System.ComponentModel.DataAnnotations;

namespace Sapphire.Data.Mai.Dtos;

public record MaiUserPlayRecordDto
{
    [MaxLength(64)] public string UserId { get; init; } = "";

    public byte[]? Data { get; set; } = [];
}
