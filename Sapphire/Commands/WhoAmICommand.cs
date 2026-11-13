using System.Text;

namespace Sapphire.Commands;

public class WhoAmICommand : CommandBase
{
    public WhoAmICommand()
    {
        CommandHeads = ["whoami"];
        DirectCommandHeads = ["whoami"];
        ActivationSettingsIdentifier = "whoami.enabled";
    }

    protected override async Task Process(MessageContext context)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine($"场景：{(context.IsGroup ? "群聊" : "单聊")}");
        stringBuilder.AppendLine($"ChatId：{context.ChatId}");
        stringBuilder.AppendLine($"CanonicalUserId：{context.UserId}");
        stringBuilder.AppendLine($"SceneUserId：{context.SceneUserId}");

        if (!string.IsNullOrWhiteSpace(context.UnionOpenId))
            stringBuilder.AppendLine($"UnionOpenId：{context.UnionOpenId}");

        if (!string.IsNullOrWhiteSpace(context.MemberRole))
            stringBuilder.AppendLine($"群角色：{context.MemberRole}");

        if (!string.IsNullOrWhiteSpace(context.UserName))
            stringBuilder.AppendLine($"用户名：{context.UserName}");

        if (!string.IsNullOrWhiteSpace(context.GroupNickname))
            stringBuilder.AppendLine($"群昵称：{context.GroupNickname}");

        await SendMessage(context, stringBuilder.ToString().Trim());
    }
}
