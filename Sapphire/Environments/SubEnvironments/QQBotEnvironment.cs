namespace Sapphire.Environments.SubEnvironments;

public record QQBotEnvironment
{
    public int AppId { get; init; }

    public string AppSecret { get; init; } = "";
}
