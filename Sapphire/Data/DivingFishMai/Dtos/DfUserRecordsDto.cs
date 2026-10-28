using System.Text.Json.Serialization;
using Sapphire.Data.Mai.Dtos;

namespace Sapphire.Data.DivingFishMai.Dtos;

public record DfUserRecordsDto
{
    [JsonPropertyName("records")] public required DfUserRecordDto[] Records { get; init; }
}

public record DfUserRecordDto
{
    [JsonPropertyName("song_id")] public int SongId { get; init; }
    [JsonPropertyName("level_index")] public int LevelIndex { get; init; }
    [JsonPropertyName("achievements")] public float Achievement { get; init; }
    [JsonPropertyName("fc")] public required string FcState { get; init; }
    [JsonPropertyName("fs")] public required string FsState { get; init; }
    [JsonPropertyName("dxScore")] public int DxScore { get; init; }

    public MaiPlayRecord ToMaiPlayRecord()
    {
        var result = new MaiPlayRecord
        {
            ClearState = FcState switch
            {
                "app" => ClearState.AllPerfectPlus,
                "ap" => ClearState.AllPerfect,
                "fcp" => ClearState.FullComboPlus,
                "fc" => ClearState.FullCombo,
                _ => ClearState.Empty
            },
            SyncState = FsState switch
            {
                "fsdp" => SyncState.FullSyncDxPlus,
                "fsd" => SyncState.FullSyncDx,
                "fsp" => SyncState.FullSyncPlus,
                "fs" => SyncState.FullSync,
                "sync" => SyncState.Sync,
                _ => SyncState.Empty
            },
            DxScore = (ushort)DxScore,
            Score = (uint)(Achievement * 10000),
            PlayCount = 0
        };

        return result;
    }
}

public record DfUserRecordUploadDto
{
    [JsonPropertyName("achievements")] public float Achievements { get; init; }
    [JsonPropertyName("dxScore")] public int DxScore { get; init; }
    [JsonPropertyName("fc")] public required string Fc { get; init; }
    [JsonPropertyName("fs")] public required string Fs { get; init; }
    [JsonPropertyName("level_index")] public int LevelIndex { get; init; }
    [JsonPropertyName("title")] public required string Title { get; init; }
    [JsonPropertyName("type")] public required string Type { get; init; }

    public static DfUserRecordUploadDto? ConvertFromMaiPlayRecord(MaiPlayRecord maiPlayRecord, MaiChartDto chart)
    {
        if (chart.Song == null) return null;
        if (chart.Song.SongType == MaiSongType.Utage) return null;

        return new DfUserRecordUploadDto
        {
            Achievements = (float)maiPlayRecord.Score / 10000,
            DxScore = maiPlayRecord.DxScore,
            Fc = maiPlayRecord.ClearState switch
            {
                ClearState.Empty => "",
                ClearState.FullCombo => "fc",
                ClearState.FullComboPlus => "fcp",
                ClearState.AllPerfect => "ap",
                ClearState.AllPerfectPlus => "app",
                _ => ""
            },
            Fs = maiPlayRecord.SyncState switch
            {
                SyncState.Empty => "",
                SyncState.Sync => "sync",
                SyncState.FullSync => "fs",
                SyncState.FullSyncPlus => "fsp",
                SyncState.FullSyncDx => "fsd",
                SyncState.FullSyncDxPlus => "fsdp",
                _ => ""
            },
            LevelIndex = chart.LevelIndex,
            Title = chart.Song?.SongName ?? "",
            Type = chart.Song?.SongType switch
            {
                MaiSongType.Dx => "DX",
                MaiSongType.Std => "SD",
                _ => ""
            }
        };
    }
}