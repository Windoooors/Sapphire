using System.Collections.Concurrent;

namespace Sapphire.Commands;

public class ConfirmationCommand : CommandBase
{
    private static readonly ConcurrentDictionary<(string ChatId, string UserId), ConfirmationItem> Items = new();

    public ConfirmationCommand()
    {
        Instance = this;
    }

    public static ConfirmationCommand? Instance { get; private set; }

    protected override async Task ProcessDirectly(MessageContext context)
    {
        var text = context.Content.Trim();

        if (text is not ("确定" or "取消"))
            return;

        if (!Items.TryGetValue((context.ChatId, context.UserId), out var item))
            return;

        Items.TryRemove((context.ChatId, context.UserId), out _);

        if (item.ExpiresAt < DateTime.UtcNow)
            return;

        if (text == "确定")
            await item.OnAgree();
        else
            await item.OnCancel();
    }

    public async Task AddConfirmation(Func<Task> onAgree, Func<Task> onCancel, string actionName,
        MessageContext context, string targetUserId)
    {
        var item = new ConfirmationItem
        {
            OnAgree = onAgree,
            OnCancel = onCancel,
            TargetUserId = targetUserId,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        Items[(context.ChatId, targetUserId)] = item;

        var targetName = await GroupMemberCommandBase.GetNickname(targetUserId, context.ChatId) ?? targetUserId;

        await SendMessage(context,
            $"您正在尝试{actionName}，请用户 {targetName} ({targetUserId}) 直接回复 “确定” 或 “取消”。");
    }

    public class ConfirmationItem
    {
        public required Func<Task> OnAgree { get; init; }
        public required Func<Task> OnCancel { get; init; }
        public required string TargetUserId { get; init; }

        public DateTime ExpiresAt { get; init; }
    }
}
