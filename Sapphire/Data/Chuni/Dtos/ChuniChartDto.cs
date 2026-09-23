namespace Sapphire.Data.Chuni.Dtos;

public record ChuniChartDto
{
    public int ChartId { get; init; }
    public int SapphireId { get; init; }
    public int LevelIndex { get; init; }

    public int CnLevel { get; init; } = -1;
    public int JpLevel { get; init; } = -1;
    
    public int NoteCount { get; init; }
    public required string CharterName { get; init; }
    public ChuniSongDto? Song { get; init; }
}