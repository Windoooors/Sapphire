namespace Sapphire.Environments.SubEnvironments;

public record NapCatEnvironment
{
    public string Ip { get; init; } = "127.0.0.1";
    public int Port { get; init; }
    public string Token { get; init; } = "";
    public int HeartBeatInterval { get; init; }

    public long BotQqId { get; init; }
}