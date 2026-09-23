using EleCho.GoCqHttpSdk.Post;
using Microsoft.EntityFrameworkCore;
using Sapphire.Data.Settings.Database;
using Sapphire.Data.Settings.Dtos;
using Sapphire.UserSettings;

namespace Sapphire.Data.Settings;

public class SettingsDataManager
{
    public static SettingsDataManager? Instance { get; private set; }
    
    private readonly Dictionary<(bool, long, string?), bool> _settingValues;
    
    public SettingsDataManager()
    {
        using var context = new SettingsMetadataContext();

        context.Database.Migrate();
        
        _settingValues = context.SettingsItems.AsNoTracking().ToDictionary(x => (x.IsGroup, x.ChatId, x.Identifier), x => x.Value);
        
        Instance = this;
    }

    public bool GetValue(CqMessagePostContext postContext, string identifier)
    {
        var defaultValue = SettingsItems.DefaultValues.GetValueOrDefault(identifier, true);
        var value = defaultValue;
        
        if (postContext is CqGroupMessagePostContext groupMessagePostContext)
            value = _settingValues.GetValueOrDefault((true, groupMessagePostContext.GroupId, identifier), defaultValue);
        if (postContext is CqPrivateMessagePostContext privateMessagePostContext)
            value = _settingValues.GetValueOrDefault((false, privateMessagePostContext.Sender.UserId, identifier), defaultValue);

        return value;
    }

    public async Task SetValue(CqMessagePostContext postContext, string identifier, bool value)
    {
        await using var context = new SettingsMetadataContext();
        if (postContext is CqGroupMessagePostContext groupMessagePostContext)
        {
            if (_settingValues.ContainsKey((true, groupMessagePostContext.GroupId, identifier)))
            {
                _settingValues[(true, groupMessagePostContext.GroupId, identifier)] = value;
                
                var settingsItem = await context.SettingsItems.FindAsync(true, groupMessagePostContext.GroupId, identifier);
                if (settingsItem != null)
                    settingsItem.Value = value;
                else
                    await context.SettingsItems.AddAsync(new SettingsStorageItemDto
                    {
                        Identifier = identifier,
                        ChatId = groupMessagePostContext.GroupId,
                        IsGroup = true
                    });
            }
            else
            {
                _settingValues.Add((true, groupMessagePostContext.GroupId, identifier), value);
                await context.SettingsItems.AddAsync(new SettingsStorageItemDto
                {
                    Identifier = identifier,
                    ChatId = groupMessagePostContext.GroupId,
                    IsGroup = true
                });
            }
        }
        
        if (postContext is CqPrivateMessagePostContext privateMessagePostContext)
        {
            if (_settingValues.ContainsKey((false, privateMessagePostContext.Sender.UserId, identifier)))
            {
                _settingValues[(false, privateMessagePostContext.Sender.UserId, identifier)] = value;
                
                var settingsItem = await context.SettingsItems.FindAsync(false, privateMessagePostContext.Sender.UserId, identifier);
                if (settingsItem != null)
                    settingsItem.Value = value;
                else
                    await context.SettingsItems.AddAsync(new SettingsStorageItemDto
                    {
                        Identifier = identifier,
                        ChatId = privateMessagePostContext.Sender.UserId,
                        IsGroup = false
                    });
            }
            else
            {
                _settingValues.Add((false, privateMessagePostContext.Sender.UserId, identifier), value);
                await context.SettingsItems.AddAsync(new SettingsStorageItemDto
                {
                    Identifier = identifier,
                    ChatId = privateMessagePostContext.Sender.UserId,
                    IsGroup = false
                });
            }
        }
        
        await context.SaveChangesAsync();
    }
}