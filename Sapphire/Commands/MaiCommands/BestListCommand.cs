using System.Text;
using EleCho.GoCqHttpSdk.Message;
using EleCho.GoCqHttpSdk.Post;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Environments;
using Sapphire.ImageGeneration;
using Sapphire.ImageGeneration.ImageGenerators.MaiCommandImageGenerators;

namespace Sapphire.Commands.MaiCommands;

public class BestListCommand : MaiCommandBase
{
    public BestListCommand()
    {
        DirectCommandHeads = ["b50"];
        CommandHeads = ["b50"];

        ActivationSettingsIdentifier = "mai_b50.enabled";

        SupportedFlags =
        [
            new FlagSpec
            {
                LongName = "--all-perfect",
                ShortName = "-ap",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--all-perfect-plus",
                ShortName = "-app",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--full-combo",
                ShortName = "-fc",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--full-combo-plus",
                ShortName = "-fcp",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--japan",
                ShortName = "-jp",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--play-count",
                ShortName = "-pc",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--fit",
                ShortName = "-f",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--user",
                ShortName = "-u"
            }
        ];
    }

    private static List<(int, MaiPlayRecord)> GetTopRecords(
        ReadOnlySpan<MaiPlayRecord> userRecords,
        Func<int, MaiPlayRecord, MaiSongDto, int, bool> filter,
        Comparison<(int Index, MaiPlayRecord Record, MaiSongDto Song, int ChartIndex)> comparison,
        int count = 35)
    {
        var reversed = Comparer<(int, MaiPlayRecord, MaiSongDto, int)>.Create((a, b) => comparison(b, a));

        var heap = new PriorityQueue<(int, MaiPlayRecord), (int, MaiPlayRecord, MaiSongDto, int)>(reversed);

        for (var i = 0; i < userRecords.Length; i++)
        {
            var maiPlayRecord = userRecords[i];
            var sapphireId = i / 5 + 1;
            var chartIndex = i % 5;

            if (MaiDataManager.Instance == null)
                continue;

            if (!MaiDataManager.Instance.SapphireIdToSong.TryGetValue(sapphireId, out var song))
                continue;

            if (chartIndex >= song.Charts.Count)
                continue;

            if (!filter(i, maiPlayRecord, song, chartIndex) || maiPlayRecord.ClearState == ClearState.NotPlayed ||
                song.SongType == MaiSongType.Utage)
                continue;

            heap.Enqueue((i, maiPlayRecord), (i, maiPlayRecord, song, chartIndex));

            if (heap.Count > count)
                heap.Dequeue();
        }

        var result = new List<(int, MaiPlayRecord)>(heap.Count);
        while (heap.Count > 0)
            result.Add(heap.Dequeue());

        result.Reverse();
        return result;
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, CqMessagePostContext context)
    {
        var useJp = commandContext.HasFlag("--japan");
        var usePc = commandContext.HasFlag("--play-count");
        var useApp = commandContext.HasFlag("--all-perfect-plus");
        var useAp = commandContext.HasFlag("--all-perfect");
        var useFcp = commandContext.HasFlag("--full-combo-plus");
        var useFc = commandContext.HasFlag("--full-combo");
        var useFit = commandContext.HasFlag("--fit");

        var useAssignedUserId = commandContext.HasFlag("--user");
        var userIdString = commandContext.GetFlag("--user");

        var userId = context.UserId;

        if (useAssignedUserId && (userIdString == null || !long.TryParse(userIdString, out userId)))
        {
            await SendMessage(context, ["无效的用户码"]);
            return;
        }

        await ProcessBase(context, userId, (i, record, song, chartIndex) =>
        {
            if (useAp && record.ClearState is not ClearState.AllPerfect and not ClearState.AllPerfectPlus)
                return false;
            if (useApp && record.ClearState is not ClearState.AllPerfectPlus)
                return false;
            if (useFcp && record.ClearState is not ClearState.AllPerfect and not ClearState.AllPerfectPlus
                    and not ClearState.FullComboPlus)
                return false;
            if (useFc && record.ClearState is not ClearState.AllPerfect and not ClearState.AllPerfectPlus
                    and not ClearState.FullComboPlus and not ClearState.FullCombo)
                return false;

            return true;
        }, usePc, useFit, useJp);
    }

    protected override async Task Process(CqMessagePostContext context)
    {
        await ProcessBase(context, context.UserId, (_, _, _, _) => true,
            false, false, false);
    }

    private async Task ProcessBase(CqMessagePostContext context, long userId,
        Func<int, MaiPlayRecord, MaiSongDto, int, bool> filter,
        bool usePc, bool useFit, bool useJpLevel)
    {
        var startTime = DateTime.UtcNow;

        if (MaiDataManager.Instance == null)
        {
            await SendUnknownError(context);
            return;
        }

        var env = SpEnvironmentHandler.Environment.Mai;

        var userRecordBytes = await MaiDataManager.Instance.GetUserPlayRecordsInBytesAsync(userId);

        var sb = new StringBuilder();

        if (userRecordBytes == null)
        {
            sb.Append("无游玩记录");
            await SendMessage(context, [sb.ToString()]);
            return;
        }

        var userRecords = MaiDataManager.Instance.DecodeUserPlayRecords(userRecordBytes);

        var totalPc = 0;
        foreach (var maiPlayRecord in userRecords) totalPc += maiPlayRecord.PlayCount;

        if (usePc && totalPc == 0)
        {
            await SendMessage(context, ["记录中不包含游玩次数信息"]);
            return;
        }

        Comparison<(int Index, MaiPlayRecord Record, MaiSongDto Song, int ChartIndex)> comparison = (a, b) =>
        {
            if (usePc)
                return b.Record.PlayCount
                    .CompareTo(a.Record.PlayCount);

            if (useFit)
                return MaiDataManager.GetRating(b.Song.Charts[b.ChartIndex].FitLevel ?? 0, b.Record.Score)
                    .CompareTo(MaiDataManager.GetRating(a.Song.Charts[a.ChartIndex].FitLevel ?? 0, a.Record.Score));

            return MaiDataManager.GetRating(b.Song.SapphireId, b.ChartIndex, b.Record.Score, useJpLevel)
                .CompareTo(MaiDataManager.GetRating(a.Song.SapphireId, a.ChartIndex, a.Record.Score, useJpLevel));
        };

        var recordsOldSorted = GetTopRecords(userRecords,
            (i, r, song, chartIndex) =>
            {
                var songVersion = useJpLevel ? song.JpVersionIndex : song.CnVersionIndex;

                var indexes = useJpLevel
                    ? env.JpLastestVersionIndexes
                    : env.CnLastestVersionIndexes;

                return indexes.All(x => songVersion >= x) &&
                       filter(i, r, song, chartIndex);
            }, comparison);
        var recordsNewSorted = GetTopRecords(userRecords,
            (i, r, song, chartIndex) =>
            {
                var songVersion = (useJpLevel ? song.JpVersionIndex : song.CnVersionIndex) ?? -1;
                var indexes = useJpLevel
                    ? env.JpLastestVersionIndexes
                    : env.CnLastestVersionIndexes;
                return indexes.Contains(songVersion) && filter(i, r, song, chartIndex);
            }, comparison, 15);

        var queryTime = DateTime.UtcNow - startTime;
        var generationStartTime = DateTime.UtcNow;

        var userName = await GetUserName(userId);

        var dataObject = new BestListData
        {
            NewBestList = recordsNewSorted,
            OldBestList = recordsOldSorted,
            UserName = userName,
            UserId = userId
        };

        var imageResult =
            await ImageGenerationHandler.Instance.AddTask(
                BestListImageGenerator.Generate(dataObject, useJpLevel, useFit));
        var generationTime = DateTime.UtcNow - generationStartTime;
        await SendMessage(context,
        [
            new CqImageMsg("base64://" + imageResult),
            $"查询用时 {queryTime.TotalMilliseconds}ms，图片生成用时 {generationTime.TotalMilliseconds}ms"
        ]);
    }

    public class BestListData
    {
        public required IEnumerable<(int, MaiPlayRecord)> NewBestList { get; init; }
        public required IEnumerable<(int, MaiPlayRecord)> OldBestList { get; init; }
        public required string UserName { get; init; }
        public required long UserId { get; set; }
    }
}