using Sapphire.Environments.SubEnvironments;

namespace Sapphire.Environments;

public record SpEnvironment
{
    public required NapCatEnvironment NapCat { get; init; } = new ();
    public required DatabaseEnvironment Database { get; init; } = new ();
    public required DivingFishEnvironment DivingFish { get; init; } = new ();
}