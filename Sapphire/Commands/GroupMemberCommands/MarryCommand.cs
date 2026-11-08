using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.GroupMember;
using Sapphire.ImageGeneration;

namespace Sapphire.Commands.GroupMemberCommands;

public class MarryCommand : GroupMemberCommandBase
{
    private static readonly Dictionary<long, Dictionary<long, long>> Pairings = new();
    private static readonly Lock PairingLock = new();
    private static readonly Random Random = new();

    public MarryCommand()
    {
        DirectCommandHeads = ["娶", "嫁", "娶群友"];
        CommandHeads = ["娶", "嫁", "娶群友"];

        ActivationSettingsIdentifier = "marry.enabled";

        Program.Timer.DayChanged += ClearAllPairings;
    }

    private static void ClearAllPairings()
    {
        lock (PairingLock)
        {
            Pairings.Clear();
        }
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

        var groupId = groupMessageContext.GroupId;
        var userId = groupMessageContext.UserId;

        if (!GroupMemberDataManager.Instance.UserPool.TryGetValue(groupId, out var group))
        {
            await GroupMemberDataManager.Instance.AddUserToPool(userId, groupId);

            await SendNoAvailableMemberMessage(context);

            return;
        }

        if (!group.Contains(userId))
            await GroupMemberDataManager.Instance.AddUserToPool(userId, groupId);

        if (group.Count < 2)
        {
            await SendNoAvailableMemberMessage(context);
            return;
        }

        var partnerId = EnsurePairsAndGetPartner(groupId, group, userId);

        if (partnerId is null)
        {
            await SendNoAvailableMemberMessage(context);
            return;
        }

        if (!await SendMessage(groupMessageContext, partnerId.Value))
        {
            RemovePairing(groupId, userId, partnerId.Value);
            await Process(context);
        }
    }

    private static long? EnsurePairsAndGetPartner(
        long groupId,
        IEnumerable<long> groupMembers,
        long currentUserId)
    {
        lock (PairingLock)
        {
            if (!Pairings.TryGetValue(groupId, out var pairs))
            {
                pairs = new Dictionary<long, long>();
                Pairings[groupId] = pairs;
            }

            var members = groupMembers.Distinct().ToHashSet();

            CleanupInvalidPairings(members, pairs);

            var unpaired = members
                .Where(id => !pairs.ContainsKey(id))
                .ToList();

            Shuffle(unpaired);

            for (var i = 0; i + 1 < unpaired.Count; i += 2)
            {
                var a = unpaired[i];
                var b = unpaired[i + 1];

                pairs[a] = b;
                pairs[b] = a;
            }

            return pairs.TryGetValue(currentUserId, out var partnerId)
                ? partnerId
                : null;
        }
    }

    private static void CleanupInvalidPairings(
        HashSet<long> members,
        Dictionary<long, long> pairs)
    {
        foreach (var pair in pairs.ToArray())
        {
            var userId = pair.Key;
            var partnerId = pair.Value;

            var invalid =
                userId == partnerId ||
                !members.Contains(userId) ||
                !members.Contains(partnerId) ||
                !pairs.TryGetValue(partnerId, out var reversePartnerId) ||
                reversePartnerId != userId;

            if (!invalid)
                continue;

            pairs.Remove(userId);

            if (pairs.TryGetValue(partnerId, out var reverseId) &&
                reverseId == userId)
                pairs.Remove(partnerId);
        }
    }

    private static void RemovePairing(long groupId, long userId, long partnerId)
    {
        lock (PairingLock)
        {
            if (!Pairings.TryGetValue(groupId, out var pairs))
                return;

            if (pairs.TryGetValue(userId, out var p1) && p1 == partnerId)
                pairs.Remove(userId);

            if (pairs.TryGetValue(partnerId, out var p2) && p2 == userId)
                pairs.Remove(partnerId);

            if (pairs.Count == 0)
                Pairings.Remove(groupId);
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private async Task<bool> SendMessage(
        CqGroupMessagePostContext groupMessageContext,
        long userId)
    {
        var nickname = await GetNickname(userId, groupMessageContext.GroupId, true);

        if (GroupMemberDataManager.Instance == null)
            return false;

        if (nickname == null)
        {
            await GroupMemberDataManager.Instance.RemoveUserFromPool(
                userId,
                groupMessageContext.GroupId);

            return false;
        }

        await SendMessage(groupMessageContext, [
            new CqImageMsg("base64://" +
                           await ImageGeneratorFunctions.GetUserAvatarBase64Async(userId, 640)),
            $"您今天的对象是 {nickname} ({userId})！"
        ]);

        return true;
    }

    private async Task SendNoAvailableMemberMessage(CqMessagePostContext context)
    {
        await SendMessage(context, ["无可娶群友"]);
    }
}