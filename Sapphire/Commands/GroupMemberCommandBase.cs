using System.Text;
using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Action;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Commands.GroupMemberCommands;
using Sapphire.Data.GroupMember;

namespace Sapphire.Commands;

public abstract class GroupMemberCommandBase : CommandBase
{
    protected static GroupMemberDataManager? DataManager;

    public static bool TryGetUsers(CqMessagePostContext context, string key, out long[]? userIds)
    {
        if (context is CqGroupMessagePostContext groupContext)
        {
            var result = MatchExact(key);
            if (result.Count > 0)
            {
                userIds = result.ToArray();
                return true;
            }
            
            var predicate = GroupMemberSearchCommand.CreateMemberAliasPredicate(key, groupContext.GroupId);
            result.AddRange(
                GroupMemberDataManager.Instance?.AliasedUserIds.Where(predicate).Select(x => x.userId) ?? []);
            
            if (result.Count > 0)
            {
                userIds = result.ToArray();
                return true;
            }
        }

        if (long.TryParse(key, out long id))
        {
            userIds = [id];
            return true;
        }

        userIds = null;
        return false;
        
        List<long> MatchExact(string searchKey)
        {
            var result = new List<long>();

            if (GroupMemberDataManager.Instance?.AliasToUserIds.TryGetValue(
                    searchKey.ToLower(), out var value) ?? false)
                result.AddRange(value.Where(x => x.groupId == groupContext.GroupId)
                    .Select(x => x.userId));

            return result.Distinct().ToList();
        }
    }

    public static async Task<string?> GetNickname(long id, long groupId)
    {
        if (Program.BotSessionHandler.Session == null)
            return null;

        var memberInformation = await Program.BotSessionHandler.Session.GetGroupMemberInformationAsync(groupId, id);

        if (memberInformation == null || memberInformation.Status == CqActionStatus.Failed) return null;

        var nickname = memberInformation.GroupNickname == ""
            ? memberInformation.Nickname
            : memberInformation.GroupNickname;
        return nickname;
    }

    public static async Task<string?> GetNickname(long id)
    {
        if (Program.BotSessionHandler.Session == null)
            return null;

        var memberInformation = await Program.BotSessionHandler.Session.GetStrangerInformationAsync(id);

        if (memberInformation == null || memberInformation.Status == CqActionStatus.Failed) return null;

        var nickname = memberInformation.Nickname;
        return nickname;
    }
    
    protected static async Task<string> BuildMemberListString(IEnumerable<long> users, string commandHint, string hint, long groupId)
    {
        var userSet = users.ToHashSet();

        var count = userSet.Count;

        if (count == 0) return "未找到符合条件的用户";

        if (count > 50) return "匹配的用户数量过多，请缩小查询范围";

        var stringBuilder = new StringBuilder("找到了如下用户：\n");

        foreach (var userId in userSet)
        {
            var nickName = await GetNickname(userId, groupId);

            if (nickName == null) continue;

            stringBuilder.AppendLine($"{nickName} ({userId})");
        }

        stringBuilder.AppendLine($"请发送指令 \"{commandHint}\" 以{hint}");

        return stringBuilder.ToString().Trim();
    }

    protected async Task SendWrongContextMessage(CqMessagePostContext context)
    {
        await SendMessage(context, ["该功能仅支持在群聊中使用"]);
    }
}