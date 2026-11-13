using Sapphire.Utils;

namespace Sapphire.Commands;

public class IcsAssistantCommand : CommandBase
{
    private static readonly Dictionary<string, Func<string, IFormattable>> FloatConverters = new()
    {
        ["32"] = s => FloatConverter.ToSingle(s),
        ["64"] = s => FloatConverter.ToDouble(s)
    };

    private static readonly Dictionary<string, Func<string, IFormattable>> SignedConverters = new()
    {
        ["8"] = s => IntegerConverter.ToSByte(s),
        ["16"] = s => IntegerConverter.ToInt16(s),
        ["32"] = s => IntegerConverter.ToInt32(s),
        ["64"] = s => IntegerConverter.ToInt64(s)
    };

    private static readonly Dictionary<string, Func<string, IFormattable>> UnsignedConverters = new()
    {
        ["8"] = s => IntegerConverter.ToByte(s),
        ["16"] = s => IntegerConverter.ToUInt16(s),
        ["32"] = s => IntegerConverter.ToUInt32(s),
        ["64"] = s => IntegerConverter.ToUInt64(s)
    };

    public IcsAssistantCommand()
    {
        DirectCommandHeads = ["ics", "计基外挂"];
        CommandHeads = ["ics"];

        ActivationSettingsIdentifier = "ics.enabled";

        SupportedFlags =
        [
            new FlagSpec
            {
                LongName = "--signed",
                ShortName = "-s",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--float",
                ShortName = "-f",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--length",
                ShortName = "-l",
                TakesValue = true
            },
            new FlagSpec
            {
                LongName = "--target",
                ShortName = "-t",
                TakesValue = true
            }
        ];
    }

    protected override async Task ProcessWithArguments(
        CommandContext commandContext,
        MessageContext context)
    {
        var numberString = commandContext.Argument;
        var target = commandContext.GetFlag("--target") ?? "hex";

        if (numberString == null)
        {
            await SendArgumentError(context);
            return;
        }

        var lengthString = commandContext.GetFlag("--length", "32") ?? "32";

        var signed = commandContext.HasFlag("--signed");
        var isFloat = commandContext.HasFlag("--float");

        var converters = isFloat
            ? FloatConverters
            : signed
                ? SignedConverters
                : UnsignedConverters;

        if (!converters.TryGetValue(lengthString, out var converter))
        {
            await SendMessage(context, ["不支持的长度"]);
            return;
        }

        try
        {
            var value = converter(numberString);

            if (isFloat)
            {
                var isSingle = value is float;

                string result;

                if (isSingle)
                    result = target switch
                    {
                        "hex" => FloatConverter.ToHex((float)value),
                        "bin" => FloatConverter.ToBinary((float)value),
                        _ => FloatConverter.ToDecimal((float)value)
                    };
                else
                    result = target switch
                    {
                        "hex" => FloatConverter.ToHex((double)value),
                        "bin" => FloatConverter.ToBinary((double)value),
                        _ => FloatConverter.ToDecimal((double)value)
                    };

                await SendMessage(context, [result]);
            }
            else
            {
                await SendMessage(context, [
                    target switch
                    {
                        "hex" => "0x",
                        "bin" => "0b",
                        _ => ""
                    } +
                    value.ToString(target switch
                    {
                        "hex" => "X",
                        "bin" => "B",
                        _ => "D"
                    }, null)
                ]);
            }
        }
        catch (FormatException)
        {
            await SendMessage(context, ["输入格式有误"]);
        }
        catch (OverflowException)
        {
            await SendMessage(context, ["溢出"]);
        }
    }
}