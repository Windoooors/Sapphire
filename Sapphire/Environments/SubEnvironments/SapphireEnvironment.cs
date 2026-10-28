namespace Sapphire.Environments.SubEnvironments;

public record SapphireEnvironment
{
    public string BotName { get; init; } = "Sapphire Bot";
    public long AdministratorQqId { get; init; }
    public long BotQqId { get; init; }
}