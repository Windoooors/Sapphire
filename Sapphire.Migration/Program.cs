using System.Net.WebSockets;
using System.Text.Json;

namespace Sapphire.Migration;

public static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<int> Main(string[] args)
    {
        var configPath = GetConfigPath(args);
        var config = MigrationConfigLoader.Load(configPath);

        if (config == null)
            return 2;

        using var cancellationTokenSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        await RunAsync(config, cancellationTokenSource.Token);

        return 0;
    }

    private static async Task RunAsync(MigrationConfig config, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var socket = new ClientWebSocket();
                socket.Options.Proxy = null;
                socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

                var url = BuildUrl(config.Bot.WebSocketUrl, config.Bot.Token);
                await socket.ConnectAsync(new Uri(url), cancellationToken);

                Log($"已连接主 bot：{url}");

                await ReceiveLoopAsync(socket, config, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"连接/通信失败：{ex.Message}");
            }

            if (!cancellationToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }

    private static async Task ReceiveLoopAsync(ClientWebSocket socket, MigrationConfig config,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            using var memoryStream = new MemoryStream();

            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                    return;

                memoryStream.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            if (memoryStream.Length == 0)
                continue;

            MigrationRequest? request;

            try
            {
                request = JsonSerializer.Deserialize<MigrationRequest>(memoryStream.ToArray(), JsonOptions);
            }
            catch (Exception ex)
            {
                Log($"无法解析迁移请求：{ex.Message}");
                continue;
            }

            if (request == null || request.Type != "migrate")
                continue;

            MigrationResponse response;

            try
            {
                response = await MigrationHandler.MigrateAsync(request, config, cancellationToken);
            }
            catch (Exception ex)
            {
                response = new MigrationResponse
                {
                    RequestId = request.RequestId,
                    Success = false,
                    Message = ex.Message
                };
            }

            var payload = JsonSerializer.SerializeToUtf8Bytes(response, JsonOptions);
            await socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);

            Log($"迁移请求 {request.RequestId} 完成：success={response.Success}, " +
                $"scores={response.Scores}, aliases={response.Aliases}, " +
                $"members={response.Members}, settings={response.Settings}");
        }
    }

    private static string BuildUrl(string url, string token)
    {
        if (string.IsNullOrEmpty(token))
            return url;

        return url + (url.Contains('?') ? "&" : "?") + "token=" + Uri.EscapeDataString(token);
    }

    private static string GetConfigPath(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
            if (args[i] is "--config" or "-c" && i + 1 < args.Length)
                return args[i + 1];

        return Path.Combine(AppContext.BaseDirectory, "config.toml");
    }

    private static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}
