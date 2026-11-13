using System.Text;
using Sapphire.Environments;

namespace Sapphire.Commands.MaiCommands;

public class ToleranceCommand : MaiCommandBase
{
    public ToleranceCommand()
    {
        CommandHeads = ["tolerance"];
        DirectCommandHeads = ["tolerance", "容错"];
        ActivationSettingsIdentifier = "mai_tolerance.enabled";
        SupportedFlags =
        [
            new FlagSpec
            {
                LongName = "--sssp",
                ShortName = "-sssp",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--sss",
                ShortName = "-sss",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--ssp",
                ShortName = "-ssp",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--ss",
                ShortName = "-ss",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--sp",
                ShortName = "-sp",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--s",
                ShortName = "-s",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--custom",
                ShortName = "-c",
                TakesValue = true
            },
            new FlagSpec
            {
                LongName = "--detailed",
                ShortName = "-d",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--remaster",
                ShortName = "-rem",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--master",
                ShortName = "-mas",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--expert",
                ShortName = "-exp",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--advanced",
                ShortName = "-adv",
                TakesValue = false
            },
            new FlagSpec
            {
                LongName = "--basic",
                ShortName = "-bas",
                TakesValue = false
            }
        ];
    }

    protected override async Task ProcessWithArguments(CommandContext commandContext, MessageContext context)
    {
        if (string.IsNullOrEmpty(commandContext.Argument))
        {
            await SendArgumentError(context);
            return;
        }

        if (!TryGetSongsByString(commandContext.Argument, out var songs) || songs == null)
        {
            await SendUnknownError(context);
            return;
        }

        if (songs.Length == 0)
        {
            await SendSongNotFoundError(context);
            return;
        }

        if (songs.Length != 1)
        {
            await SendMessage(context,
            [
                BuildSongListString(songs,
                    $"{SpEnvironmentHandler.Environment.Sapphire.CommandPrefixes[0]} mai tolerance SID{songs[0].SapphireId} -sssp",
                    $"查询歌曲 {NormalizeSongNameString(songs[0])} 的 SSS+ 分数线")
            ]);
            return;
        }

        var song = songs[0];

        var charts = song.Charts;

        var levelIndex = 3;

        if (commandContext.HasFlag("--remaster"))
            levelIndex = 4;
        if (commandContext.HasFlag("--master"))
            levelIndex = 3;
        if (commandContext.HasFlag("--expert"))
            levelIndex = 2;
        if (commandContext.HasFlag("--advanced"))
            levelIndex = 1;
        if (commandContext.HasFlag("--basic"))
            levelIndex = 0;

        if (charts.Count - 1 < levelIndex)
        {
            await SendMessage(context, ["不支持的难度参数"]);
            return;
        }

        var achievement = 100.5f;

        if (commandContext.HasFlag("--custom"))
        {
            var achievementString = commandContext.GetFlag("--custom");

            achievement = float.TryParse(achievementString, out var tempAchievement) ? tempAchievement : achievement;
        }

        if (commandContext.HasFlag("--sssp"))
            achievement = 100.5f;
        if (commandContext.HasFlag("--sss"))
            achievement = 100f;
        if (commandContext.HasFlag("--ssp"))
            achievement = 99.5f;
        if (commandContext.HasFlag("--ss"))
            achievement = 99f;
        if (commandContext.HasFlag("--sp"))
            achievement = 98f;
        if (commandContext.HasFlag("--s"))
            achievement = 97f;

        if (achievement < 0 || achievement > 101)
        {
            await SendMessage(context, ["不支持的达成率参数"]);
            return;
        }

        var chart = charts[levelIndex];

        var stringBuilder =
            new StringBuilder(
                $"谱面详情：\nTAP 数量：{chart.TapCount ?? 0}\nHOLD 数量：{chart.HoldCount ?? 0}\n" +
                $"SLIDE 数量：{chart.SlideCount ?? 0}\n" +
                $"TOUCH 数量：{chart.TouchCount ?? 0}\nBREAK 数量：{chart.BreakCount ?? 0}\n");

        var totalBasicScore = ((chart.TapCount ?? 0) + (chart.TouchCount ?? 0)) * 500 +
                              (chart.HoldCount ?? 0) * 1000 + (chart.SlideCount ?? 0) * 1500 +
                              (chart.BreakCount ?? 0) * 2500;
        var totalBreakScore = (chart.BreakCount ?? 0) * 100;
        var totalScore = totalBasicScore + totalBreakScore;

        var singleMissTapAchievementLoss = 50000f / totalBasicScore;
        var singleGoodTapAchievementLoss = 25000f / totalBasicScore;
        var singleGreatTapAchievementLoss = 10000f / totalBasicScore;

        stringBuilder.AppendLine($"（FiNALE）总分数：{totalScore}");

        stringBuilder.AppendLine($"\n在其余音符所得分数均为最高的情况下，要达到 {achievement:F4}%，该谱面允许：");

        var currentAchievement = 101f;
        var goodCount = 0;
        while (currentAchievement - singleGoodTapAchievementLoss > achievement)
        {
            currentAchievement -= singleGoodTapAchievementLoss;
            goodCount++;
        }

        currentAchievement = 101f;
        var greatCount = 0;
        while (currentAchievement - singleGreatTapAchievementLoss > achievement)
        {
            currentAchievement -= singleGreatTapAchievementLoss;
            greatCount++;
        }

        currentAchievement = 101f;
        var missCount = 0;
        while (currentAchievement - singleMissTapAchievementLoss > achievement)
        {
            currentAchievement -= singleMissTapAchievementLoss;
            missCount++;
        }

        stringBuilder.AppendLine(
            $"\n打出 {greatCount} 个 GREAT TAP （-{singleGreatTapAchievementLoss:F4}%/GREAT (400) TAP）");
        stringBuilder.AppendLine(
            $"或打出 {goodCount} 个 GOOD TAP （-{singleGoodTapAchievementLoss:F4}%/GOOD (250) TAP）");
        stringBuilder.AppendLine(
            $"或打出 {missCount} 个 MISS TAP （-{singleMissTapAchievementLoss:F4}%/MISS (0) TAP）");
        if (commandContext.HasFlag("--detailed"))
        {
            stringBuilder.AppendLine("\n附：");

            stringBuilder.AppendLine($"-{20000f / totalBasicScore:F4}%/GREAT (800) HOLD");
            stringBuilder.AppendLine($"-{50000f / totalBasicScore:F4}%/GOOD (500) HOLD");
            stringBuilder.AppendLine($"-{100000f / totalBasicScore:F4}%/MISS (0) HOLD");

            stringBuilder.AppendLine("");

            stringBuilder.AppendLine($"-{30000f / totalBasicScore:F4}%/GREAT (1200) SLIDE");
            stringBuilder.AppendLine($"-{75000f / totalBasicScore:F4}%/GOOD (750) SLIDE");
            stringBuilder.AppendLine($"-{150000f / totalBasicScore:F4}%/MISS (0) SLIDE");

            stringBuilder.AppendLine("");

            stringBuilder.AppendLine(
                $"-{25f / totalBreakScore:F4}%/PERFECT (2550) BREAK");
            stringBuilder.AppendLine(
                $"-{50f / totalBreakScore:F4}%/PERFECT (2500) BREAK");
            stringBuilder.AppendLine(
                $"-{50000f / totalBasicScore + 60f / totalBreakScore:F4}%/GREAT (2000) BREAK");
            stringBuilder.AppendLine(
                $"-{100000f / totalBasicScore + 60f / totalBreakScore:F4}%/GREAT (1500) BREAK");
            stringBuilder.AppendLine(
                $"-{125000f / totalBasicScore + 60f / totalBreakScore:F4}%/GREAT (1250) BREAK");
            stringBuilder.AppendLine(
                $"-{150000f / totalBasicScore + 70f / totalBreakScore:F4}%/GOOD (1000) BREAK");
            stringBuilder.AppendLine(
                $"-{250000f / totalBasicScore + 100f / totalBreakScore:F4}%/MISS (0) BREAK");
        }

        await SendMessage(context, [stringBuilder.ToString().Trim()]);
    }
}