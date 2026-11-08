using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.GroupMember;
using Sapphire.Environments;
using Sapphire.ImageGeneration;

namespace Sapphire.Commands.GroupMemberCommands;

public class RapeCommand : GroupMemberCommandBase
{
    public RapeCommand()
    {
        CommandHeads = ["日", "透", "透群友"];
        DirectCommandHeads = ["日", "透", "透群友"];
        ActivationSettingsIdentifier = "rape.enabled";
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        if (context is not CqGroupMessagePostContext groupMessageContext)
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
            [
                await BuildMemberListString(userIds,
                    $"{SpEnvironmentHandler.Environment.Sapphire.CommandPrefixes[0]} group-member alias {userIds[0]}",
                    "以透列表中第一位群友",
                    groupMessageContext.GroupId)
            ]);
            return;
        }

        var userId = userIds[0];

        if (userId == groupMessageContext.UserId)
        {
            await SendMessage(context, ["哇 还有水仙"]);
            return;
        }

        if (userId == SpEnvironmentHandler.Environment.Sapphire.BotQqId)
        {
            await SendMessage(context, ["🥺我怕你把持不住"]);
            return;
        }

        if (!GroupMemberDataManager.Instance.UserPool.TryGetValue(groupMessageContext.GroupId, out var group) ||
            !group.Contains(userId))
        {
            await GroupMemberDataManager.Instance.AddUserToPool(groupMessageContext.UserId,
                groupMessageContext.GroupId);

            await SendMemberNotAvailableMessage(context);

            return;
        }

        if (!await SendMessage(groupMessageContext, userId)) await SendMemberNotAvailableMessage(context);
    }

    protected override async Task Process(CqMessagePostContext context)
    {
        if (context is not CqGroupMessagePostContext groupMessageContext)
        {
            await SendWrongContextMessage(context);
            return;
        }

        if (GroupMemberDataManager.Instance == null)
        {
            await SendUnknownError(context);
            return;
        }

        if (!GroupMemberDataManager.Instance.UserPool.TryGetValue(groupMessageContext.GroupId, out var group))
        {
            await GroupMemberDataManager.Instance.AddUserToPool(groupMessageContext.UserId,
                groupMessageContext.GroupId);

            await SendNoAvailableMemberMessage(context);

            return;
        }

        if (!group.Contains(groupMessageContext.UserId))
            await GroupMemberDataManager.Instance.AddUserToPool(groupMessageContext.UserId,
                groupMessageContext.GroupId);

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

            if (groupArr[randomResult] != groupMessageContext.UserId)
                break;
        }

        var userId = groupArr[randomResult];
        if (!await SendMessage(groupMessageContext, userId)) await Process(context);
    }

    private async Task<bool> SendMessage(CqGroupMessagePostContext groupMessageContext, long userId)
    {
        var nickname = await GetNickname(userId, groupMessageContext.GroupId, true);

        if (GroupMemberDataManager.Instance == null)
            return false;

        if (nickname == null)
        {
            await GroupMemberDataManager.Instance.RemoveUserFromPool(groupMessageContext.UserId,
                groupMessageContext.GroupId);

            return false;
        }

        await SendMessage(groupMessageContext, [
            new CqImageMsg("base64://" +
                           await ImageGeneratorFunctions.GetUserAvatarBase64Async(userId, 640)),
            $"您把群友 {nickname} ({userId}) 配了"
        ]);

        return true;
    }

    private async Task SendNoAvailableMemberMessage(CqMessagePostContext context)
    {
        await SendMessage(context, ["无可配群友"]);
    }

    private async Task SendMemberNotAvailableMessage(CqMessagePostContext context)
    {
        await SendMessage(context, ["群友不可配"]);
    }
}