using EleCho.GoCqHttpSdk;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;

namespace Sapphire.Commands;

public class FlagSpec
{
    public string Name => LongName ?? ShortName;
    public required string ShortName { get; set; }
    public required string LongName { get; set; }
    public bool TakesValue { get; set; } = true;
}

public class CommandContext
{
    public string? Argument { get; set; }

    public Dictionary<string, string> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string? GetFlag(string flagName, string? defaultValue = null)
    {
        return Flags.TryGetValue(flagName, out var val) ? val : defaultValue;
    }

    public bool HasFlag(string flagName)
    {
        return Flags.ContainsKey(flagName);
    }
}

public abstract class CommandBase
{
    public CommandBase[] SubCommands { get; protected set; } = [];

    public string[] CommandHeads { get; protected set; } = [];
    public string[] DirectCommandHeads { get; protected set; } = [];

    public List<FlagSpec> SupportedFlags { get; protected set; } = [];

    public string ActivationSettingsIdentifier { get; protected set; } = "";

    public virtual void Initialize()
    {
        
    }

    public void StartInitialize()
    {
        Initialize();
        
        foreach (var groupCommandBase in SubCommands)
        {
            groupCommandBase.StartInitialize();
        }
    }

    public async Task Receive(CommandContext commandContext, CqMessagePostContext context)
    {
        if (commandContext.Flags.Count == 0 && commandContext.Argument is null)
        {
            await Process(context);
            return;
        }

        await ProcessWithArguments(commandContext, context);
    }

    protected virtual async Task Process(CqMessagePostContext context)
    {
        await SendMessage(context, ["dame"]);
    }

    protected virtual async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        await SendMessage(context, ["dame"]);
    }

    protected async Task SendMessage(long id, CqMessage message, bool isGroup = true)
    {
        var session = Program.BotSessionHandler?.Session;

        if (session == null)
            return;

        if (isGroup)
            await session.SendGroupMessageAsync(id, message);
        else
            await session.SendPrivateMessageAsync(id, message);
    }

    protected async Task SendMessage(CqMessagePostContext context, CqMessage message, bool reply = true)
    {
        var session = Program.BotSessionHandler?.Session;

        if (session == null)
            return;
        
        if (context is CqPrivateMessagePostContext privateMessageContext)
        {
            var finalMessage = reply ? [new CqReplyMsg(privateMessageContext.MessageId), .. message] : message;
            await session.SendPrivateMessageAsync(privateMessageContext.UserId, finalMessage);
        }
        if (context is CqGroupMessagePostContext groupMessageContext)
        {
            var finalMessage = reply ? [new CqReplyMsg(groupMessageContext.MessageId), .. message] : message;
            await session.SendGroupMessageAsync(groupMessageContext.GroupId, finalMessage);
        }
    }
}