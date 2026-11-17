using Sapphire.Data.GroupMember;
using Sapphire.Environments;
using Sapphire.ImageGeneration;
using Sapphire.Messages;
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
    
    protected override async Task ProcessDirectly(MessageContext context)
    {
        if (!(context.Content.StartsWith("日") || context.Content.StartsWith("透") || context.Content.StartsWith("操") ||
            context.Content.StartsWith("干")))
        {
            return;
        }
        
        var userReference = context.Content.Substring(1, context.Content.Length - 1);
        if (userReference.StartsWith(" "))
        {
            return;
        }
        
        if (!TryGetUsers(context, userReference, out var userIds) || userIds == null ||
            GroupMemberDataManager.Instance == null || userIds.Length is > 1 or 0)
        {
            return;
        }

        await ProcessWithArguments(new CommandContext()
        {
            Argument = userReference,
            Flags = []
        }, context);
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, MessageContext context)
    {
        if (!context.IsGroup)
        {
            await SendWrongContextMessage(context);
            return;
        }

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
        if (GroupMemberDataManager.Instance == null)
            return false;

        var sceneUserId = GroupMemberDataManager.Instance.GetSceneUserId(context.ChatId, userId);
        var nickname = await GetNickname(userId, context.ChatId, true);

        if (nickname == null)
        {
            var inGroup = await IsUserInGroupAsync(context, userId, sceneUserId);

            if (inGroup == false)
            {
                await GroupMemberDataManager.Instance.RemoveUserFromPool(userId, context.ChatId);
                return false;
            }

            nickname = await GetNickname(userId, context.ChatId, true) ?? userId;
        }

        sceneUserId ??= userId;

        var avatarBase64 = await ImageGeneratorFunctions.GetUserAvatarBase64Async(sceneUserId, 640);

        await SendMessage(context, [new ImageMessage(avatarBase64), $"您把群友 {nickname} ({userId}) 配了"]);

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