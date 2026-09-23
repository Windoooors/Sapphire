namespace Sapphire.Data.Chuni.Dtos;

public record ChuniSongDto
{
    public string ArtistName { get; init; } = "";
    public string SongName { get; init; } = "";

    public string WeKanji { get; init; } = "";
    public int StarDifficulty { get; init; } = -1;
    public required ChuniChartDto[] Charts { get; init; }
    public int CnVersionIndex { get; init; }
    public int JpVersionIndex { get; init; }
    public bool DeletedInJp { get; init; }
    public bool DeletedInCn { get; init; }
    public int SongBpm { get; init; }
    public int CategoryIndex { get; init; }
    public int SapphireId { get; init; }
    public int SongId { get; init; }
}