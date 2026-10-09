using EleCho.GoCqHttpSdk.Post;

namespace Sapphire.Commands;

public class NRConvertCommand : CommandBase // NR stands for Number Representation
{
    public NRConvertCommand()
    {
        DirectCommandHeads = ["nrc", "数制转换"];
        CommandHeads = ["nrc"];

        ActivationSettingsIdentifier = "nrc.enabled";

        SupportedFlags = [new FlagSpec()
        {
            LongName = "--complement",
            ShortName = "-c"
        }, new FlagSpec()
        {
            LongName = "--length",
            ShortName = "-l"
        }];
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        var numberString = commandContext.GetFlag("--complement");
        var lengthString = commandContext.GetFlag("--length", "8");

        if (commandContext.HasFlag("--complement"))
        {
            switch (lengthString)
            {
                case "8":
                {
                    if (sbyte.TryParse(numberString, out var number))
                    {
                        await SendMessage(context, [number.ToString("B")]);
                        return;
                    }

                    break;
                }
                case "16":
                {
                    if (Int16.TryParse(numberString, out var number))
                    {
                        await SendMessage(context, [number.ToString("B")]);
                        return;
                    }

                    break;
                }
                case "32":
                {
                    if (Int32.TryParse(numberString, out var number))
                    {
                        await SendMessage(context, [number.ToString("B")]);
                        return;
                    }

                    break;
                }
                case "64":
                {
                    if (Int64.TryParse(numberString, out var number))
                    {
                        await SendMessage(context, [number.ToString("B")]);
                        return;
                    }

                    break;
                }
                case "128":
                {
                    if (Int128.TryParse(numberString, out var number))
                    {
                        await SendMessage(context, [number.ToString("B")]);
                        return;
                    }

                    break;
                }
            }

            await SendArgumentError(context);
        }
    }
}