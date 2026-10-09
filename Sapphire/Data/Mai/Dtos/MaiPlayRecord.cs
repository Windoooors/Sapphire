using System.Runtime.InteropServices;

namespace Sapphire.Data.Mai.Dtos;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MaiPlayRecord
{
    public uint Score;
    public ClearState ClearState;
    public SyncState SyncState;
    public ushort PlayCount;
    public ushort DxScore;
}

public enum ClearState : byte
{
    NotPlayed,
    AllPerfectPlus,
    AllPerfect,
    FullComboPlus,
    FullCombo,
    Empty
}

public enum SyncState : byte
{
    FullSyncDxPlus,
    FullSyncDx,
    FullSyncPlus,
    FullSync,
    Sync,
    Empty
}