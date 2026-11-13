using Tomlyn;

namespace Sapphire.Migration;

public sealed record MigrationConfig
{
    public required DatabaseConfig Database { get; init; }
    public required DatabaseConfig LegacyDatabase { get; init; }
    public required BotConfig Bot { get; init; }
}

public sealed record DatabaseConfig
{
    public string SqlConnectionString { get; init; } = "";
}

public sealed record BotConfig
{
    public string WebSocketUrl { get; init; } = "ws://127.0.0.1:5201/migration/";
    public string Token { get; init; } = "";
}

public static class MigrationConfigLoader
{
    public static MigrationConfig? Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"找不到配置文件：{path}");
            Console.Error.WriteLine("请把 config.example.toml 复制为 config.toml 并填写两个数据库连接串。");
            return null;
        }

        if (!TomlSerializer.TryDeserialize<MigrationConfig>(File.ReadAllText(path), out var config))
        {
            Console.Error.WriteLine($"配置文件解析失败：{path}");
            return null;
        }

        if (string.IsNullOrWhiteSpace(config.Database.SqlConnectionString) ||
            string.IsNullOrWhiteSpace(config.LegacyDatabase.SqlConnectionString))
        {
            Console.Error.WriteLine("Database.SqlConnectionString 或 LegacyDatabase.SqlConnectionString 为空。");
            return null;
        }

        if (string.IsNullOrWhiteSpace(config.Bot.WebSocketUrl))
        {
            Console.Error.WriteLine("Bot.WebSocketUrl 为空。");
            return null;
        }

        return config;
    }
}
