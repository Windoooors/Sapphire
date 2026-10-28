using System.Text;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.GroupMember;
using Sapphire.Utils;

namespace Sapphire.Commands.GroupMemberCommands;

public class GroupMemberSearchCommand : GroupMemberCommandBase
{
    public GroupMemberSearchCommand()
    {
        CommandHeads = ["search"];
        DirectCommandHeads = ["member-search", "msearch"];
        
        ActivationSettingsIdentifier = "group_member_search.enabled";
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
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

        if (string.IsNullOrEmpty(commandContext.Argument))
        {
            await SendMessage(context, ["请指定查询范围"]);
            return;
        }

        var aliasPredicate = CreateMemberAliasPredicate(commandContext.Argument, groupMessageContext.GroupId);

        var searchResult = GroupMemberDataManager.Instance.AliasedUserIds.Where(aliasPredicate);

        await SendMessage(context, [await BuildSearchResultString(searchResult)]);
    }

    public static Func<(long groupId, long userId), bool> CreateMemberAliasPredicate(string? value, long groupId)
    {
        if (value?.Length == 0) return _ => true;

        var inputKey = SearchHelper.NormalizeCjkString(value ?? "");

        return x => (GroupMemberDataManager.Instance?.AliasSearchKeys[x]
            .Any(y => y.Contains(inputKey.ToLower())) ?? false) && x.groupId == groupId;
    }

    private static async Task<string> BuildSearchResultString(IEnumerable<(long groupId, long userId)> userIds)
    {
        var idSet = userIds.ToHashSet();

        var count = idSet.Count;

        if (count == 0) return "未找到符合条件的用户";

        if (count > 50) return "结果过多，请缩小查询范围";

        var stringBuilder = new StringBuilder("找到了如下用户：\n");

        foreach (var valueTuple in idSet)
            stringBuilder.AppendLine($"{await GetNickname(valueTuple.userId, valueTuple.groupId)} ({valueTuple.userId})");

        return stringBuilder.ToString().Trim();
    }
}