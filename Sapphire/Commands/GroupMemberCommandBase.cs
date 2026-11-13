using System.Collections.Concurrent;
using System.Text;
using Sapphire.Commands.GroupMemberCommands;
using Sapphire.Data.GroupMember;

namespace Sapphire.Commands;

public abstract class GroupMemberCommandBase : CommandBase
{
    private static readonly ConcurrentDictionary<string, string> KnownNames = new(StringComparer.Ordinal);

    protected static GroupMemberDataManager? DataManager;

    public static void RememberName(string userId, string? displayName)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            KnownNames[userId] = displayName;
    }

    public static bool TryGetUsers(MessageContext context, string key, out string[]? userIds)
    {
        if (GroupMemberDataManager.Instance == null)
        {
            userIds = null;
            return false;
        }

        if (!context.IsGroup)
        {
            userIds = [];
            return true;
        }

        var result = MatchExact(key, context.ChatId);
        if (result.Count > 0)
        {
            userIds = result.Distinct().ToArray();
            return true;
        }

        var predicate = GroupMemberSearchCommand.CreateMemberAliasPredicate(key, context.ChatId);
        result.AddRange(GroupMemberDataManager.Instance.AliasedUserIds
            .Where(predicate)
            .Select(x => x.userId));

        if (result.Count > 0)
        {
            userIds = result.Distinct().ToArray();
            return true;
        }

        var byId = GroupMemberDataManager.Instance.FindUserIds(context.ChatId, key);
        if (byId.Count > 0)
        {
            userIds = byId.ToArray();
            return true;
        }

        userIds = [];
        return true;

        List<string> MatchExact(string searchKey, string groupId)
        {
            var matched = new List<string>();

            if (GroupMemberDataManager.Instance?.AliasToUserIds.TryGetValue(
                    searchKey.ToLower(), out var value) ?? false)
                matched.AddRange(value.Where(x => x.groupId == groupId)
                    .Select(x => x.userId));

            return matched.Distinct().ToList();
        }
    }

    public static Task<string?> GetNickname(string userId, string groupId, bool forceGroupNickname = false)
    {
        var groupName = GroupMemberDataManager.Instance?.GetDisplayName(groupId, userId);
        if (!string.IsNullOrWhiteSpace(groupName))
            return Task.FromResult<string?>(groupName);

        return KnownNames.TryGetValue(userId, out var name)
            ? Task.FromResult<string?>(name)
            : Task.FromResult<string?>(null);
    }

    public static Task<string?> GetNickname(string userId)
    {
        return KnownNames.TryGetValue(userId, out var name)
            ? Task.FromResult<string?>(name)
            : Task.FromResult<string?>(null);
    }

    protected static async Task SendMemberNotFoundError(MessageContext context)
    {
        await SendMessage(context, "未找到匹配的用户");
    }

    protected static async Task<string> BuildMemberListString(IEnumerable<string> users, string commandHint,
        string hint, string groupId)
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

    protected static async Task SendWrongContextMessage(MessageContext context)
    {
        await SendMessage(context, "该功能仅支持在群聊中使用");
    }
}
