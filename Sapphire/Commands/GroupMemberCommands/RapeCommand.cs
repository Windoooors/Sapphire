using Sapphire.Data.GroupMember;
using Sapphire.Environments;
using Sapphire.ImageGeneration;
using Sapphire.Utils;

namespace Sapphire.Commands.GroupMemberCommands;

public class RapeCommand : GroupMemberCommandBase
{
    public RapeCommand()
    {
        CommandHeads = ["日", "透", "透群友"];
        DirectCommandHeads = ["日", "透", "透群友"];
        ActivationSettingsIdentifier = "rape.enabled";
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, MessageContext context)
    {
        if (!context.IsGroup)
        {
            await SendWrongContextMessage(context);
            return;
        }

        if (!await EnsureMigratedAsync(context))
            return;

        var targetString = commandContext.Argument;

        if (string.IsNullOrEmpty(targetString))
        {
            await SendArgumentError(context);
            return;
        }

        if (!TryGetUsers(context, targetString, out var userIds) || userIds == null ||
            GroupMemberDataManager.Instance == null)
        {
            await SendUnknownError(context);
            return;
        }

        if (userIds.Length == 0)
        {
            await SendMemberNotFoundError(context);
            return;
        }

        if (userIds.Length > 1)
        {
            await SendMessage(context,
                await BuildMemberListString(userIds,
                    $"{SpEnvironmentHandler.Environment.Sapphire.CommandPrefixes[0]} group-member alias {userIds[0]}",
                    "以透列表中第一位群友",
                    context.ChatId));
            return;
        }

        var userId = userIds[0];

        if (userId == context.UserId)
        {
            await SendMessage(context, "哇 还有水仙");
            return;
        }

        if (userId == BotClient.CurrentUserId)
        {
            await SendMessage(context, "🥺我怕你把持不住");
            return;
        }

        if (!GroupMemberDataManager.Instance.UserPool.TryGetValue(context.ChatId, out var group) ||
            !group.Contains(userId))
        {
            await GroupMemberDataManager.Instance.AddUserToPool(context.UserId, context.SceneUserId,
                context.ChatId);

            await SendMemberNotAvailableMessage(context);

            return;
        }

        if (!await SendPairingMessage(context, userId)) await SendMemberNotAvailableMessage(context);
    }

    protected override async Task Process(MessageContext context)
    {
        if (!context.IsGroup)
        {
            await SendWrongContextMessage(context);
            return;
        }

        if (GroupMemberDataManager.Instance == null)
        {
            await SendUnknownError(context);
            return;
        }

        if (!await EnsureMigratedAsync(context))
            return;

        if (!GroupMemberDataManager.Instance.UserPool.TryGetValue(context.ChatId, out var group))
        {
            await GroupMemberDataManager.Instance.AddUserToPool(context.UserId, context.SceneUserId,
                context.ChatId);

            await SendNoAvailableMemberMessage(context);

            return;
        }

        if (!group.Contains(context.UserId))
            await GroupMemberDataManager.Instance.AddUserToPool(context.UserId, context.SceneUserId,
                context.ChatId);

        if (group.Count is 0 or 1)
        {
            await SendNoAvailableMemberMessage(context);

            return;
        }

        var random = new Random();
        int randomResult;
        var groupArr = group.ToArray();
        while (true)
        {
            randomResult = random.Next(0, group.Count);

            if (groupArr[randomResult] != context.UserId)
                break;
        }

        var userId = groupArr[randomResult];
        if (!await SendPairingMessage(context, userId)) await Process(context);
    }

    private async Task<bool> SendPairingMessage(MessageContext context, string userId)
    {
        var nickname = await GetNickname(userId, context.ChatId, true);

        if (GroupMemberDataManager.Instance == null)
            return false;

        if (nickname == null)
        {
            await GroupMemberDataManager.Instance.RemoveUserFromPool(context.UserId,
                context.ChatId);

            return false;
        }

        var sceneUserId = GroupMemberDataManager.Instance.GetSceneUserId(context.ChatId, userId) ?? userId;

        await SendImage(context,
            await ImageGeneratorFunctions.GetUserAvatarBase64Async(sceneUserId, 640));
        await SendMessage(context, $"您把群友 {nickname} ({userId}) 配了");

        return true;
    }

    private async Task SendNoAvailableMemberMessage(MessageContext context)
    {
        await SendMessage(context, "无可配群友");
    }

    private async Task SendMemberNotAvailableMessage(MessageContext context)
    {
        await SendMessage(context, "群友不可配");
    }
}
