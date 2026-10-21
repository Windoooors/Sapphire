using Sapphire.Commands.MaiCommands;
using Sapphire.Data.DivingFishMai;
using Sapphire.Data.Mai;
using Sapphire.ImageGeneration.ImageGenerators;

namespace Sapphire.Commands;

public class MaiCommand : MaiCommandBase
{
    public MaiCommand()
    {
        CommandHeads = ["mai", "m", "maimai"];
        DirectCommandHeads = [];
        SubCommands =
        [
            new SearchCommand(), new AliasCommand(), new BindCommand(), new UpdateScoreCommand(), new InfoCommand(),
            new BestListCommand(), new WckSessionLoginCommand(), new WckSessionLogoutCommand(), new ExportCommand()
        ];
    }

    public override void Initialize()
    {
        DataManager = new MaiDataManager();
        DivingFishMaiDataManager = new DivingFishMaiDataManager();
        MaiImageGeneratorFunctions.LoadImages();
    }
}