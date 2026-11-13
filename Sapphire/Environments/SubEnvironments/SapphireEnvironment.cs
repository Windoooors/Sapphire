namespace Sapphire.Environments.SubEnvironments;

public record SapphireEnvironment
{
    public string BotName { get; init; } = "Sapphire Bot";

    // 旧 NapCat 配置字段，保留以便旧 config.toml 仍能反序列化；新代码不再使用。
    public long AdministratorQqId { get; init; }
    public long BotQqId { get; init; }

    // 官方 QQ Bot 的用户标识是 OpenID 字符串；可填 canonical OpenID（union_openid）。
    public string[] AdministratorOpenIds { get; init; } = [];

    public string[] CommandPrefixes { get; init; } = ["s", "sapphire", "saf"];
    public TimeOnly UtcDayTickTime { get; init; } = TimeOnly.FromDateTime(DateTime.Today);
}