namespace Sapphire.Data.Mai.Dtos;

public record MaiVersionDto
{
    public int VersionIndex { get; init; }
    public string? JpName { get; init; }
    public string? CnName { get; init; }
    public string? JpBriefName { get; init; }
    public string? CnBriefName { get; init; }
}