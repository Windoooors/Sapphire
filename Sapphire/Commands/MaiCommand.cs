using System.Text;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Commands.MaiCommands;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.DivingFishDataManagement;
using Sapphire.Data.Mai.Dtos;

namespace Sapphire.Commands;

public class MaiCommand : MaiCommandBase
{
    public MaiCommand()
    {
        CommandHeads = ["mai", "m", "maimai"];
        DirectCommandHeads = [];
        SubCommands = [new SearchCommand(), new AliasCommand(), new BindCommand(), new UpdateScoreCommand(), new InfoCommand()];
    }

    public override void Initialize()
    {
        DataManager = new MaiDataManager();
        DivingFishMaiDataManager = new DivingFishMaiDataManager();
    }
}