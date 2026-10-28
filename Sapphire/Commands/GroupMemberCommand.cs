using Sapphire.Commands.GroupMemberCommands;
using Sapphire.Data.GroupMember;

namespace Sapphire.Commands;

public class GroupMemberCommand : GroupMemberCommandBase
{
    public GroupMemberCommand()
    {
        CommandHeads = ["group", "group-member", "gm"];
        DirectCommandHeads = [];
        SubCommands = [new GroupMemberAliasCommand(), new GroupMemberSearchCommand()];
    }

    public override void Initialize()
    {
        DataManager = new GroupMemberDataManager();
    }
}