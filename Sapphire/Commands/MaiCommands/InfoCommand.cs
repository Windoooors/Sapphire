using System.Text;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.Dtos;

namespace Sapphire.Commands.MaiCommands;

public class InfoCommand : MaiCommandBase
{
    public InfoCommand()
    {
        DirectCommandHeads = ["info"];
        CommandHeads = ["info"];
        
        ActivationSettingsIdentifier = "mai_info.enabled";
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        if (MaiDataManager.Instance == null)
        {
            await SendUnknownError(context);
            return;
        }
        
        if (commandContext.Argument == null)
        {
            await SendArgumentError(context);
            return;
        }

        if (!TryGetSongsByString(commandContext.Argument, out var songs) || songs == null || songs.Length == 0)
        {
            await SendMessage(context, ["未找到匹配的乐曲"]);
            return;
        }

        if (songs.Length > 1)
        {
            await SendMessage(context, [BuildSongListString(songs, $"info SID{songs[0].SapphireId}", "查询列表中第一首乐曲的信息")]);
            return;
        }
        
        var song = songs[0];

        var sb = new StringBuilder();
        sb.AppendLine($"曲名：{song.SongName ?? "未知"}");
        sb.AppendLine($"艺术家：{song.ArtistName ?? "未知"}");
        sb.AppendLine($"BPM: {song.SongBpm.ToString() ?? "未知"}");

        var userRecordBytes = await MaiDataManager.Instance.GetUserPlayRecordsInBytesAsync(context.UserId);
        
        if (userRecordBytes != null)
        {
            var userRecords = MaiDataManager.Instance.DecodeUserPlayRecords(userRecordBytes);

            sb.AppendLine();
            for (var i = 0; i < song.Charts.Count; i++)
            {
                var index = i + (song.SapphireId - 1) * 5;

                if (userRecords.Length <= index || userRecords[index].ClearState == ClearState.NotPlayed)
                {
                    continue;
                }

                sb.AppendLine($"难度 {i}. {song.Charts[i].CnLevel}: {userRecords[index].Score}");
            }
        }
        
        await SendMessage(context, [sb.ToString().Trim()]);
    }
}