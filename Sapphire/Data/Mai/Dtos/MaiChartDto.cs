using System.ComponentModel.DataAnnotations;

namespace Sapphire.Data.Mai.Dtos;

public record MaiChartDto
{
    public int ChartId { get; init; }
    public int SapphireId { get; init; }
    public int LevelIndex { get; init; }

    public int? CnLevel { get; init; }
    public int? JpLevel { get; init; }
    public int? FitLevel { get; init; }

    public int? TapCount { get; init; }
    public int? HoldCount { get; init; }
    public int? SlideCount { get; init; }
    public int? TouchCount { get; init; }
    public int? BreakCount { get; init; }

    [MaxLength(512)] public string? CharterName { get; init; }

    public MaiSongDto? Song { get; init; }
}