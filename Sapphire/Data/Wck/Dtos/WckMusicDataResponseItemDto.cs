using System.Text.Json.Serialization;
using Sapphire.Data.Mai.Dtos;

namespace Sapphire.Data.Wck.Dtos;

public record WckMusicDataResponseItemDto
{
    [JsonPropertyName("Id")] public int Id { get; set; }
    [JsonPropertyName("Level")] public int Level { get; set; }
    [JsonPropertyName("Achievement")] public int Achievement { get; set; }
    [JsonPropertyName("DxScore")] public ushort DxScore { get; set; }
    [JsonPropertyName("ComboStatus")] public int ComboStatus { get; set; }
    [JsonPropertyName("SyncStatus")] public int SyncStatus { get; set; }
    [JsonPropertyName("PlayCount")] public ushort PlayCount { get; set; }

    public MaiPlayRecord ToMaiPlayRecord()
    {
        var result = new MaiPlayRecord
        {
            ClearState = ComboStatus switch
            {
                0 => ClearState.Empty,
                1 => ClearState.FullCombo,
                2 => ClearState.FullComboPlus,
                3 => ClearState.AllPerfect,
                4 => ClearState.AllPerfectPlus,
                _ => ClearState.NotPlayed
            },
            SyncState = SyncStatus switch
            {
                0 => SyncState.Empty,
                5 => SyncState.Sync,
                1 => SyncState.FullSync,
                2 => SyncState.FullSyncPlus,
                3 => SyncState.FullSyncDx,
                4 => SyncState.FullSyncDxPlus,
                _ => SyncState.Empty
            },
            DxScore = DxScore,
            PlayCount = PlayCount,
            Score = (uint)Achievement
        };

        return result;
    }
}