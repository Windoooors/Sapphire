using Sapphire.Commands;
using Sapphire.Data.GroupMember;
using Sapphire.Data.Settings;
using Sapphire.Environments;
using Sapphire.Utils;
using Serilog;
using Timer = Sapphire.Utils.Timer;

namespace Sapphire;

public static class Program
{
    public static Timer Timer = new(SpEnvironmentHandler.Environment.Sapphire.UtcDayTickTime);

    private static SettingsDataManager? _settingsDataManager;

    private static readonly CommandBase[] GroupCommands =
    [
        new PingCommand(), new MaiCommand(), new SettingsCommand(), new ConfirmationCommand(),
        new IcsAssistantCommand(), new WhoAmICommand(),
        new GayFactor(), new GroupMemberCommand()
    ];

    private static CommandParser? _commandParser;

    public static HttpClient HttpClient = new();

    public static async Task Main()
    {
        var keepAlive = new ManualResetEvent(false);

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            foreach (var groupCommand in GroupCommands) groupCommand.OnExitCall();

            try
            {
                BotClient.StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error occurred while stopping the bot client");
            }

            keepAlive.Set();
        };

        LoggerInitializer.Initialize();

        Log.Information("Initializing commands");

        try
        {
            _settingsDataManager = new SettingsDataManager();

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
            await BotClient.RunAsync(StartParsing);
        }
        catch (Exception e)
        {
            Log.Error(e, "Unknown error occurred");
#if DEBUG
            throw;
#else
            return;
#endif
        }

        keepAlive.WaitOne();
    }

    private static void StartParsing(MessageContext context)
    {
        GroupMemberCommandBase.RememberName(context.UserId, context.DisplayName);

        if (context.IsGroup)
            GroupMemberDataManager.Instance?.UpdateUser(context.ChatId, context.UserId, context.SceneUserId,
                context.DisplayName);

        _commandParser ??= new CommandParser(GroupCommands,
            SpEnvironmentHandler.Environment.Sapphire.CommandPrefixes);

        _ = Task.Run(async () =>
        {
            try
            {
                await _commandParser.ParseAndExecute(context);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while parsing a message");
            }
        });
    }
}
