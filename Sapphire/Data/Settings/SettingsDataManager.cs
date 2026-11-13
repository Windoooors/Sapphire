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

    public bool GetValue(MessageContext context, string identifier)
    {
        var defaultValue = SettingsItems.DefaultValues.GetValueOrDefault(identifier, true);
        return _settingValues.GetValueOrDefault((context.IsGroup, context.ChatId, identifier), defaultValue);
    }

    public async Task SetValue(MessageContext context, string identifier, bool value)
    {
        var key = (context.IsGroup, context.ChatId, identifier);

        await using var context_ = new SettingsMetadataContext();

        _settingValues[key] = value;

        var settingsItem = await context_.SettingsItems.FindAsync(context.IsGroup, context.ChatId, identifier);
        if (settingsItem == null)
            await context_.SettingsItems.AddAsync(new SettingsStorageItemDto
            {
                Identifier = identifier,
                ChatId = context.ChatId,
                IsGroup = context.IsGroup,
                Value = value
            });
        else
            settingsItem.Value = value;

        await context_.SaveChangesAsync();
    }
}
