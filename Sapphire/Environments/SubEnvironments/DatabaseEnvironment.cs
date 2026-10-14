namespace Sapphire.Environments.SubEnvironments;

public record DatabaseEnvironment
{
    public string SqlConnectionString { get; init; } = "";
}