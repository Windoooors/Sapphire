namespace Sapphire.UserSettings;

public static class SettingsItems
{
    public static readonly SettingsCategory[] Categories =
    [
        new SettingsCategory()
        {
            Name = "舞萌歌曲搜索",
            Items =
            [
                new SettingsItem()
                {
                    DisplayName = "启用",
                    Identifier = "mai_search.enabled",
                }
            ]
        },
        new SettingsCategory()
        {
            Name = "Ping",
            Items =
            [
                new SettingsItem()
                {
                    DisplayName = "启用",
                    Identifier = "ping.enabled",
                }
            ]
        },
        new SettingsCategory()
        {
            Name = "杂项",
            Items =
            [
                new SettingsItem()
                {
                    DisplayName = "简介指令",
                    Identifier = "lite_commands.enabled",
                }
            ]
        }
    ];

    public static readonly Dictionary<string, bool> DefaultValues = new()
    {
        { "mai_search.enabled", true },
        { "ping.enabled", false },
        { "lite_commands.enabled", true }
    };
}

public class SettingsCategory
{
    public required string Name { get; init; }
    public required SettingsItem[] Items { get; init; }
}

public class SettingsItem
{
    public required string DisplayName { get; init; }
    public required string Identifier { get; init; }
}