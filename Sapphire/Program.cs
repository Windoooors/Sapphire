using EleCho.GoCqHttpSdk;
using Sapphire.Commands;
using Sapphire.Data.Settings;
using Sapphire.Utils;
using Serilog;

namespace Sapphire;

public static class Program
{
    public static BotSessionHandler BotSessionHandler = new();
    private static CancellationToken _sessionCancellationToken;

    private static SettingsDataManager? _settingsDataManager;

    private static readonly CommandBase[] GroupCommands =
    [
        new PingCommand(), new MaiCommand(), new SettingsCommand(), new ConfirmationCommand(), new NrConvertCommand(),
        new GayFactor(), new GroupMemberCommand()
    ];

    private static readonly string[] CommandPrefixes = ["s"];

    private static CommandParser? _commandParser;

    public static HttpClient HttpClient = new();

    public static async Task Main()
    {
        var keepAlive = new ManualResetEvent(false);

        AppDomain.CurrentDomain.ProcessExit += (_, _) => keepAlive.Set();

        LoggerInitializer.Initialize();

        Log.Information("Initializing commands");

        try
        {
            _settingsDataManager = new SettingsDataManager();

            _sessionCancellationToken = CancellationToken.None;

            foreach (var groupCommandBase in GroupCommands) groupCommandBase.StartInitialize();
        }
        catch (Exception e)
        {
            Log.Error(e, "Error occurred when initializing commands");
#if DEBUG
            throw;
#endif
        }

        Log.Information("Commands initialized");

        try
        {
            await BotSessionHandler.RunSession(_sessionCancellationToken,
                StartParsing);
        }
        catch (Exception e)
        {
            Log.Error(e, "Unknown error occurred");
#if DEBUG
            throw;
#endif
        }

        keepAlive.WaitOne();
    }

    private static void StartParsing(CqWsSession session)
    {
        _commandParser = new CommandParser(GroupCommands, CommandPrefixes);

        session.UseGroupMessage(context => { _ = _commandParser.ParseAndExecute(context); });
        session.UsePrivateMessage(context => { _ = _commandParser.ParseAndExecute(context); });
    }
}