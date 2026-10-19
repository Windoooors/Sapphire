using System.Net;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.DivingFishMai;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Data.Wck;
using Serilog;

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
            new FlagSpec
            {
                LongName = "--divingfish",
                ShortName = "-df"
            },
            new FlagSpec
            {
                LongName = "--wahlap",
                ShortName = "-wl"
            }
        ];
    }

    private async Task UpdateDf(CqMessagePostContext context)
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
            if (!MaiDataManager.Instance.IdToSong.TryGetValue(record.SongId, out var song) ||
                song.SongType == MaiSongType.Utage)
                continue;

            var index = (song.SapphireId - 1) * 5 + record.LevelIndex;
            result[index] = record.ToMaiPlayRecord();
            count++;
        }

        var succeeded = await MaiDataManager.Instance.InsertUserPlayRecords(result, context.UserId);

        if (succeeded)
            await SendMessage(context, [$"已成功更新 {count} 张谱面的游玩数据"]);
        else
            await SendUnknownError(context);
    }

    private async Task UpdateWahlap(CqMessagePostContext context)
    {
        try
        {
            var sessionId = await WckDataManager.GetSessionId(context);

            if (sessionId == null) return;

            await SendMessage(context, [
                "正在尝试更新成绩，请稍等"
            ]);

            var records = await WckDataManager.GetUserScores(sessionId);

            if (records == null)
            {
                await SendNotLoggedInError(context);
                return;
            }

            if (MaiDataManager.Instance == null)
            {
                await SendUnknownError(context);
                return;
            }

            var result = new MaiPlayRecord[MaiDataManager.Instance.Songs.Length * 5];

            var count = 0;

            foreach (var record in records)
            {
                if (!MaiDataManager.Instance.IdToSong.TryGetValue(record.Id, out var song) ||
                    song.SongType == MaiSongType.Utage)
                    continue;

                var index = (song.SapphireId - 1) * 5 + record.Level;
                result[index] = record.ToMaiPlayRecord();
                count++;
            }

            var succeeded = await MaiDataManager.Instance.InsertUserPlayRecords(result, context.UserId);

            if (succeeded)
                await SendMessage(context, [$"已成功更新 {count} 张谱面的游玩数据"]);
            else
                await SendUnknownError(context);
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException httpEx && httpEx.StatusCode != HttpStatusCode.OK)
            {
                await SendMessage(context, [
                    "与服务器通信时出现问题"
                ]);
                return;
            }

            await SendUnknownError(context);
            Log.Error(ex, "Error occurred while updating user records from wahlap");
        }
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        if (commandContext.HasFlag("--divingfish"))
        {
            await UpdateDf(context);

            return;
        }

        if (commandContext.HasFlag("--wahlap"))
        {
            await UpdateWahlap(context);
            return;
        }

        await SendArgumentError(context);
    }
}