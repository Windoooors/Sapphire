using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.Chuni;
using Sapphire.Data.Chuni.Database;

namespace Sapphire.Commands;

public class ChuniCommand : ChuniCommandBase
{
    public ChuniCommand()
    {
        CommandHeads = ["chuni", "chu", "chunithm", "c"];
        DirectCommandHeads = [];
    }

    public override void Initialize()
    {
        DataManager = new ChuniDataManager();
    }

    protected override async Task Process(CqMessagePostContext context)
    {
        await SendMessage(context, ["chuni"]);
    }
}