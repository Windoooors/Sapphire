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

    private static readonly Dictionary<string, Func<string, IFormattable>> FloatConverters = new()
    {
        ["32"] = s => Convert.ToSingle(s),
        ["64"] = s => Convert.ToDouble(s),
    };

    private static readonly Dictionary<string, Func<string, IFormattable>> SignedConverters = new()
    {
        ["8"] = s => Convert.ToSByte(s),
        ["16"] = s => Convert.ToInt16(s),
        ["32"] = s => Convert.ToInt32(s),
        ["64"] = s => Convert.ToInt64(s),
    };

    private static readonly Dictionary<string, Func<string, IFormattable>> UnsignedConverters = new()
    {
        ["8"] = s => Convert.ToByte(s),
        ["16"] = s => Convert.ToUInt16(s),
        ["32"] = s => Convert.ToUInt32(s),
        ["64"] = s => Convert.ToUInt64(s),
    };

    protected override async Task ProcessWithArguments(
        CommandContext commandContext,
        CqMessagePostContext context)
    {
        var numberString = commandContext.Argument;
        var target = commandContext.GetFlag("--target") ?? "hex";
        
        if (numberString == null)
        {
            await SendArgumentError(context);
            return;
        }
        
        var lengthString = commandContext.GetFlag("--length", "32") ?? "32";

        var format = target switch
        {
            "hex" => "X",
            "bin" => "B",
            "dec" => "D",
            _ => "X"
        };

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
            await SendMessage(context, [value.ToString(format, null)]);
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