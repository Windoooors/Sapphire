using Microsoft.EntityFrameworkCore;
using Sapphire.Commands;
using Sapphire.Data.Settings.Database;
using Sapphire.Data.Settings.Dtos;
using Sapphire.UserSettings;

namespace Sapphire.Data.Settings;

public class SettingsDataManager
{
    private readonly Dictionary<(bool, string, string?), bool> _settingValues;

    public SettingsDataManager()
    {
        using var context = new SettingsMetadataContext();

        context.Database.Migrate();

        _settingValues = context.SettingsItems.AsNoTracking()
            .ToDictionary(x => (x.IsGroup, x.ChatId, x.Identifier), x => x.Value);

        Instance = this;
    }

    public static SettingsDataManager? Instance { get; private set; }

    /// <summary>
    ///     迁移服务直接写库后，重新读取设置表。
    /// </summary>
    public void Reload()
    {
        using var context = new SettingsMetadataContext();

        var values = context.SettingsItems.AsNoTracking()
            .ToDictionary(x => (x.IsGroup, x.ChatId, x.Identifier), x => x.Value);

        _settingValues.Clear();

        foreach (var pair in values)
            _settingValues[pair.Key] = pair.Value;
    }

    public bool GetValue(MessageContext context, string identifier)
    {
        var defaultValue = SettingsItems.DefaultValues.GetValueOrDefault(identifier, true);
        return _settingValues.GetValueOrDefault((context.IsGroup, GetChatId(context), identifier), defaultValue);
    }

    public async Task SetValue(MessageContext context, string identifier, bool value)
    {
        var chatId = GetChatId(context);
        var key = (context.IsGroup, chatId, identifier);

        await using var dbContext = new SettingsMetadataContext();

        _settingValues[key] = value;

        var settingsItem = await dbContext.SettingsItems.FindAsync(context.IsGroup, chatId, identifier);
        if (settingsItem == null)
            await dbContext.SettingsItems.AddAsync(new SettingsStorageItemDto
            {
                Identifier = identifier,
                ChatId = chatId,
                IsGroup = context.IsGroup,
                Value = value
            });
        else
            settingsItem.Value = value;

        await dbContext.SaveChangesAsync();
    }

    private static string GetChatId(MessageContext context)
    {
        // 群设置按 group_openid；单聊设置按 canonical 用户 ID，迁移后才能跨场景生效。
        return context.IsGroup ? context.ChatId : context.UserId;
    }
}
