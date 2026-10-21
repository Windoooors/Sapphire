using System.Text;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Commands;
using Sapphire.Data.Settings;
using Serilog;

namespace Sapphire.Utils;

public class CommandParser(IEnumerable<CommandBase> rootCommands, string[] commandPrefixes)
{
    public async Task ParseAndExecute(CqMessagePostContext context)
    {
        foreach (var rootCommand in rootCommands) await rootCommand.ReceiveDirectly(context);

        if (SettingsDataManager.Instance == null)
            return;

        var commandLine = context.RawMessage;

        if (string.IsNullOrWhiteSpace(commandLine)) return;

        var tokens = Tokenize(commandLine);
        if (tokens.Count == 0) return;

        var (matchedCmd, consumedTokens) = MatchCommand(tokens,
            SettingsDataManager.Instance.GetValue(context, "lite_commands.enabled"));
        if (matchedCmd == null) return;

        if (!SettingsDataManager.Instance.GetValue(context, matchedCmd.ActivationSettingsIdentifier))
            return;

        var rawTokens = tokens.Skip(consumedTokens).ToList();

        var commandContext = ExtractFlagsAndArguments(rawTokens, matchedCmd);

        try
        {
            await matchedCmd.Receive(commandContext, context);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unknown error occurred");
#if DEBUG
            throw;
#endif
        }
    }

    private static CommandContext ExtractFlagsAndArguments(List<string> tokens, CommandBase cmd)
    {
        var context = new CommandContext();

        var positionalTokens = new List<string>();

        var flagLookup = new Dictionary<string, FlagSpec>(StringComparer.OrdinalIgnoreCase);
        if (cmd.SupportedFlags.Count > 0)
            foreach (var flag in cmd.SupportedFlags)
            {
                if (!string.IsNullOrEmpty(flag.ShortName)) flagLookup[flag.ShortName] = flag;
                if (!string.IsNullOrEmpty(flag.LongName)) flagLookup[flag.LongName] = flag;
            }

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (flagLookup.TryGetValue(token, out var spec))
            {
                if (spec.TakesValue)
                {
                    var valTokens = new List<string>();

                    while (i + 1 < tokens.Count && !flagLookup.ContainsKey(tokens[i + 1]))
                    {
                        valTokens.Add(tokens[i + 1]);
                        i++;
                    }

                    context.Flags[spec.Name] = valTokens.Count > 0
                        ? string.Join(" ", valTokens)
                        : string.Empty;
                }
                else
                {
                    context.Flags[spec.Name] = "true";
                }
            }
            else
            {
                positionalTokens.Add(token);
            }
        }

        context.Argument = positionalTokens.Count > 0
            ? string.Join(" ", positionalTokens)
            : null;

        return context;
    }

    private (CommandBase? command, int consumedTokensCount) MatchCommand(List<string> tokens, bool withDirectCommands)
    {
        if (tokens.Count >= 2 && commandPrefixes.Contains(tokens[0]))
        {
            CommandBase? matchedCmd = null;
            var currentPool = rootCommands;
            var matchIndex = 1;

            for (var i = 1; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var match = currentPool.FirstOrDefault(c =>
                    c.CommandHeads.Any(y => string.Equals(y, token, StringComparison.OrdinalIgnoreCase)));

                if (match != null)
                {
                    matchedCmd = match;
                    currentPool = match.SubCommands;
                    matchIndex = i + 1;
                }
                else
                {
                    break;
                }
            }

            if (matchedCmd != null) return (matchedCmd, matchIndex);
        }

        if (withDirectCommands)
        {
            var firstToken = tokens[0];
            var directMatch = FindDirectCommand(rootCommands, firstToken);
            if (directMatch != null) return (directMatch, 1);
        }

        return (null, 0);
    }

    private CommandBase? FindDirectCommand(IEnumerable<CommandBase> commands, string headToken)
    {
        foreach (var cmd in commands)
        {
            if (cmd.DirectCommandHeads.Length > 0 &&
                cmd.DirectCommandHeads.Any(y => string.Equals(y, headToken, StringComparison.OrdinalIgnoreCase)))
                return cmd;
            
            var childMatch = FindDirectCommand(cmd.SubCommands, headToken);
            if (childMatch != null) return childMatch;
        }

        return null;
    }

    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var currentToken = new StringBuilder();
        var inQuotes = false;

        foreach (var c in input)
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (currentToken.Length == 0)
                    continue;

                tokens.Add(currentToken.ToString());
                currentToken.Clear();
            }
            else
            {
                currentToken.Append(c);
            }

        tokens.Add(currentToken.ToString());

        return tokens;
    }
}