using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using Sapphire.Environments;
using Serilog;

namespace Sapphire.Utils;

public sealed record MigrationRequest
{
    public string Type { get; init; } = "migrate";
    public required string RequestId { get; init; }
    public required long QqNumber { get; init; }
    public required string CanonicalUserId { get; init; }
    public required string SceneUserId { get; init; }
    public required string ChatId { get; init; }
    public bool IsGroup { get; init; } = true;
    public long LegacyGroupId { get; init; }
}

public sealed record MigrationResponse
{
    public string Type { get; init; } = "completed";
    public required string RequestId { get; init; }
    public required bool Success { get; init; }
    public string? Message { get; init; }
    public int Scores { get; init; }
    public int Aliases { get; init; }
    public int Members { get; init; }
    public int Settings { get; init; }
}

/// <summary>
///     主 bot 侧的本机 WebSocket 服务端。迁移项目连接后，bot 把迁移请求发过去，
///     迁移项目完成写库后回传结果。
/// </summary>
public static class MigrationServer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly ConcurrentDictionary<Guid, ClientConnection> Clients = new();
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<MigrationResponse>> Pending = new();

    private static HttpListener? _listener;
    private static CancellationTokenSource? _cancellation;

    public static bool IsRunning => _listener?.IsListening == true;

    public static int ConnectedClients => Clients.Count;

    public static Task StartAsync()
    {
        var environment = SpEnvironmentHandler.Environment.Migration;

        if (!environment.Enabled || _listener != null)
            return Task.CompletedTask;

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{environment.Port}/migration/");
        listener.Start();

        _listener = listener;
        _cancellation = new CancellationTokenSource();

        _ = Task.Run(() => AcceptLoopAsync(listener, _cancellation.Token));

        Log.Information("Migration WebSocket server listening on ws://127.0.0.1:{Port}/migration/",
            environment.Port);

        return Task.CompletedTask;
    }

    public static async Task<MigrationResponse?> RequestAsync(MigrationRequest request, TimeSpan timeout)
    {
        var client = Clients.Values.FirstOrDefault();

        if (client == null)
            return null;

        var completion = new TaskCompletionSource<MigrationResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Pending[request.RequestId] = completion;

        try
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(request, JsonOptions);
            await client.SendAsync(payload);

            return await completion.Task.WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            Log.Warning("Migration request {RequestId} timed out after {Timeout}", request.RequestId, timeout);
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while sending migration request {RequestId}", request.RequestId);
            return null;
        }
        finally
        {
            Pending.TryRemove(request.RequestId, out _);
        }
    }

    public static Task StopAsync()
    {
        _cancellation?.Cancel();
        _listener?.Stop();
        _listener?.Close();
        _listener = null;

        foreach (var client in Clients.Values)
            client.Dispose();

        Clients.Clear();

        return Task.CompletedTask;
    }

    private static async Task AcceptLoopAsync(HttpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;

            try
            {
                context = await listener.GetContextAsync();
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Migration server failed to accept a connection");
                continue;
            }

            _ = Task.Run(() => HandleClientAsync(context, cancellationToken), cancellationToken);
        }
    }

    private static async Task HandleClientAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var environment = SpEnvironmentHandler.Environment.Migration;

        try
        {
            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.Close();
                return;
            }

            if (!string.IsNullOrEmpty(environment.Token) &&
                !string.Equals(context.Request.QueryString["token"], environment.Token, StringComparison.Ordinal))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.Close();
                return;
            }

            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var connection = new ClientConnection(webSocketContext.WebSocket);
            var id = Guid.NewGuid();

            Clients[id] = connection;
            Log.Information("Migration client {ClientId} connected", id);

            try
            {
                await ReadLoopAsync(connection, cancellationToken);
            }
            finally
            {
                Clients.TryRemove(id, out _);
                connection.Dispose();
                Log.Information("Migration client {ClientId} disconnected", id);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Migration client handling failed");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.Close();
        }
    }

    private static async Task ReadLoopAsync(ClientConnection connection, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        while (connection.Socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            using var memoryStream = new MemoryStream();

            WebSocketReceiveResult result;

            do
            {
                result = await connection.Socket.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                    return;

                memoryStream.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            if (memoryStream.Length == 0)
                continue;

            try
            {
                var response = JsonSerializer.Deserialize<MigrationResponse>(memoryStream.ToArray(), JsonOptions);

                if (response == null || string.IsNullOrEmpty(response.RequestId))
                    continue;

                if (Pending.TryRemove(response.RequestId, out var completion))
                    completion.TrySetResult(response);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to parse migration response");
            }
        }
    }

    private sealed class ClientConnection(WebSocket socket) : IDisposable
    {
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public WebSocket Socket { get; } = socket;

        public async Task SendAsync(byte[] payload)
        {
            await _sendLock.WaitAsync();

            try
            {
                await Socket.SendAsync(payload, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void Dispose()
        {
            try
            {
                Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None)
                    .GetAwaiter().GetResult();
            }
            catch
            {
                // ignored
            }

            Socket.Dispose();
            _sendLock.Dispose();
        }
    }
}
