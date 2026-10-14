using EleCho.GoCqHttpSdk;
using Sapphire.Commands;
using Sapphire.Commands.Settings;
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
        [new PingCommand(), new MaiCommand(), new SettingsCommand(), new ConfirmationCommand(), new NrConvertCommand()];

    private static readonly string[] CommandPrefixes = ["s"];

    private static CommandParser? _commandParser;

    public static HttpClient HttpClient = new();

    public static async Task Main()
    {
        var keepAlive = new ManualResetEvent(false);

        AppDomain.CurrentDomain.ProcessExit += (_, _) => keepAlive.Set();

        LoggerInitializer.Initialize();

        Log.Information("Sapphire is initializing commands...");

        _settingsDataManager = new SettingsDataManager();

        _sessionCancellationToken = CancellationToken.None;

        foreach (var groupCommandBase in GroupCommands) groupCommandBase.StartInitialize();

        Log.Information("Commands initialized.");

        await BotSessionHandler.RunSession(_sessionCancellationToken,
            StartParsing);

        keepAlive.WaitOne();
    }

    private static void StartParsing(CqWsSession session)
    {
        _commandParser = new CommandParser(GroupCommands, CommandPrefixes);

        session.UseGroupMessage(async context => { await _commandParser.ParseAndExecute(context); });
        session.UsePrivateMessage(async context => { await _commandParser.ParseAndExecute(context); });
    }
}