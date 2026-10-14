using EleCho.GoCqHttpSdk.Post;

namespace Sapphire.Commands;

public class PingCommand : CommandBase
{
    public PingCommand()
    {
        CommandHeads = ["ping"];
        DirectCommandHeads = ["ping"];
        ActivationSettingsIdentifier = "ping.enabled";

        SupportedFlags =
        [
            new FlagSpec
            {
                ShortName = "-t",
                LongName = "--test",
                TakesValue = true
            },
            new FlagSpec
            {
                ShortName = "-t2",
                LongName = "--test2",
                TakesValue = true
            }
        ];
    }

    protected override async Task Process(CqMessagePostContext context)
    {
        await SendMessage(context, ["Sapphire speaking!!!"]);
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        if (commandContext.HasFlag("--test"))
        {
            var msg = commandContext.GetFlag("--test");

            if (msg != null)
            {
                await SendMessage(context, [msg]);
                return;
            }
        }

        if (commandContext.HasFlag("--test2"))
        {
            if (ConfirmationCommand.Instance == null)
                return;

            await ConfirmationCommand.Instance.AddConfirmation(async () => { await SendMessage(context, ["确定了"]); },
                async () => { await SendMessage(context, ["取消了"]); }, "ping", context, context.UserId);

            return;
        }

        await SendMessage(context, ["未知参数"]);
    }
}