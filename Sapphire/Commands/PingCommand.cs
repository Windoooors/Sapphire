using System.Text;
using EleCho.GoCqHttpSdk.Post;

namespace Sapphire.Commands;

public class PingCommand : CommandBase
{
    public PingCommand()
    {
        CommandHeads = ["ping"];
        DirectCommandHeads = ["ping"];
        ActivationSettingsIdentifier = "ping.enabled";
    }

    protected override async Task Process(CqMessagePostContext context)
    {
        var stringBuilder = new StringBuilder("Pong!\n");
        stringBuilder.Append("未释放 ImageSharp.Image 实例数量：");
        stringBuilder.Append(SixLabors.ImageSharp.Diagnostics.MemoryDiagnostics.TotalUndisposedAllocationCount);

        await SendMessage(context, [stringBuilder.ToString()]);
    }
}