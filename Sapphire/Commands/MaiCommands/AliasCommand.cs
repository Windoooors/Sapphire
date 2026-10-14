using System.Text;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.Dtos;

namespace Sapphire.Commands.MaiCommands;

public class AliasCommand : MaiCommandBase
{
    public AliasCommand()
    {
        CommandHeads = ["alias"];
        DirectCommandHeads = ["别名"];

        SupportedFlags =
        [
            new FlagSpec
            {
                LongName = "--add",
                ShortName = "-a",
                TakesValue = true
            },
            new FlagSpec
            {
                LongName = "--remove",
                ShortName = "-rm",
                TakesValue = true
            }
        ];

        ActivationSettingsIdentifier = "mai_alias.enabled";
    }

    private async Task AddAlias(CommandContext commandContext, CqMessagePostContext context)
    {
        var aliasToBeAdded = commandContext.GetFlag("--add")?.Trim();
        if (string.IsNullOrEmpty(aliasToBeAdded))
        {
            await SendArgumentError(context);
            return;
        }

        var songString = commandContext.Argument;
        if (MaiDataManager.Instance == null || !TryGetSongsByString(songString ?? "", out var songs) ||
            songs == null || ConfirmationCommand.Instance == null)
        {
            await SendUnknownError(context);
            return;
        }

        if (songs.Length == 0)
        {
            await SendMessage(context, ["未找到匹配的乐曲"]);
            return;
        }

        if (songs.Length > 1)
        {
            await SendMessage(context,
            [
                BuildSongListString(songs, $"s mai alias SID{songs[0].SapphireId} --add {aliasToBeAdded}",
                    $"为乐曲 {NormalizeSongNameString(songs[0])} 添加别名 \"{aliasToBeAdded}\"")
            ]);
            return;
        }

        await ConfirmationCommand.Instance.AddConfirmation(async () =>
            {
                switch (await MaiDataManager.Instance.TryAddAlias(songs[0], aliasToBeAdded, context.UserId))
                {
                    case true:
                        await SendMessage(context, ["已添加"]);
                        break;
                    case false:
                        await SendMessage(context, ["已存在该别名"]);
                        break;
                }
            }, async () => { await SendMessage(context, ["已取消"]); },
            $"为乐曲 {NormalizeSongNameString(songs[0])} 添加别名 \"{aliasToBeAdded}\"", context, context.UserId);
    }

    private async Task RemoveAlias(CommandContext commandContext, CqMessagePostContext context)
    {
        var aliasToBeDeleted = commandContext.GetFlag("--remove")?.Trim();
        if (!uint.TryParse(aliasToBeDeleted, out var aliasId))
        {
            var aliasIdString = aliasToBeDeleted?.ToLower().Replace("aid", "").Replace("id", "");

            if (!uint.TryParse(aliasIdString, out aliasId))
            {
                await SendArgumentError(context);
                return;
            }
        }

        if (MaiDataManager.Instance == null || ConfirmationCommand.Instance == null)
        {
            await SendUnknownError(context);
            return;
        }

        if (!MaiDataManager.Instance.AliasIdToAlias.TryGetValue((int)aliasId, out var alias))
        {
            await SendMessage(context, ["未找到指定的别名"]);
            return;
        }

        await ConfirmationCommand.Instance.AddConfirmation(async () =>
            {
                switch (await MaiDataManager.Instance.TryRemoveAlias((int)aliasId, context.UserId))
                {
                    case true:
                        await SendMessage(context, ["已删除"]);
                        break;
                    case false:
                        await SendMessage(context, ["删除失败，您无权删除该别名"]);
                        break;
                }
            }, async () => { await SendMessage(context, ["已取消"]); },
            $"为乐曲 {NormalizeSongNameString(alias.Song)} 删除别名 \"{alias.Alias}\"", context, context.UserId);
    }

    private async Task QueryAliases(CommandContext commandContext, CqMessagePostContext context)
    {
        if (MaiDataManager.Instance == null || !TryGetSongsByString(commandContext.Argument ?? "", out var songs) ||
            songs == null)
        {
            await SendUnknownError(context);
            return;
        }

        if (songs.Length == 0)
        {
            await SendMessage(context, ["未找到匹配的乐曲"]);
            return;
        }

        if (songs.Length > 1)
        {
            await SendMessage(context,
                [BuildSongListString(songs, $"s mai alias SID{songs[0].SapphireId}", "查询列表中第一首乐曲的别名")]);
            return;
        }

        var aliases = MaiDataManager.Instance.SongToAliases[songs[0]];

        await SendMessage(context, [BuildAliasListString(songs[0], aliases)]);
    }

    private static string BuildAliasListString(MaiSongDto songDto, IEnumerable<MaiAliasDto> aliases)
    {
        var aliasesArray = aliases.ToArray();

        var stringBuilder = new StringBuilder($"歌曲 {NormalizeSongNameString(songDto)}");

        if (aliasesArray.Length == 0)
        {
            stringBuilder.Append(" 没有别名");
            return stringBuilder.ToString().Trim();
        }

        stringBuilder.AppendLine(" 有如下别名：");

        foreach (var alias in aliasesArray) stringBuilder.AppendLine($"AID{alias.AliasId}. {alias.Alias}");

        return stringBuilder.ToString().Trim();
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        if (commandContext.HasFlag("--add"))
        {
            await AddAlias(commandContext, context);
            return;
        }

        if (commandContext.HasFlag("--remove"))
        {
            await RemoveAlias(commandContext, context);
            return;
        }

        await QueryAliases(commandContext, context);
    }
}