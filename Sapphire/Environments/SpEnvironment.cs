using Sapphire.Environments.SubEnvironments;

namespace Sapphire.Environments;

public record SpEnvironment
{
    public required NapCatEnvironment NapCat { get; init; } = new();
    public required DatabaseEnvironment Database { get; init; } = new();
    public required DivingFishEnvironment DivingFish { get; init; } = new();
    public required MaiEnvironment Mai { get; init; } = new();
    public required WahlapConnectiveKitsEnvironment WahlapConnectiveKits { get; init; } = new();
    public required ImageGenerationEnvironment ImageGeneration { get; init; } = new();
    public required SapphireEnvironment Sapphire { get; init; } = new();
}