using System.Text;
using Sapphire.Commands.MaiCommands;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Environments;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Sapphire.ImageGeneration.ImageGenerators.MaiCommandImageGenerators;

public static class BestListImageGenerator
{
    private static readonly Image RemasterIndicatorImage =
        new Image<Rgba32>(100, 5, ColorPalette.RemasterColor.ToPixel<Rgba32>());

    private static readonly Image MasterIndicatorImage =
        new Image<Rgba32>(100, 5, ColorPalette.MasterColor.ToPixel<Rgba32>());

    private static readonly Image ExpertIndicatorImage =
        new Image<Rgba32>(100, 5, ColorPalette.ExpertColor.ToPixel<Rgba32>());

    private static readonly Image AdvancedIndicatorImage =
        new Image<Rgba32>(100, 5, ColorPalette.AdvancedColor.ToPixel<Rgba32>());

    private static readonly Image BasicIndicatorImage =
        new Image<Rgba32>(100, 5, ColorPalette.BasicColor.ToPixel<Rgba32>());

    public static async Task<string> Generate(BestListCommand.BestListData data, bool useJpLevel, bool useFit)
    {
        using var backgroundImage = new Image<Rgba32>(2880, 2200, ColorPalette.BackgroundColor.ToPixel<Rgba32>());


        backgroundImage.Mutate(x =>
        {
            using var maskImage = new Image<Rgba32>(2880, 1880,
                ColorPalette.GetColorWithNewTransparency(ColorPalette.MiddleLayerColor, 0.2f).ToPixel<Rgba32>());

            x.DrawImage(maskImage, new Point(0, 320), 1f);

            var oldCount = 0;
            foreach (var valueTuple in data.OldBestList)
            {
                var sapphireId = valueTuple.Item1 / 5 + 1;
                var levelIndex = valueTuple.Item1 % 5;

                using var itemImage =
                    GenerateItem(oldCount, sapphireId, levelIndex, valueTuple.Item2, useJpLevel, useFit);
                var column = oldCount % 5;
                var row = oldCount / 5;

                var top = row * (124 + 45) + 410;
                var left = column * (508 + 42) + 86;

                x.DrawImage(itemImage, new Point(left, top), 1f);
                oldCount++;
            }

            var newCount = 0;
            foreach (var valueTuple in data.NewBestList)
            {
                var sapphireId = valueTuple.Item1 / 5 + 1;
                var levelIndex = valueTuple.Item1 % 5;

                using var itemImage =
                    GenerateItem(newCount, sapphireId, levelIndex, valueTuple.Item2, useJpLevel, useFit);
                var column = newCount % 5;
                var row = newCount / 5;

                var top = row * (124 + 45) + 1647;
                var left = column * (508 + 42) + 86;

                x.DrawImage(itemImage, new Point(left, top), 1f);
                newCount++;
            }

            using var userAvatar = ImageGeneratorFunctions.GetUserAvatar(data.UserId);

            if (userAvatar.Width != 100 || userAvatar.Height != 100)
                userAvatar.Mutate(y => y.Resize(100, 100));

            x.DrawImage(userAvatar, new Point(2694, 97), 1f);

            x.Paint(canvas =>
            {
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 26.8f, data.UserName,
                    new PointF(2681.3f, 163.1f),
                    HorizontalAlignment.Right);

                var oldRating = data.OldBestList.Sum(y =>
                    {
                        var chart = MaiDataManager.Instance?.SapphireIdToSong[y.Item1 / 5 + 1].Charts[y.Item1 % 5];

                        return MaiDataManager.GetRating(
                            (useFit
                                ? chart?.FitLevel
                                : useJpLevel
                                    ? chart?.JpLevel
                                    : chart?.CnLevel) ?? 0,
                            y.Item2.Score);
                    }
                );

                var newRating = data.NewBestList.Sum(y =>
                    {
                        var chart = MaiDataManager.Instance?.SapphireIdToSong[y.Item1 / 5 + 1].Charts[y.Item1 % 5];

                        return MaiDataManager.GetRating(
                            (useFit
                                ? chart?.FitLevel
                                : useJpLevel
                                    ? chart?.JpLevel
                                    : chart?.CnLevel) ?? 0,
                            y.Item2.Score);
                    }
                );

                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 19.1f,
                    $"{oldRating + newRating}·{oldRating}·{newRating}", new PointF(2681.3f, 135.6f),
                    HorizontalAlignment.Right);

                var textOptions = new RichTextOptions(ColorPalette.NormalFont.CreateFont(22f))
                {
                    Origin = new PointF(2792.9f, 227.8f),
                    Dpi = 96f,
                    FallbackFontFamilies = [ColorPalette.FallbackFont, ColorPalette.EmojiFont],
                    MaxLines = 2,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    TextAlignment = TextAlignment.End
                };

                canvas.DrawText(textOptions,
                    $"generated by {SpEnvironmentHandler.Environment.Sapphire.BotName}\npowered by Sapphire",
                    Brushes.Solid(ColorPalette.GetColorWithNewTransparency(ColorPalette.FontColor, 0.1f)), null);

                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 202.6f,
                    "top 50",
                    new PointF(-10.3f, -62.9f), HorizontalAlignment.Left, 0.1f);
            });
        });

        return await ImageGeneratorFunctions.ToBase64Async(backgroundImage);
    }

    private static Image GenerateItem(int index, int sapphireId, int levelIndex, MaiPlayRecord maiPlayRecord,
        bool useJpLevel, bool useFit)
    {
        if (MaiDataManager.Instance == null) throw new InvalidOperationException("MaiDataManager not set");

        var song = MaiDataManager.Instance.SapphireIdToSong[sapphireId];

        var image = new Image<Rgba32>(513, 156);

        image.Mutate(x =>
        {
            var songName = song.SongName ?? "未知乐曲";

            var coverImage = MaiImageGeneratorFunctions.GetCoverImage(song.SapphireId);
            x.DrawImage(coverImage, new Point(0, 2), 1f);

            if (!SpEnvironmentHandler.Environment.ImageGeneration.PreloadCoverImages)
                coverImage.Dispose();

            x.DrawImage(levelIndex switch
            {
                0 => BasicIndicatorImage,
                1 => AdvancedIndicatorImage,
                2 => ExpertIndicatorImage,
                3 => MasterIndicatorImage,
                4 => RemasterIndicatorImage,
                _ => BasicIndicatorImage
            }, new Point(0, 107), 1f);

            x.Paint(y =>
            {
                ImageGeneratorFunctions.DrawSimpleText(y, ColorPalette.NormalFont, 15.7f, "标题", new PointF(106.7f, -1f),
                    HorizontalAlignment.Left, 0.5f);
                ImageGeneratorFunctions.DrawSimpleText(y, ColorPalette.RegularFont, 24f, songName,
                    new PointF(106.7f, 18.3f),
                    HorizontalAlignment.Left, 1f, 275f);
                ImageGeneratorFunctions.DrawSimpleText(y, ColorPalette.NormalFont, 15.7f, "达成率",
                    new PointF(106.7f, 59.7f),
                    HorizontalAlignment.Left, 0.5f);

                var achievementText = MaiImageGeneratorFunctions.GetAchievementText(maiPlayRecord.Score);
                var rateText = MaiImageGeneratorFunctions.GetRateText(maiPlayRecord.Score);

                var textOptions = new RichTextOptions(ColorPalette.RegularFont.CreateFont(26f))
                {
                    Origin = new PointF(106.7f, 78.9f),
                    Dpi = 96f,
                    FallbackFontFamilies = [ColorPalette.FallbackFont, ColorPalette.EmojiFont],
                    TextRuns =
                    [
                        new RichTextRun
                        {
                            Font = ColorPalette.RegularFont.CreateFont(26f),
                            Start = 0,
                            End = achievementText.Length - 6
                        },
                        new RichTextRun
                        {
                            Font = ColorPalette.RegularFont.CreateFont(22f),
                            Start = achievementText.Length - 5,
                            End = achievementText.Length + 1
                        },
                        new RichTextRun
                        {
                            Font = ColorPalette.HeavyFont.CreateFont(22f),
                            Start = achievementText.Length + 1,
                            End = achievementText.Length + rateText.Length + 1
                        }
                    ]
                };

                y.DrawText(textOptions, $"{achievementText}·{rateText}", Brushes.Solid(ColorPalette.FontColor), null);

                var altInfoStringBuilder = new StringBuilder($"#{index + 1}·SID{song.SapphireId}");
                if (song.SongId != null)
                    altInfoStringBuilder.Append($"·ID{song.SongId}");
                altInfoStringBuilder.Append($"·{
                    levelIndex switch {
                        0 => "bas",
                        1 => "adv",
                        2 => "exp",
                        3 => "mas",
                        4 => "re",
                        _ => "unknown"
                    }
                }");

                ImageGeneratorFunctions.DrawSimpleText(y, ColorPalette.NormalFont, 15.7f,
                    altInfoStringBuilder.ToString(),
                    new PointF(0, 114.8f), HorizontalAlignment.Left, 0.5f);

                ImageGeneratorFunctions.DrawSimpleText(y, ColorPalette.NormalFont, 15.7f, "dx 分数", new PointF(512, -2f),
                    HorizontalAlignment.Right, 0.5f);
                var starRate = (float)maiPlayRecord.DxScore /
                               MaiImageGeneratorFunctions.GetMaxDxScore(song.Charts[levelIndex]);
                ImageGeneratorFunctions.DrawSimpleText(y, ColorPalette.RegularFont, 18.9f,
                    $"{maiPlayRecord.DxScore}/{MaiImageGeneratorFunctions.GetMaxDxScore(song.Charts[levelIndex])}",
                    new PointF(512, 18.3f),
                    HorizontalAlignment.Right);

                var starCount = MaiImageGeneratorFunctions.GetStarCount(starRate);
                var starTextStringBuilder = new StringBuilder();

                for (var i = 0; i < starCount; i++) starTextStringBuilder.Append("★");

                ImageGeneratorFunctions.DrawSimpleText(y, ColorPalette.RegularFont, 15.7f,
                    starTextStringBuilder.ToString(),
                    new PointF(512, 41.3f), HorizontalAlignment.Right);

                ImageGeneratorFunctions.DrawSimpleText(y, ColorPalette.NormalFont, 15.7f, "评分", new PointF(512, 66.7f),
                    HorizontalAlignment.Right, 0.5f);
                ImageGeneratorFunctions.DrawSimpleText(y, ColorPalette.RegularFont, 18.9f, MaiDataManager.GetRating(
                        (useFit
                            ? song.Charts[levelIndex].FitLevel
                            : useJpLevel
                                ? song.Charts[levelIndex].JpLevel
                                : song.Charts[levelIndex].CnLevel) ?? 0, maiPlayRecord.Score).ToString(),
                    new PointF(512, 86.9f),
                    HorizontalAlignment.Right);

                ImageGeneratorFunctions.DrawSimpleText(y, ColorPalette.NormalFont, 15.7f, "等级",
                    new PointF(460.4f, 66.7f),
                    HorizontalAlignment.Right, 0.5f);
                ImageGeneratorFunctions.DrawSimpleText(y, ColorPalette.RegularFont, 18.9f,
                    MaiImageGeneratorFunctions.GetLevelText(useFit
                        ? song.Charts[levelIndex].FitLevel
                        : useJpLevel
                            ? song.Charts[levelIndex].JpLevel
                            : song.Charts[levelIndex].CnLevel, useFit) ?? "未知", new PointF(460.4f, 86.9f),
                    HorizontalAlignment.Right);

                var clearStateStringBuilder = new StringBuilder();
                if (maiPlayRecord.ClearState != ClearState.Empty)
                    clearStateStringBuilder.Append(
                        maiPlayRecord.ClearState switch

                        {
                            ClearState.AllPerfectPlus => "AP+",
                            ClearState.AllPerfect => "AP",
                            ClearState.FullComboPlus => "FC+",
                            ClearState.FullCombo => "FC",
                            _ => ""
                        }
                    );
                if (maiPlayRecord.SyncState != SyncState.Empty)
                    clearStateStringBuilder.Append(
                        "·" + maiPlayRecord.SyncState switch
                        {
                            SyncState.FullSyncDxPlus => "FDX+",
                            SyncState.FullSyncDx => "FDX",
                            SyncState.FullSyncPlus => "FS+",
                            SyncState.FullSync => "FS",
                            SyncState.Sync => "SYNC",
                            _ => ""
                        }
                    );

                if (maiPlayRecord.PlayCount != 0) clearStateStringBuilder.Append($"·{maiPlayRecord.PlayCount} plays");

                ImageGeneratorFunctions.DrawSimpleText(y, ColorPalette.RegularFont, 15.7f,
                    clearStateStringBuilder.ToString().Trim('·'),
                    new PointF(512, 114.8f), HorizontalAlignment.Right);
            });
        });

        return image;
    }
}