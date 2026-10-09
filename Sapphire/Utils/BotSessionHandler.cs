using EleCho.GoCqHttpSdk;
using Sapphire.Environments;
using Sapphire.Environments.SubEnvironments;
using Serilog;

namespace Sapphire.Utils;

public class BotSessionHandler
{
    private static DateTime _lastHeartbeatTime;
    private static bool _heartBeating;

    private readonly NapCatEnvironment _napCatEnvironment =
        SpEnvironmentHandler.Instance?.Environment.NapCat ?? new NapCatEnvironment();

    public CqWsSession? Session;

    private static CqWsSession GetSession()
    {
        var napCatEnvironment = SpEnvironmentHandler.Instance?.Environment.NapCat ?? new NapCatEnvironment();

        return new CqWsSession(new CqWsSessionOptions
        {
            BaseUri = new Uri("ws://" + napCatEnvironment.Ip + ":" +
                              napCatEnvironment.Port),
            AccessToken = napCatEnvironment.Token
        });
    }

    public async Task RunSession(CancellationToken cancellationToken, Action<CqWsSession> onSessionConnected)
    {
        _lastHeartbeatTime = DateTime.UtcNow;

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if ((DateTime.UtcNow - _lastHeartbeatTime).TotalMilliseconds >= _napCatEnvironment.HeartBeatInterval)
                _heartBeating = false;

            if (Session == null || !Session.IsConnected || !_heartBeating)
            {
                Log.Information("Sapphire is trying to connect with NapCat...");
                
                if (Session != null)
                    await Session.StopAsync();

                Session?.Dispose();

                Session = GetSession();

                var sessionStartTask = Session.StartAsync();
                var delayTask = Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

                var completedTask = await Task.WhenAny(sessionStartTask, delayTask);

                if (completedTask == delayTask) continue;

                if (Session.IsConnected)
                {
                    if (!_heartBeating)
                    {
                        onSessionConnected?.Invoke(Session);
                        
                        Log.Information("Sapphire is connected with NapCat.");
                    }

                    _heartBeating = true;
                    _lastHeartbeatTime = DateTime.UtcNow;
                }

                Session.UseHeartbeat(async (_, next) =>
                {
                    _lastHeartbeatTime = DateTime.UtcNow;

                    await next.Invoke();
                });
            }

            Thread.Sleep(1000);
        }
    }
}