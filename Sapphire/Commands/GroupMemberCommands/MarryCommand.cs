using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.GroupMember;
using Sapphire.ImageGeneration;

namespace Sapphire.Commands.GroupMemberCommands;

public class MarryCommand : GroupMemberCommandBase
{
    private static readonly Dictionary<long, Dictionary<long, Pairing>> Pairings = new();
    private static readonly Lock PairingLock = new();
    private static readonly Random Random = new();

    public MarryCommand()
    {
        DirectCommandHeads = ["娶", "嫁", "娶群友"];
        CommandHeads = ["娶", "嫁", "娶群友"];

        ActivationSettingsIdentifier = "marry.enabled";
    }

    public override void Initialize()
    {
        base.Initialize();

        LoadPairings();
    }

    private static void LoadPairings()
    {
        if (GroupMemberDataManager.Instance == null)
            return;

        var marriages = GroupMemberDataManager.Instance.GetMarriages();

        lock (PairingLock)
        {
            Pairings.Clear();

            foreach (var marriage in marriages)
            {
                if (!Pairings.TryGetValue(marriage.GroupId, out var pairs))
                {
                    pairs = new Dictionary<long, Pairing>();
                    Pairings[marriage.GroupId] = pairs;
                }

                pairs[marriage.UserId] = new Pairing(marriage.PartnerId, marriage.MarriedAt);
                pairs[marriage.PartnerId] = new Pairing(marriage.UserId, marriage.MarriedAt);
            }
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

        var partnerId = await EnsurePairsAndGetPartner(groupId, group, userId);

        if (partnerId is null)
        {
            await SendNoAvailableMemberMessage(context);
            return;
        }

        if (!await SendMessage(groupMessageContext, partnerId.Value))
        {
            await RemovePairing(groupId, userId, partnerId.Value);
            await Process(context);
        }
    }

    private static async Task<long?> EnsurePairsAndGetPartner(
        long groupId,
        IEnumerable<long> groupMembers,
        long currentUserId)
    {
        var manager = GroupMemberDataManager.Instance;

        if (manager == null)
            return null;

        List<(long UserId, long PartnerId)> removedPairs;
        (long UserId, long PartnerId)? addedPair = null;
        long? partnerId;
        var marriedAt = DateTime.UtcNow;

        lock (PairingLock)
        {
            if (!Pairings.TryGetValue(groupId, out var pairs))
            {
                pairs = new Dictionary<long, Pairing>();
                Pairings[groupId] = pairs;
            }

            var members = groupMembers.Distinct().ToHashSet();

            removedPairs = CleanupInvalidPairings(members, pairs);

            if (pairs.TryGetValue(currentUserId, out var currentPairing) &&
                manager.IsMarriageExpired(currentPairing.MarriedAt))
            {
                removedPairs.Add((currentUserId, currentPairing.PartnerId));
                RemovePairFromMemory(pairs, currentUserId, currentPairing.PartnerId);
            }

            if (pairs.TryGetValue(currentUserId, out var activePairing))
            {
                partnerId = activePairing.PartnerId;
            }
            else
            {
                var candidates = members
                    .Where(id => id != currentUserId && !pairs.ContainsKey(id))
                    .ToList();

                if (candidates.Count == 0)
                {
                    partnerId = null;
                }
                else
                {
                    var candidateId = candidates[Random.Next(candidates.Count)];

                    pairs[currentUserId] = new Pairing(candidateId, marriedAt);
                    pairs[candidateId] = new Pairing(currentUserId, marriedAt);

                    addedPair = (currentUserId, candidateId);
                    partnerId = candidateId;
                }
            }
        }

        foreach (var pair in removedPairs.Distinct())
            await manager.RemoveMarriage(groupId, pair.UserId, pair.PartnerId);

        if (addedPair is { } added)
            await manager.AddMarriages(groupId, [added], marriedAt);

        return partnerId;
    }

    private static List<(long UserId, long PartnerId)> CleanupInvalidPairings(
        HashSet<long> members,
        Dictionary<long, Pairing> pairs)
    {
        var removedPairs = new List<(long UserId, long PartnerId)>();

        foreach (var pair in pairs.ToArray())
        {
            var userId = pair.Key;
            var partnerId = pair.Value.PartnerId;

            var invalid =
                userId == partnerId ||
                !members.Contains(userId) ||
                !members.Contains(partnerId) ||
                !pairs.TryGetValue(partnerId, out var reversePairing) ||
                reversePairing.PartnerId != userId;

            if (!invalid)
                continue;

            pairs.Remove(userId);
            removedPairs.Add((userId, partnerId));

            if (pairs.TryGetValue(partnerId, out var reversePairingToRemove) &&
                reversePairingToRemove.PartnerId == userId)
                pairs.Remove(partnerId);
        }

        return removedPairs;
    }

    private static async Task RemovePairing(long groupId, long userId, long partnerId)
    {
        lock (PairingLock)
        {
            if (Pairings.TryGetValue(groupId, out var pairs))
            {
                RemovePairFromMemory(pairs, userId, partnerId);

                if (pairs.Count == 0)
                    Pairings.Remove(groupId);
            }
        }

        if (GroupMemberDataManager.Instance is { } manager)
            await manager.RemoveMarriage(groupId, userId, partnerId);
    }

    private static void RemovePairFromMemory(
        Dictionary<long, Pairing> pairs,
        long userId,
        long partnerId)
    {
        if (pairs.TryGetValue(userId, out var userPairing) &&
            userPairing.PartnerId == partnerId)
            pairs.Remove(userId);

        if (pairs.TryGetValue(partnerId, out var partnerPairing) &&
            partnerPairing.PartnerId == userId)
            pairs.Remove(partnerId);
    }

    private sealed record Pairing(long PartnerId, DateTime MarriedAt);

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