using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;

namespace Sapphire.Commands;

public class ConfirmationCommand : CommandBase
{
    public ConfirmationCommand()
    {
        Instance = this;
    }

    public static ConfirmationCommand? Instance { get; private set; }

    private Dictionary<long, ConfirmationItem> Items { get; } = new();

    protected override async Task ProcessDirectly(CqMessagePostContext context)
    {
        var replyMessage = (CqReplyMsg?)context.Message.FirstOrDefault(x => x is CqReplyMsg);
        if (replyMessage == null || replyMessage.Id == null)
            return;

        var id = replyMessage.Id ?? 0;
        if (!Items.TryGetValue(id, out var item)) return;

        if (context.UserId == item.TargetUserId)
        {
            if (context.Message.Text.Trim().Equals("确定"))
            {
                await item.OnAgree();
                Items.Remove(id);
            }
            else if (context.Message.Text.Trim().Equals("取消"))
            {
                await item.OnCancel();
                Items.Remove(id);
            }
        }
    }

    public async Task AddConfirmation(Func<Task> onAgree, Func<Task> onCancel, string actionName,
        CqMessagePostContext context, long targetUserId)
    {
        var item = new ConfirmationItem
        {
            OnAgree = onAgree,
            OnCancel = onCancel,
            TargetUserId = targetUserId,
            Timestamp = DateTime.UtcNow
        };

        var messageId = await SendMessage(context,
            [$"您正在尝试{actionName}，请用户 {await GroupMemberCommandBase.GetNickname(targetUserId) ?? "未知"} ({targetUserId}) 对此条消息回复 “确定” 以确定，回复 “取消” 以取消。"]);

        if (messageId == null)
        {
            await SendUnknownError(context);
            return;
        }

        var itemAdded = Items.TryAdd(messageId ?? 0, item);

        if (!itemAdded) await SendUnknownError(context);
    }

    public class ConfirmationItem
    {
        public required Func<Task> OnAgree { get; init; }
        public required Func<Task> OnCancel { get; init; }
        public required long TargetUserId { get; init; }

        public DateTime Timestamp { get; init; }
    }
}