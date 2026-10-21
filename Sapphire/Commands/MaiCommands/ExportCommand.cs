using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.DivingFishMai.Dtos;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Environments;

namespace Sapphire.Commands.MaiCommands;

public class ExportCommand : MaiCommandBase
{
    public ExportCommand()
    {
        CommandHeads = ["export"];
        DirectCommandHeads = ["export", "导出分数"];

        SupportedFlags =
        [
            new FlagSpec()
            {
                ShortName = "-df",
                LongName = "--diving-fish",
            }
        ];
    }

    private async Task ExportDf(CqMessagePostContext context)
    {
        if (MaiDataManager.Instance == null || DivingFishMaiDataManager == null)
        {
            await SendUnknownError(context);
            return;
        }
        
        var userRecordBytes =
            await MaiDataManager.Instance.GetUserPlayRecordsInBytesAsync(context.UserId);

        if (userRecordBytes == null)
        {
            await SendMessage(context, ["无游玩记录"]);
            return;
        }
        
        var userRecords = MaiDataManager.Instance.DecodeUserPlayRecords(userRecordBytes);

        var uploadContents = new List<DfUserRecordUploadDto>();

        for (var i = 0; i < userRecords.Length; i++)
        {
            var sapphireId = i / 5 + 1;
            var chartId = i % 5;
            
            var record = userRecords[i];

            if (record.ClearState == ClearState.NotPlayed)
                continue;

            if (!MaiDataManager.Instance.SapphireIdToSong.TryGetValue(sapphireId, out var song) ||
                song.Charts.Count - 1 < chartId)
                continue;
            
            var chart = song.Charts[chartId];
            var result = DfUserRecordUploadDto.ConvertFromMaiPlayRecord(record, chart);

            if (result != null)
                uploadContents.Add(result);
        }
        
        var uploadContentsArray = uploadContents.ToArray();
        if (uploadContentsArray.Length == 0)
        {
            await SendMessage(context, ["无游玩记录"]);
            return;
        }

        var uploadResult = await DivingFishMaiDataManager.UploadUserRecordsAsync(uploadContentsArray, context.UserId);

        if (!uploadResult)
        {
            await SendMessage(context,
                [$"{SpEnvironmentHandler.Environment.Sapphire.BotName} 无权更新您的成绩，请发送 \"bind -df\" 指令重新绑定授权后再试"]);
            return;
        }
        
        await SendMessage(context,
            [$"上传了 {uploadContentsArray.Length} 项游玩记录（包括已删除曲目）"]);
    }
    
    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        if (commandContext.HasFlag("--diving-fish"))
        {
            await ExportDf(context);

            return;
        }
        await SendArgumentError(context);
    }
}