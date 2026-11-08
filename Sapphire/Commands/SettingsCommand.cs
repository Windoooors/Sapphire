using System.Text;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.Settings;
using Sapphire.UserSettings;

namespace Sapphire.Commands;

public class SettingsCommand : CommandBase
{
    public SettingsCommand()
    {
        CommandHeads = ["settings"];
        DirectCommandHeads = ["settings"];
    }

    protected override async Task Process(CqMessagePostContext context)
    {
        if (SettingsDataManager.Instance == null)
        {
            await SendUnknownError(context);
            return;
        }

        var stringBuilder = new StringBuilder("设置项：\n");

        foreach (var settingsCategory in SettingsItems.Categories)
        {
            stringBuilder.AppendLine(settingsCategory.Name);
            foreach (var settingsItem in settingsCategory.Items)
            {
                stringBuilder.Append("    ");
                stringBuilder.Append(settingsItem.DisplayName);
                stringBuilder.Append("[");
                stringBuilder.Append(settingsItem.Identifier);
                stringBuilder.Append("] ");
                stringBuilder.AppendLine(SettingsDataManager.Instance.GetValue(context, settingsItem.Identifier)
                    ? "True"
                    : "False");
            }
        }

        await SendMessage(context, [stringBuilder.ToString()]);
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        if (context is CqGroupMessagePostContext groupMessageContext)
            if (!IsBotAdministrator(context.UserId) && !await IsGroupAdministrator(context.UserId, groupMessageContext))
            {
                await SendUnpermittedError(context);
                return;
            }

        if (!SettingsItems.DefaultValues.ContainsKey(commandContext.Argument ?? ""))
        {
            await SendMessage(context, ["未找到设置项"]);
            return;
        }

        if (SettingsDataManager.Instance == null)
        {
            await SendUnknownError(context);
            return;
        }

        var value = SettingsDataManager.Instance.GetValue(context, commandContext.Argument ?? "");
        await SettingsDataManager.Instance.SetValue(context, commandContext.Argument ?? "", value = !value);

        await SendMessage(context, [$"{value}"]);
    }
}