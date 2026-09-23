using Sapphire.Environments.SubEnvironments;

namespace Sapphire.Environments;

public record SpEnvironment
{
    public required NapCatEnvironment NapCat { get; init; }
    public required DatabaseEnvironment Database { get; init; }
}