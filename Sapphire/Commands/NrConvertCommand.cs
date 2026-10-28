using EleCho.GoCqHttpSdk.Post;

namespace Sapphire.Commands;

public class NrConvertCommand : CommandBase // NR stands for Number Representation
{
    public NrConvertCommand()
    {
        DirectCommandHeads = ["nrc", "数制转换"];
        CommandHeads = ["nrc"];

        ActivationSettingsIdentifier = "nrc.enabled";

        SupportedFlags =
        [
            new FlagSpec
            {
                LongName = "--complement",
                ShortName = "-c"
            },
            new FlagSpec
            {
                LongName = "--length",
                ShortName = "-l"
            },
            new FlagSpec
            {
                LongName = "--hex",
                ShortName = "-h",
                TakesValue = false
            }
        ];
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        var numberString = commandContext.GetFlag("--complement");
        var lengthString = commandContext.GetFlag("--length", "8");

        var format = commandContext.HasFlag("--hex") ? "X" : "B";


        if (commandContext.HasFlag("--complement"))
        {
            switch (lengthString)
            {
                case "8":
                {
                    if (sbyte.TryParse(numberString, out var number))
                    {
                        await SendMessage(context, [number.ToString(format)]);
                        return;
                    }

                    break;
                }
                case "16":
                {
                    if (short.TryParse(numberString, out var number))
                    {
                        await SendMessage(context, [number.ToString(format)]);
                        return;
                    }

                    break;
                }
                case "32":
                {
                    if (int.TryParse(numberString, out var number))
                    {
                        await SendMessage(context, [number.ToString(format)]);
                        return;
                    }

                    break;
                }
                case "64":
                {
                    if (long.TryParse(numberString, out var number))
                    {
                        await SendMessage(context, [number.ToString(format)]);
                        return;
                    }

                    break;
                }
                case "128":
                {
                    if (Int128.TryParse(numberString, out var number))
                    {
                        await SendMessage(context, [number.ToString(format)]);
                        return;
                    }

                    break;
                }
            }

            await SendArgumentError(context);
        }
    }
}