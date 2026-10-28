using System.Text;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.GroupMember;

namespace Sapphire.Commands.GroupMemberCommands;

public class GroupMemberAliasCommand : GroupMemberCommandBase
{
    public GroupMemberAliasCommand()
    {
        CommandHeads = ["alias"];
        DirectCommandHeads = ["member-alias", "malias"];

        SupportedFlags = new List<FlagSpec>
        {
            new()
            {
                LongName = "--add",
                ShortName = "-a"
            },
            new()
            {
                LongName = "--remove",
                ShortName = "-rm",
                TakesValue = true
            }
        };
        
        ActivationSettingsIdentifier = "group_member_alias.enabled";
    }

    private async Task QueryAliases(CommandContext commandContext, CqGroupMessagePostContext context)
    {
        if (!TryGetUsers(context, commandContext.Argument ?? "", out var userIds) || userIds == null)
        {
            await SendMessage(context, ["未找到该用户"]);
            return;
        }
        
        if (userIds.Length > 1)
        {
            await SendMessage(context,
                [await BuildMemberListString(userIds, $"s group-member alias {userIds[0]}", "查询列表中第一位用户的昵称", context.GroupId)]);
            return;
        }
        
        var userId = userIds[0];
        if (!(GroupMemberDataManager.Instance?.UserIdToAliases.TryGetValue((context.GroupId, userId),
                out var aliases) ?? false))
        {
            await SendMessage(context,
                [$"用户 {await GetNickname(userId) ?? "未知"} ({userId}) 没有昵称"]);
            return;
        }
        
        var sb = new StringBuilder($"用户 {await GetNickname(userId) ?? "未知"} ({userId}) 有如下昵称：\n");
        
        foreach (var alias in aliases)
        {
            sb.AppendLine($"AID{alias.MemberAliasId}. {alias.Alias}");
        }
        
        await SendMessage(context,
            [sb.ToString()]);
    }
    
    private async Task AddAlias(CommandContext commandContext, CqGroupMessagePostContext context)
    {
        var aliasToBeAdded = commandContext.GetFlag("--add")?.Trim();
        if (string.IsNullOrEmpty(aliasToBeAdded))
        {
            await SendArgumentError(context);
            return;
        }

        var userString = commandContext.Argument;
        if (GroupMemberDataManager.Instance == null || !TryGetUsers(context,userString ?? "", out var users) ||
            users == null || ConfirmationCommand.Instance == null)
        {
            await SendUnknownError(context);
            return;
        }

        if (users.Length == 0)
        {
            await SendMessage(context, ["未找到匹配的用户"]);
            return;
        }
        
        var nickname = await GetNickname(users[0], context.GroupId);

        if (nickname == null)
        {
            await SendMessage(context, ["未找到匹配的用户"]);
            return;
        }

        if (users.Length > 1)
        {
            await SendMessage(context,
            [
                await BuildMemberListString(users, $"s group-member alias {users[0]} --add {aliasToBeAdded}",
                    $"为用户 {nickname} ({users[0]}) 添加别名 \"{aliasToBeAdded}\"",
                    context.GroupId)
            ]);
            return;
        }

        await ConfirmationCommand.Instance.AddConfirmation(async () =>
            {
                switch (await GroupMemberDataManager.Instance.TryAddAlias(users[0], context.GroupId ,aliasToBeAdded))
                {
                    case true:
                        await SendMessage(context, ["已添加"]);
                        break;
                    case false:
                        await SendMessage(context, ["已存在该别名"]);
                        break;
                }
            }, async () => { await SendMessage(context, ["已取消"]); },
            $"为用户 {nickname} ({users[0]}) 添加别名 \"{aliasToBeAdded}\"", context, users[0]);
    }
    
    private async Task RemoveAlias(CommandContext commandContext, CqGroupMessagePostContext context)
    {
        var aliasToBeDeleted = commandContext.GetFlag("--remove")?.Trim();
        if (!uint.TryParse(aliasToBeDeleted, out var aliasId))
        {
            var aliasIdString = aliasToBeDeleted?.ToLower().Replace("aid", "").Replace("id", "");

            if (!uint.TryParse(aliasIdString, out aliasId))
            {
                await SendArgumentError(context);
                return;
            }
        }

        if (GroupMemberDataManager.Instance == null || ConfirmationCommand.Instance == null)
        {
            await SendUnknownError(context);
            return;
        }

        if (!GroupMemberDataManager.Instance.AliasIdToAlias.TryGetValue((int)aliasId, out var alias))
        {
            await SendMessage(context, ["未找到指定的别名"]);
            return;
        }

        await ConfirmationCommand.Instance.AddConfirmation(async () =>
            {
                switch (await GroupMemberDataManager.Instance.TryRemoveAlias((int)aliasId, context.UserId, IsBotAdministrator(context.UserId) || await IsGroupAdministrator(context.UserId, context)))
                {
                    case true:
                        await SendMessage(context, ["已删除"]);
                        break;
                    case false:
                        await SendMessage(context, ["删除失败，您无权删除该别名"]);
                        break;
                }
            }, async () => { await SendMessage(context, ["已取消"]); },
            $"为用户 {await GetNickname(alias.UserId, context.GroupId) ?? "未知"} ({alias.UserId}) 删除别名 \"{alias.Alias}\"", context, context.UserId);
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        if (context is not CqGroupMessagePostContext groupMessageContext)
        {
            await SendWrongContextMessage(context);
            return;
        }
        
        if (commandContext.HasFlag("--add"))
        {
            await AddAlias(commandContext, groupMessageContext);
            return;
        }

        if (commandContext.HasFlag("--remove"))
        {
            await RemoveAlias(commandContext, groupMessageContext);
            return;
        }

        await QueryAliases(commandContext, groupMessageContext);
    }
}