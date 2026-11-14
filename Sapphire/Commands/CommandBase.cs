using System.Text;
using QQBot;
using Sapphire.Environments;
using Sapphire.Messages;
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

    protected static Task SendMessage(MessageContext context, params string[] messages)
    {
        var text = string.Concat(messages);

        return string.IsNullOrEmpty(text)
            ? Task.CompletedTask
            : SendSegmentsAsync(context, [new TextMessage(text)]);
    }

    protected static Task SendMessage(MessageContext context, string message)
    {
        return string.IsNullOrEmpty(message)
            ? Task.CompletedTask
            : SendSegmentsAsync(context, [new TextMessage(message)]);
    }

    /// <summary>
    ///     按顺序发送图文混排片段。官方 API 的 msg_type 不能同时携带文本和富媒体，
    ///     因此连续的文本会合并成一条文本消息，图片各自作为一条富媒体消息发送。
    /// </summary>
    protected static Task SendMessage(MessageContext context, params MessageSegment[] segments)
    {
        return SendSegmentsAsync(context, segments);
    }

    protected static Task SendImage(MessageContext context, string base64Image)
    {
        return string.IsNullOrWhiteSpace(base64Image)
            ? Task.CompletedTask
            : SendSegmentsAsync(context, [new ImageMessage(base64Image)]);
    }

    private static async Task SendSegmentsAsync(MessageContext context, MessageSegment[] segments)
    {
        var images = segments.OfType<ImageMessage>()
            .Where(image => !string.IsNullOrWhiteSpace(image.Base64))
            .ToArray();

        var text = string.Concat(segments.OfType<TextMessage>().Select(message => message.Text));

        if (images.Length == 0)
        {
            if (!string.IsNullOrEmpty(text))
                await SendTextAsync(context, text);

            return;
        }

        // 官方 API 的 media 消息可以同时携带 content 和 attachment；单图时把文本一起带上，
        // 这样 Info/B50/娶群友等“图片 + 说明”就是一条消息，而不是两条。
        if (images.Length == 1)
        {
            await SendMediaAsync(context, text, images[0]);
            return;
        }

        // 多图时按顺序处理：每张图片带上前面的文本，最后剩余的文本单独发送。
        var textBuffer = new StringBuilder();

        foreach (var segment in segments)
        {
            switch (segment)
            {
                case TextMessage textMessage:
                    textBuffer.Append(textMessage.Text);
                    break;

                case ImageMessage imageMessage when !string.IsNullOrWhiteSpace(imageMessage.Base64):
                    await SendMediaAsync(context, textBuffer.ToString(), imageMessage);
                    textBuffer.Clear();
                    break;
            }
        }

        if (textBuffer.Length > 0)
            await SendTextAsync(context, textBuffer.ToString());
    }

    private static async Task SendTextAsync(MessageContext context, string text)
    {
        try
        {
            await context.Channel.SendMessageAsync(text, passiveSource: context.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while sending a message to {ChatId}", context.ChatId);
        }
    }

    private static async Task SendMediaAsync(MessageContext context, string? content, ImageMessage image)
    {
        try
        {
            using var attachment = new FileAttachment(Convert.FromBase64String(image.Base64), image.Filename);

            await context.Channel.SendMessageAsync(
                content: string.IsNullOrWhiteSpace(content) ? null : content,
                attachment: attachment,
                passiveSource: context.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while sending a message with an image to {ChatId}", context.ChatId);
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
