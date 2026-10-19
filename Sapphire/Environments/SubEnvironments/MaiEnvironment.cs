namespace Sapphire.Environments.SubEnvironments;

public record MaiEnvironment
{
    public int[] CnLastestVersionIndexes { get; init; } = [24];
    public int[] JpLastestVersionIndexes { get; init; } = [26, 27];
}