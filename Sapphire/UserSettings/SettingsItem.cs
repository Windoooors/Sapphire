namespace Sapphire.UserSettings;

public static class SettingsItems
{
    public static readonly SettingsCategory[] Categories =
    [
        new()
        {
            Name = "舞萌歌曲搜索",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "mai_search.enabled"
                }
            ]
        },
        new()
        {
            Name = "舞萌别名查询",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "mai_alias.enabled"
                }
            ]
        },
        new()
        {
            Name = "舞萌歌曲信息查询",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "mai_info.enabled"
                }
            ]
        },
        new()
        {
            Name = "舞萌 Top 50 查询",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "mai_b50.enabled"
                }
            ]
        },
        new()
        {
            Name = "舞萌绑定",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "mai_bind.enabled"
                }
            ]
        },
        new()
        {
            Name = "舞萌分数更新",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "mai_update.enabled"
                }
            ]
        },
        new()
        {
            Name = "舞萌分数导出",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "mai_export.enabled"
                }
            ]
        },
        new()
        {
            Name = "舞萌谱面容错查询",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "mai_tolerance.enabled"
                }
            ]
        },
        new()
        {
            Name = "华立账户登录",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "mai_wck_login.enabled"
                }
            ]
        },
        new()
        {
            Name = "华立账户登出",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "mai_wck_logout.enabled"
                }
            ]
        },
        new()
        {
            Name = "群友别名",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "group_member_alias.enabled"
                }
            ]
        },
        new()
        {
            Name = "群友搜索",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "group_member_search.enabled"
                }
            ]
        },
        new()
        {
            Name = "娶群友",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "marry.enabled"
                }
            ]
        },
        new()
        {
            Name = "透群友",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "rape.enabled"
                }
            ]
        },
        new()
        {
            Name = "Ping",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "ping.enabled"
                }
            ]
        },
        new()
        {
            Name = "计基助手",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "ics.enabled"
                }
            ]
        },
        new()
        {
            Name = "Gay 指数",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "启用",
                    Identifier = "gay_factor.enabled"
                }
            ]
        },
        new()
        {
            Name = "杂项",
            Items =
            [
                new SettingsItem
                {
                    DisplayName = "简介指令",
                    Identifier = "lite_commands.enabled"
                }
            ]
        }
    ];

    public static readonly Dictionary<string, bool> DefaultValues = new()
    {
        { "mai_search.enabled", true },
        { "ping.enabled", true },
        { "lite_commands.enabled", true },
        { "mai_info.enabled", true },
        { "mai_alias.enabled", true },
        { "mai_b50.enabled", true },
        { "mai_bind.enabled", true },
        { "mai_update.enabled", true },
        { "mai_wck_login.enabled", true },
        { "mai_wck_logout.enabled", true },
        { "mai_export.enabled", true },
        { "group_member_search.enabled", true },
        { "ics.enabled", true },
        { "marry.enabled", true },
        { "group_member_alias.enabled", true },
        { "rape.enabled", true },
        { "gay_factor.enabled", true }
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