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