using QQBot;
using Sapphire.Environments;
using Serilog;

namespace Sapphire.Commands;

public class FlagSpec
{
    public string Name => LongName;
    public required string ShortName { get; set; }
    public required string LongName { get; set; }
    public required bool TakesValue { get; set; }
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

        foreach (var groupCommandBase in SubCommands) groupCommandBase.StartInitialize();
    }

    protected virtual async Task ProcessDirectly(MessageContext context)
    {
    }

    public async Task ReceiveDirectly(MessageContext context)
    {
        await ProcessDirectly(context);

        foreach (var commandBase in SubCommands) await commandBase.ProcessDirectly(context);
    }

    public async Task Receive(CommandContext commandContext, MessageContext context)
    {
        if (commandContext.Flags.Count == 0 && commandContext.Argument is null)
        {
            await Process(context);
            return;
        }

        await ProcessWithArguments(commandContext, context);
    }

    protected virtual async Task Process(MessageContext context)
    {
        await SendMessage(context, "dame");
    }

    protected virtual async Task ProcessWithArguments(CommandContext commandContext, MessageContext context)
    {
        await SendMessage(context, "dame");
    }

    public static async Task SendUnknownError(MessageContext context)
    {
        await SendMessage(context, "出现了未知的错误");
    }

    public static async Task SendArgumentError(MessageContext context)
    {
        await SendMessage(context, "参数错误");
    }

    public static async Task SendUnpermittedError(MessageContext context)
    {
        await SendMessage(context, "您无权执行该命令");
    }

    public void OnExitCall()
    {
        foreach (var commandBase in SubCommands) commandBase.OnExitCall();

        OnExit();
    }

    protected virtual void OnExit()
    {
    }

    protected static async Task SendMessage(MessageContext context, params string[] messages)
    {
        var text = string.Concat(messages);

        if (string.IsNullOrEmpty(text))
            return;

        try
        {
            await context.Channel.SendMessageAsync(text, passiveSource: context.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while sending a message to {ChatId}", context.ChatId);
        }
    }

    protected static async Task SendMessage(MessageContext context, string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        try
        {
            await context.Channel.SendMessageAsync(message, passiveSource: context.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while sending a message to {ChatId}", context.ChatId);
        }
    }

    protected static async Task SendImage(MessageContext context, string base64Image)
    {
        if (string.IsNullOrWhiteSpace(base64Image))
            return;

        try
        {
            using var attachment = new FileAttachment(Convert.FromBase64String(base64Image), "image.jpg");
            await context.Channel.SendMessageAsync(attachment: attachment, passiveSource: context.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while sending an image to {ChatId}", context.ChatId);
        }
    }

    protected static bool IsBotAdministrator(MessageContext context)
    {
        var administratorOpenIds = SpEnvironmentHandler.Environment.Sapphire.AdministratorOpenIds;

        if (administratorOpenIds.Length == 0)
            return false;

        if (administratorOpenIds.Contains(context.UserId, StringComparer.Ordinal))
            return true;

        if (context.UnionOpenId != null &&
            administratorOpenIds.Contains(context.UnionOpenId, StringComparer.Ordinal))
            return true;

        return !string.Equals(context.SceneUserId, context.UserId, StringComparison.Ordinal) &&
               administratorOpenIds.Contains(context.SceneUserId, StringComparer.Ordinal);
    }

    protected bool IsGroupAdministrator(MessageContext context)
    {
        if (!context.IsGroup)
            return false;

        if (context.MemberRole is "owner" or "admin")
            return true;

        return IsBotAdministrator(context);
    }
}
