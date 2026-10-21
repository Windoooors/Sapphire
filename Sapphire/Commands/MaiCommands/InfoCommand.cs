using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.Dtos;
using Sapphire.ImageGeneration;
using Sapphire.ImageGeneration.ImageGenerators.MaiCommandImageGenerators;

namespace Sapphire.Commands.MaiCommands;

public class InfoCommand : MaiCommandBase
{
    public InfoCommand()
    {
        DirectCommandHeads = ["info"];
        CommandHeads = ["info"];

        ActivationSettingsIdentifier = "mai_info.enabled";

        SupportedFlags =
        [
            new FlagSpec
            {
                LongName = "--user",
                ShortName = "-u",
                TakesValue = true
            },
            new FlagSpec
            {
                LongName = "--score",
                ShortName = "-s",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--japan",
                ShortName = "-jp",
                TakesValue = false
            }
        ];
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        var startTime = DateTime.UtcNow;

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

        string? imageString;
        
        var song = songs[0];

        if (commandContext.HasFlag("--score"))
        {
            var useAssignedUserId = commandContext.HasFlag("--user");
            var userIdString = commandContext.GetFlag("--user");

            var userId = context.UserId;

            if (useAssignedUserId && (userIdString == null || !long.TryParse(userIdString, out userId)))
            {
                await SendMessage(context, ["无效的用户码"]);
                return;
            }

            var userRecordBytes =
                await MaiDataManager.Instance.GetUserPlayRecordsInBytesAsync(
                    useAssignedUserId ? userId : context.UserId);

            var queryTime = DateTime.UtcNow - startTime;
            var generationStartTime = DateTime.UtcNow;

            var userName = await GetUserName(useAssignedUserId ? userId : context.UserId);

            if (userRecordBytes != null)
            {
                var userRecords = MaiDataManager.Instance.DecodeUserPlayRecords(userRecordBytes);

                var songRecords = new List<MaiPlayRecord>();

                for (var i = 0; i < song.Charts.Count; i++)
                {
                    var index = (song.SapphireId - 1) * 5 + i;
                    songRecords.Add(userRecords[index]);
                }

                imageString = await ImageGenerationHandler.Instance.AddTask(InfoImageGenerator.Generate(song,
                    [.. songRecords],
                    useAssignedUserId ? userId : context.UserId, userName, commandContext.HasFlag("--japan")));
                
                await SendMessage(context,
                [
                    new CqImageMsg("base64://" + imageString),
                    $"查询用时 {queryTime.TotalMilliseconds}ms，图片生成用时 {(DateTime.UtcNow - generationStartTime).TotalMilliseconds}ms"
                ]);
                return;
            }

            await SendMessage(context, ["无游玩记录"]);
        }
        else
        {
            var queryTime = DateTime.UtcNow - startTime;
            var generationStartTime = DateTime.UtcNow;

            imageString = await ImageGenerationHandler.Instance.AddTask(InfoImageGenerator.Generate(song, null,
                null, null, null));

            await SendMessage(context,
            [
                new CqImageMsg("base64://" + imageString),
                $"查询用时 {queryTime.TotalMilliseconds}ms，图片生成用时 {(DateTime.UtcNow - generationStartTime).TotalMilliseconds}ms"
            ]);
        }
    }
}