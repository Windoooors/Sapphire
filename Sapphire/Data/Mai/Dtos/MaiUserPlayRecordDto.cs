namespace Sapphire.Data.Mai.Dtos;

public record MaiUserPlayRecordDto
{
    public long UserId { get; init; }
    public byte[]? Data { get; set; } = [];
}