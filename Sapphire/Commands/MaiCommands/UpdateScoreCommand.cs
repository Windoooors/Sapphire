using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.Database;
using Sapphire.Data.Mai.DivingFishDataManagement;
using Sapphire.Data.Mai.Dtos;

namespace Sapphire.Commands.MaiCommands;

public class UpdateScoreCommand : MaiCommandBase
{
    public UpdateScoreCommand()
    {
        DirectCommandHeads = ["update"];
        CommandHeads = ["update"];
        
        ActivationSettingsIdentifier = "mai_update.enabled";

        SupportedFlags =
        [
            new FlagSpec()
            {
                LongName = "--divingfish",
                ShortName = "-df",
            }
        ];
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        if (commandContext.HasFlag("--divingfish"))
        {
            if (DivingFishMaiDataManager.Instance == null)
            {
                await SendUnknownError(context);
                return;
            }
            
            var records = await DivingFishMaiDataManager.Instance.GetUserPlayRecordsAsync(context.UserId);

            if (records == null)
            {
                await SendUnauthorizedError(context);
                return;
            }

            if (MaiDataManager.Instance == null)
            {
                await SendUnknownError(context);
                return;
            }

            var result = new MaiPlayRecord[MaiDataManager.Instance.Songs.Length * 5];

            var count = 0;
            
            foreach (var record in records.Records)
            {
                if (!MaiDataManager.Instance.IdToSong.TryGetValue(record.SongId, out var song) || song.SongType == MaiSongType.Utage)
                    continue;
                
                var index = (song.SapphireId - 1) * 5 + record.LevelIndex;
                result[index] = record.ToMaiPlayRecord();
                count++;
            }

            var succeeded = await MaiDataManager.Instance.InsertUserPlayRecords(result, context.UserId);

            if (succeeded)
            {
                await SendMessage(context, [$"已成功更新 {count} 张谱面的游玩数据"]);
            }
            else
            {
                await SendUnknownError(context);
            }
            
            return;
        }

        await SendArgumentError(context);
    }
}