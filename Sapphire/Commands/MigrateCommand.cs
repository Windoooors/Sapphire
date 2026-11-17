using Sapphire.Data.GroupMember;
using Sapphire.Data.Migration;
using Sapphire.Data.Settings;
using Sapphire.Environments;
using Sapphire.Utils;
using Serilog;

namespace Sapphire.Commands;

public class MigrateCommand : CommandBase
{
    public MigrateCommand()
    {
        CommandHeads = ["migrate"];
        DirectCommandHeads = ["migrate"];
        ActivationSettingsIdentifier = "migrate.enabled";

        SupportedFlags =
        [
            new FlagSpec
            {
                LongName = "--group",
                ShortName = "-g",
                TakesValue = true
            }
        ];
    }
    
    protected override async Task ProcessWithArguments(CommandContext commandContext, MessageContext context)
    {
        if (!MigrationServer.IsRunning)
        {
            await SendMessage(context, "迁移服务未启用");
            return;
        }

        var environment = SpEnvironmentHandler.Environment.Migration;

        if (context.IsGroup &&
            !environment.AllowAllUsers &&
            !IsBotAdministrator(context) &&
            !IsGroupAdministrator(context))
        {
            await SendUnpermittedError(context);
            return;
        }

        var qqNumberString = commandContext.Argument?.Trim();

        if (!long.TryParse(qqNumberString, out var qqNumber) || qqNumber <= 0)
        {
            await SendArgumentError(context);
            return;
        }

        long? legacyGroupId = null;

        if (context.IsGroup)
        {
            var legacyGroupString = commandContext.GetFlag("--group");

            if (string.IsNullOrWhiteSpace(legacyGroupString))
            {
                await SendArgumentError(context);
                return;
            }

            if (!long.TryParse(legacyGroupString, out var parsedGroupId) || parsedGroupId <= 0)
            {
                await SendArgumentError(context);
                return;
            }

            legacyGroupId = parsedGroupId;
        }

        var request = new MigrationRequest
        {
            RequestId = Guid.NewGuid().ToString("N"),
            QqNumber = qqNumber,
            CanonicalUserId = context.UserId,
            SceneUserId = context.SceneUserId,
            ChatId = context.ChatId,
            IsGroup = context.IsGroup,
            LegacyGroupId = legacyGroupId
        };

        await SendMessage(context, "正在尝试迁移，请稍等");

        var response = await MigrationServer.RequestAsync(request, TimeSpan.FromSeconds(90));

        if (response == null)
        {
            await SendMessage(context, "迁移服务未连接或响应超时");
            return;
        }

        if (!response.Success)
        {
            await SendMessage(context, $"迁移失败：{response.Message ?? "未知错误"}");
            return;
        }

        try
        {
            await UserMigrationStatusManager.MarkMigratedAsync(context.UserId, qqNumber);

            if (context.IsGroup)
                GroupMemberDataManager.Instance?.ReloadAndBake();

            SettingsDataManager.Instance?.Reload();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while marking migration status or reloading indexes");
        }

        if (context.IsGroup)
            await SendMessage(context,
                $"迁移完成：成绩 {response.Scores} 条\n别名 {response.Aliases} 条\n群成员 {response.Members} 条\n设置 {response.Settings} 条");
        else
            await SendMessage(context,
                $"迁移完成：成绩 {response.Scores} 条\n设置 {response.Settings} 条");
    }
}