using System.IO.Hashing;
using System.Text;
using EleCho.GoCqHttpSdk.Post;

namespace Sapphire.Commands;

public class GayFactor : CommandBase
{
    public GayFactor()
    {
        CommandHeads = ["gay指数", "gay"];
        DirectCommandHeads = ["gay指数"];

        ActivationSettingsIdentifier = "gay_factor.enabled";
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        var name = commandContext.Argument?.Trim();

        if (name == null)
        {
            await SendArgumentError(context);
            return;
        }

        var hash = XxHash32.HashToUInt32(Encoding.UTF8.GetBytes(name));
        var bucket = (int)(hash % 101);

        await SendMessage(context, [$"{name} 的 Gay 指数为 {bucket}"]);
    }
}