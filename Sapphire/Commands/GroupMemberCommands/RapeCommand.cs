using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.GroupMember;
using Sapphire.Environments;
using Sapphire.ImageGeneration;
using SixLabors.ImageSharp;

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

        if (!TryGetUsers(context, targetString, out var userIds) || userIds == null || GroupMemberDataManager.Instance == null)
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
                await BuildMemberListString(userIds, $"s group-member alias {userIds[0]}", "查询列表中第一位用户的福",
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
        
        if (!GroupMemberDataManager.Instance.UserPool.TryGetValue(groupMessageContext.GroupId, out var group) || !group.Contains(userId))
        {
            await GroupMemberDataManager.Instance.AddUserToPool(groupMessageContext.UserId,
                groupMessageContext.GroupId);

            await SendMessage(context, ["群友不可配"]);
            
            return;
        }
        
        var nickname = await GetNickname(userId, groupMessageContext.GroupId, true);

        if (nickname == null)
        {
            await GroupMemberDataManager.Instance.RemoveUserFromPool(groupMessageContext.UserId,
                groupMessageContext.GroupId);

            await SendMessage(context, ["群友不可配"]);
        }

        await SendMessage(context, [
            new CqImageMsg("base64://" +
                           await ImageGeneratorFunctions.GetUserAvatarBase64Async(userId, 640)),
            $"您把群友 {nickname} ({userId}) 配了"
        ]);
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

            await SendMessage(context, ["无可配群友"]);
            
            return;
        }
        
        if (!group.Contains(groupMessageContext.UserId))
        {
            await GroupMemberDataManager.Instance.AddUserToPool(groupMessageContext.UserId,
                groupMessageContext.GroupId);
        }

        if (group.Count is 0 or 1)
        {
            await SendMessage(context, ["无可配群友"]);
            
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
        var nickname = await GetNickname(userId, groupMessageContext.GroupId, true);

        if (nickname == null)
        {
            await GroupMemberDataManager.Instance.RemoveUserFromPool(groupMessageContext.UserId,
                groupMessageContext.GroupId);

            await Process(context);
            return;
        }

        await SendMessage(context, [
            new CqImageMsg("base64://" +
                           await ImageGeneratorFunctions.GetUserAvatarBase64Async(userId, 640)),
            $"您把群友 {nickname} ({userId}) 配了"
        ]);
    }
}