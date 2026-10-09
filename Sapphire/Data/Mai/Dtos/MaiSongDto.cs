using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Sapphire.Data.Mai.Dtos;

public enum MaiSongType
{
    Dx,
    Std,
    Utage
}

public record MaiSongDto
{
    [MaxLength(512)]
    public string? ArtistName { get; init; } = "";
    [MaxLength(512)]
    public string? SongName { get; init; } = "";

    public List<MaiChartDto> Charts { get; init; } = [];
    public int? CnVersionIndex { get; init; }
    public int? JpVersionIndex { get; init; }
    
    public bool? AvailableInUsa { get; init; }
    public bool? AvailableInIntl { get; init; }
    public bool? AvailableInJp { get; init; }
    public bool? AvailableInCn { get; init; }
    
    public int? SongBpm { get; init; }
    public int? CategoryIndex { get; init; }
    public int? SongId { get; init; }
    public int SapphireId { get; init; }
    
    public MaiSongType? SongType { get; init; }
    
    public DateOnly? ReleaseDate { get; init; }
    
    public virtual bool Equals(MaiSongDto? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return SapphireId == other.SapphireId;
    }

    public override int GetHashCode()
    {
        return SapphireId;
    }
}