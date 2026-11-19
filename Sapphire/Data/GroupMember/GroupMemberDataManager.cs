using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Sapphire.Data.GroupMember.Database;
using Sapphire.Data.GroupMember.Dtos;
using Sapphire.Environments;
using Sapphire.Utils;
using Serilog;

namespace Sapphire.Data.GroupMember;

public class GroupMemberDataManager
{
    public static GroupMemberDataManager? Instance;

    private readonly Lock _reloadLock = new();
    private static readonly SemaphoreSlim NameSaveLock = new(1, 1);

    public GroupMemberDataManager()
    {
        Instance = this;

        using var context = new GroupMemberDataContext();

        context.Database.Migrate();

        AliasList = context.GroupMemberAliases.ToList();
        var userPool = context.GroupMembers.ToList();
        var groupResult = userPool.GroupBy(x => x.GroupId);
        foreach (var group in groupResult) UserPool.Add(group.Key, group.Select(x => x.UserId).ToHashSet());

        foreach (var member in userPool)
            if (!string.IsNullOrEmpty(member.SceneUserId))
                SceneUserIds[(member.GroupId, member.UserId)] = member.SceneUserId;

        foreach (var alias in AliasList)
            if (!string.IsNullOrEmpty(alias.SceneUserId))
                SceneUserIds[(alias.GroupId, alias.UserId)] = alias.SceneUserId;

        try
        {
            foreach (var knownName in context.KnownNames.AsNoTracking().ToList())
                KnownNames[knownName.UserId] = knownName.DisplayName;

            foreach (var displayName in context.DisplayNames.AsNoTracking().ToList())
                DisplayNames[(displayName.GroupId, displayName.UserId)] = displayName.DisplayName;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load known/display names");
        }

        BakeIndexingData();
    }

    public Dictionary<string, (string groupId, string userId)[]> AliasToUserIds { get; } = [];
    public Dictionary<(string groupId, string userId), string[]> AliasSearchKeys { get; } = [];
    public Dictionary<int, GroupMemberAliasDto> AliasIdToAlias { get; } = [];
    public (string groupId, string userId)[] AliasedUserIds { get; private set; } = [];
    public Dictionary<(string groupId, string userId), GroupMemberAliasDto[]> UserIdToAliases { get; } = [];

    public List<GroupMemberAliasDto> AliasList { get; }

    public Dictionary<string, HashSet<string>> UserPool { get; } = [];

    /// <summary>
    ///     canonical 用户 ID → 最近一次已知展示名。跨群、跨重启可用。
    /// </summary>
    public ConcurrentDictionary<string, string> KnownNames { get; } = new(StringComparer.Ordinal);

    /// <summary>
    ///     (群 ID, canonical 用户 ID) → 当前群内的 member_openid。
    /// </summary>
    public Dictionary<(string groupId, string userId), string> SceneUserIds { get; } = [];

    /// <summary>
    ///     (群 ID, canonical 用户 ID) → 当前群展示名。
    /// </summary>
    public ConcurrentDictionary<(string groupId, string userId), string> DisplayNames { get; } = new();

    /// <summary>
    ///     迁移服务直接写库后，重新读取别名/成员并重建内存索引。
    /// </summary>
    public void ReloadAndBake()
    {
        lock (_reloadLock)
        {
            using var context = new GroupMemberDataContext();

            var aliases = context.GroupMemberAliases.AsNoTracking().ToList();
            var members = context.GroupMembers.AsNoTracking().ToList();

            AliasList.Clear();
            AliasList.AddRange(aliases);

            UserPool.Clear();
            foreach (var group in members.GroupBy(x => x.GroupId))
                UserPool.Add(group.Key, group.Select(x => x.UserId).ToHashSet());

            SceneUserIds.Clear();

            foreach (var member in members)
                if (!string.IsNullOrEmpty(member.SceneUserId))
                    SceneUserIds[(member.GroupId, member.UserId)] = member.SceneUserId;

            foreach (var alias in aliases)
                if (!string.IsNullOrEmpty(alias.SceneUserId))
                    SceneUserIds[(alias.GroupId, alias.UserId)] = alias.SceneUserId;

            try
            {
                var knownNames = context.KnownNames.AsNoTracking().ToList();
                KnownNames.Clear();
                foreach (var knownName in knownNames)
                    KnownNames[knownName.UserId] = knownName.DisplayName;

                var displayNames = context.DisplayNames.AsNoTracking().ToList();
                DisplayNames.Clear();
                foreach (var displayName in displayNames)
                    DisplayNames[(displayName.GroupId, displayName.UserId)] = displayName.DisplayName;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to reload known/display names");
            }

            BakeIndexingData();
        }
    }

    public string? GetSceneUserId(string groupId, string userId)
    {
        return SceneUserIds.GetValueOrDefault((groupId, userId));
    }

    public string? GetDisplayName(string groupId, string userId)
    {
        return DisplayNames.GetValueOrDefault((groupId, userId));
    }

    public string? GetKnownName(string userId)
    {
        return KnownNames.GetValueOrDefault(userId);
    }

    public void RememberName(string userId, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return;

        if (KnownNames.TryGetValue(userId, out var knownName) && knownName == displayName)
            return;

        KnownNames[userId] = displayName;

        _ = SaveKnownNameAsync(userId, displayName);
    }

    private static async Task SaveKnownNameAsync(string userId, string displayName)
    {
        await NameSaveLock.WaitAsync();

        try
        {
            await using var context = new GroupMemberDataContext();
            var entity = await context.KnownNames.FindAsync(userId);

            if (entity == null)
                context.KnownNames.Add(new KnownNameDto
                {
                    UserId = userId,
                    DisplayName = displayName,
                    UpdatedAt = DateTime.UtcNow
                });
            else
            {
                entity.DisplayName = displayName;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to persist known name for {UserId}", userId);
        }
        finally
        {
            NameSaveLock.Release();
        }
    }

    public void UpdateUser(string groupId, string userId, string sceneUserId, string? displayName)
    {
        if (!string.IsNullOrEmpty(sceneUserId))
            SceneUserIds[(groupId, userId)] = sceneUserId;

        if (string.IsNullOrWhiteSpace(displayName))
            return;

        if (DisplayNames.TryGetValue((groupId, userId), out var existingName) &&
            existingName == displayName)
            return;

        DisplayNames[(groupId, userId)] = displayName;

        _ = SaveDisplayNameAsync(groupId, userId, displayName);
    }

    private static async Task SaveDisplayNameAsync(string groupId, string userId, string displayName)
    {
        await NameSaveLock.WaitAsync();

        try
        {
            await using var context = new GroupMemberDataContext();
            var entity = await context.DisplayNames.FindAsync(groupId, userId);

            if (entity == null)
                context.DisplayNames.Add(new DisplayNameDto
                {
                    GroupId = groupId,
                    UserId = userId,
                    DisplayName = displayName,
                    UpdatedAt = DateTime.UtcNow
                });
            else
            {
                entity.DisplayName = displayName;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to persist display name for {UserId} in {GroupId}", userId, groupId);
        }
        finally
        {
            NameSaveLock.Release();
        }
    }

    public List<string> FindUserIds(string groupId, string key)
    {
        var result = new List<string>();

        if (UserPool.TryGetValue(groupId, out var users))
            foreach (var userId in users)
                if (string.Equals(userId, key, StringComparison.Ordinal) ||
                    string.Equals(GetSceneUserId(groupId, userId), key, StringComparison.Ordinal))
                    result.Add(userId);

        foreach (var ((sceneGroupId, userId), sceneUserId) in SceneUserIds)
            if (string.Equals(sceneGroupId, groupId, StringComparison.Ordinal) &&
                (string.Equals(userId, key, StringComparison.Ordinal) ||
                 string.Equals(sceneUserId, key, StringComparison.Ordinal)))
                result.Add(userId);

        return result.Distinct().ToList();
    }

    public List<GroupMemberMarriageDto> GetMarriages()
    {
        using var context = new GroupMemberDataContext();

        var dayStart = GetCurrentDayStart();

        context.GroupMemberMarriages.Where(x => x.MarriedAt < dayStart).ExecuteDelete();

        return context.GroupMemberMarriages.AsNoTracking()
            .Where(x => x.MarriedAt >= dayStart)
            .ToList();
    }

    public async Task RemoveExpiredMarriagesAsync()
    {
        await using var context = new GroupMemberDataContext();

        var dayStart = GetCurrentDayStart();

        await context.GroupMemberMarriages
            .Where(x => x.MarriedAt < dayStart)
            .ExecuteDeleteAsync();
    }

    public async Task AddMarriages(string groupId, IEnumerable<(string UserId, string PartnerId)> marriages,
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

    public async Task RemoveMarriage(string groupId, string userId, string partnerId)
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

    public async Task RemoveUserFromPool(string userId, string groupId)
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

    public async Task AddUserToPool(string userId, string sceneUserId, string groupId)
    {
        if (!UserPool.TryGetValue(groupId, out var group))
        {
            UserPool.Add(groupId, [userId]);

            await using (var context = new GroupMemberDataContext())
            {
                context.GroupMembers.Add(new GroupMemberDto
                {
                    GroupId = groupId,
                    UserId = userId,
                    SceneUserId = sceneUserId
                });

                await context.SaveChangesAsync();
            }

            return;
        }

        if (group.Contains(userId))
        {
            SceneUserIds[(groupId, userId)] = sceneUserId;

            await using var context = new GroupMemberDataContext();
            var member = await context.GroupMembers
                .FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId);

            if (member == null || member.SceneUserId == sceneUserId)
                return;

            member.SceneUserId = sceneUserId;
            await context.SaveChangesAsync();
            return;
        }

        group.Add(userId);
        SceneUserIds[(groupId, userId)] = sceneUserId;

        await using (var context = new GroupMemberDataContext())
        {
            context.GroupMembers.Add(new GroupMemberDto
            {
                GroupId = groupId,
                UserId = userId,
                SceneUserId = sceneUserId
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

    public async Task<bool> TryAddAlias(string userId, string sceneUserId, string groupId, string alias)
    {
        if (AliasList.AsQueryable()
            .Any(x => x.Alias.ToLower() == alias.ToLower() && x.GroupId == groupId && x.UserId == userId))
            return false;

        await using var context = new GroupMemberDataContext();

        var result = new GroupMemberAliasDto
        {
            Alias = alias,
            UserId = userId,
            SceneUserId = sceneUserId,
            GroupId = groupId
        };

        await context.GroupMemberAliases.AddAsync(result);

        await context.SaveChangesAsync();

        AliasList.Add(result);

        if (!string.IsNullOrEmpty(sceneUserId))
            SceneUserIds[(groupId, userId)] = sceneUserId;

        BakeIndexingData();

        return true;
    }

    public async Task<bool> TryRemoveAlias(int aliasId, string userId, bool forced = false)
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