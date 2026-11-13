using Microsoft.EntityFrameworkCore;
using Sapphire.Data.GroupMember.Database;
using Sapphire.Data.GroupMember.Dtos;
using Sapphire.Environments;
using Sapphire.Utils;

namespace Sapphire.Data.GroupMember;

public class GroupMemberDataManager
{
    public static GroupMemberDataManager? Instance;

    public GroupMemberDataManager()
    {
        Instance = this;

        using var context = new GroupMemberDataContext();

        context.Database.Migrate();

        AliasList = context.GroupMemberAliases.ToList();
        var userPool = context.GroupMembers.ToList();
        var groupResult = userPool.GroupBy(x => x.GroupId);
        foreach (var group in groupResult) UserPool.Add(group.Key, group.Select(x => x.UserId).ToHashSet());

        BakeIndexingData();
    }

    public Dictionary<string, (long groupId, long userId)[]> AliasToUserIds { get; } = [];
    public Dictionary<(long groupId, long userId), string[]> AliasSearchKeys { get; } = [];
    public Dictionary<int, GroupMemberAliasDto> AliasIdToAlias { get; } = [];
    public (long groupId, long userId)[] AliasedUserIds { get; private set; } = [];
    public Dictionary<(long groupId, long userId), GroupMemberAliasDto[]> UserIdToAliases { get; } = [];

    public List<GroupMemberAliasDto> AliasList { get; }

    public Dictionary<long, HashSet<long>> UserPool { get; } = [];

    public List<GroupMemberMarriageDto> GetMarriages()
    {
        using var context = new GroupMemberDataContext();

        var dayStart = GetCurrentDayStart();

        context.GroupMemberMarriages.Where(x => x.MarriedAt < dayStart).ExecuteDelete();

        return context.GroupMemberMarriages.AsNoTracking()
            .Where(x => x.MarriedAt >= dayStart)
            .ToList();
    }

    public async Task AddMarriages(long groupId, IEnumerable<(long UserId, long PartnerId)> marriages,
        DateTime marriedAt)
    {
        await using var context = new GroupMemberDataContext();

        foreach (var marriage in marriages)
            context.GroupMemberMarriages.Add(new GroupMemberMarriageDto
            {
                GroupId = groupId,
                UserId = marriage.UserId,
                PartnerId = marriage.PartnerId,
                MarriedAt = marriedAt
            });

        await context.SaveChangesAsync();
    }

    public async Task RemoveMarriage(long groupId, long userId, long partnerId)
    {
        await using var context = new GroupMemberDataContext();

        var marriages = await context.GroupMemberMarriages
            .Where(x =>
                x.GroupId == groupId &&
                ((x.UserId == userId && x.PartnerId == partnerId) ||
                 (x.UserId == partnerId && x.PartnerId == userId)))
            .ToListAsync();

        if (marriages.Count == 0)
            return;

        context.GroupMemberMarriages.RemoveRange(marriages);

        await context.SaveChangesAsync();
    }

    public bool IsMarriageExpired(DateTime marriedAt)
    {
        return marriedAt < GetCurrentDayStart();
    }

    private static DateTime GetCurrentDayStart()
    {
        var now = DateTime.UtcNow;
        var dayStart = now.Date.Add(SpEnvironmentHandler.Environment.Sapphire.UtcDayTickTime.ToTimeSpan());

        return now < dayStart ? dayStart.AddDays(-1) : dayStart;
    }

    public async Task RemoveUserFromPool(long userId, long groupId)
    {
        if (!UserPool.TryGetValue(groupId, out var group)) return;

        if (!group.Contains(userId)) return;

        group.Remove(userId);

        await using (var context = new GroupMemberDataContext())
        {
            var findResult =
                await context.GroupMembers.FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId);

            if (findResult == null)
                return;

            context.GroupMembers.Remove(findResult);

            await context.SaveChangesAsync();
        }
    }

    public async Task AddUserToPool(long userId, long groupId)
    {
        if (!UserPool.TryGetValue(groupId, out var group))
        {
            UserPool.Add(groupId, [userId]);

            await using (var context = new GroupMemberDataContext())
            {
                context.GroupMembers.Add(new GroupMemberDto
                {
                    GroupId = groupId,
                    UserId = userId
                });

                await context.SaveChangesAsync();
            }

            return;
        }

        if (group.Contains(userId)) return;

        group.Add(userId);

        await using (var context = new GroupMemberDataContext())
        {
            context.GroupMembers.Add(new GroupMemberDto
            {
                GroupId = groupId,
                UserId = userId
            });

            await context.SaveChangesAsync();
        }
    }

    private void BakeIndexingData()
    {
        Instance = null;

        AliasToUserIds.Clear();
        AliasSearchKeys.Clear();
        UserIdToAliases.Clear();
        AliasIdToAlias.Clear();

        foreach (var groupMemberAlias in AliasList)
        {
            AliasToUserIds.TryAdd(groupMemberAlias.Alias.ToLower(),
                AliasList.Where(x =>
                        x.Alias.ToLower() == groupMemberAlias.Alias.ToLower())
                    .Select(x =>
                        (x.GroupId, x.UserId)).ToArray());

            AliasSearchKeys.TryAdd((groupMemberAlias.GroupId, groupMemberAlias.UserId),
                AliasList.Where(x => x.UserId == groupMemberAlias.UserId && x.GroupId == groupMemberAlias.GroupId)
                    .Select(x => SearchHelper.NormalizeCjkString(x.Alias)).ToArray());

            UserIdToAliases.TryAdd((groupMemberAlias.GroupId, groupMemberAlias.UserId),
                AliasList.Where(x => x.UserId == groupMemberAlias.UserId && x.GroupId == groupMemberAlias.GroupId)
                    .ToArray());

            AliasIdToAlias.TryAdd(groupMemberAlias.MemberAliasId, groupMemberAlias);
        }

        AliasedUserIds = AliasSearchKeys.Keys.ToArray();

        Instance = this;
    }

    public async Task<bool> TryAddAlias(long userId, long groupId, string alias)
    {
        if (AliasList.AsQueryable()
            .Any(x => x.Alias.ToLower() == alias.ToLower() && x.GroupId == groupId && x.UserId == userId))
            return false;

        await using var context = new GroupMemberDataContext();

        var result = new GroupMemberAliasDto
        {
            Alias = alias,
            UserId = userId,
            GroupId = groupId
        };

        await context.GroupMemberAliases.AddAsync(result);

        await context.SaveChangesAsync();

        AliasList.Add(result);

        BakeIndexingData();

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

        AliasList.RemoveAll(x => x.MemberAliasId == aliasId);

        BakeIndexingData();

        return true;
    }
}