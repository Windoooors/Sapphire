using Microsoft.EntityFrameworkCore;
using Sapphire.Data.GroupMember.Database;
using Sapphire.Data.GroupMember.Dtos;
using Sapphire.Utils;

namespace Sapphire.Data.GroupMember;

public class GroupMemberDataManager
{
    public static GroupMemberDataManager? Instance;

    public GroupMemberDataManager()
    {
        Instance = this;

        Aliases = [];

        using var context = new GroupMemberDataContext();

        context.Database.Migrate();

        BakeIndexingData(context);
    }

    public Dictionary<string, (long groupId, long userId)[]> AliasToUserIds { get; } = [];
    public Dictionary<(long groupId, long userId), string[]> AliasSearchKeys { get; } = [];
    public Dictionary<int, GroupMemberAlias> AliasIdToAlias { get; } = [];
    public (long groupId, long userId)[] AliasedUserIds { get; private set; } = [];
    public Dictionary<(long groupId, long userId), GroupMemberAlias[]> UserIdToAliases { get; } = [];

    public GroupMemberAlias[] Aliases { get; private set; }

    private void BakeIndexingData(GroupMemberDataContext context)
    {
        Aliases = context.GroupMemberAliases.ToArray();

        AliasToUserIds.Clear();
        AliasSearchKeys.Clear();
        UserIdToAliases.Clear();
        AliasIdToAlias.Clear();

        foreach (var groupMemberAlias in Aliases)
        {
            AliasToUserIds.TryAdd(groupMemberAlias.Alias.ToLower(),
                Aliases.Where(x =>
                        x.Alias.ToLower() == groupMemberAlias.Alias.ToLower() && x.GroupId == groupMemberAlias.GroupId)
                    .Select(x =>
                        (x.GroupId, x.UserId)).ToArray());

            AliasSearchKeys.TryAdd((groupMemberAlias.GroupId, groupMemberAlias.UserId),
                Aliases.Where(x => x.UserId == groupMemberAlias.UserId && x.GroupId == groupMemberAlias.GroupId)
                    .Select(x => SearchHelper.NormalizeCjkString(x.Alias)).ToArray());

            UserIdToAliases.TryAdd((groupMemberAlias.GroupId, groupMemberAlias.UserId),
                Aliases.Where(x => x.UserId == groupMemberAlias.UserId && x.GroupId == groupMemberAlias.GroupId)
                    .ToArray());

            AliasIdToAlias.TryAdd(groupMemberAlias.MemberAliasId, groupMemberAlias);
        }

        AliasedUserIds = AliasSearchKeys.Keys.ToArray();
    }
    
    public async Task<bool> TryAddAlias(long userId, long groupId, string alias)
    {
        if (Aliases.AsQueryable().Any(x => x.Alias.ToLower() == alias.ToLower() && x.GroupId == groupId && x.UserId == userId))
            return false;

        await using var context = new GroupMemberDataContext();

        var user = await context.GroupMemberAliases.FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId);

        if (user == null)
            return false;

        await context.GroupMemberAliases.AddAsync(new GroupMemberAlias()
        {
            Alias = alias,
            UserId = userId,
            GroupId = groupId
        });

        await context.SaveChangesAsync();
        
        BakeIndexingData(context);

        return true;
    }
    
    public async Task<bool> TryRemoveAlias(int aliasId, long userId, bool forced = false)
    {
        await using var context = new GroupMemberDataContext();

        var alias = await context.GroupMemberAliases.FirstOrDefaultAsync(x => x.MemberAliasId == aliasId);

        if (alias == null)
            return false;

        if (!forced && alias.UserId != userId)
            return false;

        context.GroupMemberAliases.Remove(alias);

        await context.SaveChangesAsync();
        
        BakeIndexingData(context);

        return true;
    }
}