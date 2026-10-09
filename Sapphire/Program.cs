using System.Text.RegularExpressions;
using EleCho.GoCqHttpSdk;
using Sapphire.Commands;
using Sapphire.Commands.Settings;
using Sapphire.Data.Settings;
using Sapphire.Environments;
using Sapphire.Utils;
using Serilog;

namespace Sapphire;

public static class Program
{
    public static BotSessionHandler? BotSessionHandler;
    private static CancellationToken _sessionCancellationToken;
    private static SpEnvironmentHandler? _environmentHandler;
    
    private static SettingsDataManager? _settingsDataManager = new ();

    private static readonly CommandBase[] GroupCommands = [new PingCommand(), new MaiCommand(), new SettingsCommand(), new ConfirmationCommand(), new NRConvertCommand()];

    private static readonly string[] CommandPrefixes = ["s"];
    
    private static CommandParser? _commandParser;
    
    public static HttpClient HttpClient = new ();

    public static async Task Main()
    {
        var keepAlive = new ManualResetEvent(false);

        AppDomain.CurrentDomain.ProcessExit += (_, _) => keepAlive.Set();
        
        LoggerInitializer.Initialize();
        
        Log.Information("Sapphire is initializing commands...");
        
        _environmentHandler = new SpEnvironmentHandler();
        
        BotSessionHandler = new BotSessionHandler();

        _sessionCancellationToken = CancellationToken.None;
        
        foreach (var groupCommandBase in GroupCommands)
        {
            groupCommandBase.StartInitialize();
        }
        
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