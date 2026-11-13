using System.Collections.Concurrent;
using QQBot;
using QQBot.Logging;
using QQBot.Net.Rest;
using QQBot.Net.WebSockets;
using QQBot.WebSocket;
using Sapphire.Commands;
using Sapphire.Environments;
using Serilog;
using Serilog.Events;

namespace Sapphire.Utils;

public static class BotClient
{
    private static readonly ConcurrentDictionary<string, byte> RecentMessageIds = new();
    private static readonly ConcurrentQueue<string> RecentMessageIdQueue = new();

    private static QQBotSocketClient? _client;

    public static QQBotSocketClient? Client => _client;

    public static string? CurrentUserId => _client?.CurrentUser?.Id.ToString();

    public static async Task RunAsync(Action<MessageContext> onMessage)
    {
        var environment = SpEnvironmentHandler.Environment.QQBot;

        if (environment.AppId == 0 || string.IsNullOrWhiteSpace(environment.AppSecret))
            throw new InvalidOperationException("QQBot.AppId / QQBot.AppSecret 未配置。");

        var client = new QQBotSocketClient(new QQBotSocketConfig
        {
            AccessEnvironment = AccessEnvironment.Production,
            GatewayIntents = GatewayIntents.GroupAndC2CEvent,
            StartupCacheFetchMode = StartupCacheFetchMode.Lazy,
            StartupCacheFetchData = StartupCacheFetchData.None,
            MessageCacheSize = 0,
            LogLevel = LogSeverity.Info,
            // 本地调试时通过 HTTPS_PROXY / ALL_PROXY 指定的 SOCKS 代理访问腾讯接口。
            RestClientProvider = DefaultRestClientProvider.Create(useProxy: true),
            WebSocketProvider = DefaultWebSocketProvider.Create(HttpClient.DefaultProxy)
        });

        _client = client;

        client.Log += LogAsync;
        client.Disconnected += exception =>
        {
            Log.Warning(exception, "QQBot gateway disconnected");
            return Task.CompletedTask;
        };
        client.Ready += () =>
        {
            Log.Information("QQBot ready as {UserId}", CurrentUserId);
            return Task.CompletedTask;
        };
        client.MessageReceived += message =>
        {
            var context = CreateContext(message);
            if (context != null)
                onMessage(context);

            return Task.CompletedTask;
        };

        await client.LoginAsync(environment.AppId, TokenType.AppSecret, environment.AppSecret);
        await client.StartAsync();
    }

    public static async Task StopAsync()
    {
        if (_client == null)
            return;

        try
        {
            await _client.StopAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error occurred while stopping the QQBot client");
        }

        try
        {
            await _client.LogoutAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error occurred while logging out the QQBot client");
        }
    }

    private static MessageContext? CreateContext(SocketMessage message)
    {
        if (message is not IUserMessage userMessage)
            return null;

        if (message.Channel is not (SocketGroupChannel or SocketUserChannel))
            return null;

        if (IsDuplicate(message.Id))
            return null;

        var content = message.Content.Trim();
        if (string.IsNullOrEmpty(content))
            return null;

        var isGroup = message.Channel is SocketGroupChannel;
        var sceneUserId = message.Author.Id;

        Log.Information("Received message {MessageId} from {UserId} in {ChatId}: {Content}",
            message.Id, sceneUserId, message.Channel.Id, content);
        var unionOpenId = message.AuthorUnionOpenId;
        var userId = string.IsNullOrWhiteSpace(unionOpenId) ? sceneUserId : unionOpenId;

        return new MessageContext
        {
            IsGroup = isGroup,
            ChatId = message.Channel.Id,
            SceneUserId = sceneUserId,
            UserId = userId,
            MessageId = message.Id,
            Content = content,
            Channel = message.Channel,
            Message = userMessage,
            UserName = message.AuthorUsername,
            GroupNickname = message.AuthorGroupNickname,
            MemberRole = message.AuthorMemberRole,
            UnionOpenId = unionOpenId,
            Avatar = message.AuthorAvatar ?? message.Author.Avatar
        };
    }

    private static bool IsDuplicate(string messageId)
    {
        if (!RecentMessageIds.TryAdd(messageId, 0))
            return true;

        RecentMessageIdQueue.Enqueue(messageId);

        while (RecentMessageIdQueue.Count > 2000 &&
               RecentMessageIdQueue.TryDequeue(out var oldMessageId))
            RecentMessageIds.TryRemove(oldMessageId, out _);

        return false;
    }

    private static Task LogAsync(LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };

        Log.Write(level, message.Exception, "[QQBot/{Source}] {Message}", message.Source, message.Message);

        return Task.CompletedTask;
    }
}
