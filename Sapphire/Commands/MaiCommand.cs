using EleCho.GoCqHttpSdk.Post;
using Sapphire.Commands.MaiCommands;
using Sapphire.Data.Mai;

namespace Sapphire.Commands;

public class MaiCommand : MaiCommandBase
{
    public MaiCommand()
    {
        CommandHeads = ["mai", "m", "maimai"];
        DirectCommandHeads = [];
        SubCommands = [new SearchCommand()];
    }

    public override void Initialize()
    {
        DataManager = new MaiDataManager();
    }
}