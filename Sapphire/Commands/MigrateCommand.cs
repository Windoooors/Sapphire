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

    protected override async Task Process(MessageContext context)
    {
        await SendMessage(context,
            "用法：\n群聊：migrate <QQ号> --group <旧群号>\n私聊：migrate <QQ号>（只迁移成绩和私聊设置）");
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, MessageContext context)
    {
        if (!MigrationServer.IsRunning)
        {
            await SendMessage(context, "迁移服务未启用，请先在主 bot 配置中开启 [Migration]");
            return;
        }

        var environment = SpEnvironmentHandler.Environment.Migration;

        // 私聊只影响当前 canonical 用户的成绩/私聊设置，允许所有用户自行迁移；
        // 群聊会导入群友别名/群成员/群设置，仍要求管理员权限。
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
                await SendMessage(context, "群聊迁移必须提供 --group <旧群号>");
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

        await SendMessage(context, "正在从旧库迁移，请稍候…");

        var response = await MigrationServer.RequestAsync(request, TimeSpan.FromSeconds(90));

        if (response == null)
        {
            await SendMessage(context, "迁移服务未连接或响应超时，请确认 Sapphire.Migration 已启动");
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
                $"迁移完成：成绩 {response.Scores} 条，别名 {response.Aliases} 条，群成员 {response.Members} 条，设置 {response.Settings} 条");
        else
            await SendMessage(context,
                $"迁移完成：成绩 {response.Scores} 条，私聊设置 {response.Settings} 条");
    }
}
