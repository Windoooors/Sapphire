namespace Sapphire.Environments.SubEnvironments;

public record SapphireEnvironment
{
    public string BotName { get; init; } = "Sapphire Bot";
    public long AdministratorQqId { get; init; }
    public long BotQqId { get; init; }
    public string[] CommandPrefixes { get; init; } = ["s", "sapphire", "saf"];
    public TimeOnly UtcDayTickTime { get; init; } = TimeOnly.FromDateTime(DateTime.Today);
}