using System.Text;
using Sapphire.Data.Mai;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Environments;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Sapphire.ImageGeneration.ImageGenerators.MaiCommandImageGenerators;

public static class InfoImageGenerator
{
    private static readonly Image RemasterIndicatorImage;
    private static readonly Image MasterIndicatorImage;
    private static readonly Image ExpertIndicatorImage;
    private static readonly Image AdvancedIndicatorImage;
    private static readonly Image BasicIndicatorImage;

    static InfoImageGenerator()
    {
        RemasterIndicatorImage =
            new Image<Rgba32>(231, 222, ColorPalette.RemasterColor.ToPixel<Rgba32>());
        RemasterIndicatorImage.Mutate(processCtx =>
        {
            processCtx.Paint(canvas =>
            {
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 48, "re",
                    new PointF(215, 45), HorizontalAlignment.Right, 0.2f, float.MaxValue,
                    ColorPalette.RemasterFontColor);
            });
        });

        MasterIndicatorImage =
            new Image<Rgba32>(231, 128, ColorPalette.MasterColor.ToPixel<Rgba32>());
        MasterIndicatorImage.Mutate(processCtx =>
        {
            processCtx.Paint(canvas =>
            {
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 48, "mas",
                    new PointF(215, 45), HorizontalAlignment.Right, 0.2f, float.MaxValue,
                    ColorPalette.MasterFontColor);
            });
        });

        ExpertIndicatorImage =
            new Image<Rgba32>(231, 128, ColorPalette.ExpertColor.ToPixel<Rgba32>());
        ExpertIndicatorImage.Mutate(processCtx =>
        {
            processCtx.Paint(canvas =>
            {
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 48, "exp",
                    new PointF(215, 45f), HorizontalAlignment.Right, 0.2f, float.MaxValue,
                    ColorPalette.ExpertFontColor);
            });
        });

        AdvancedIndicatorImage =
            new Image<Rgba32>(231, 128, ColorPalette.AdvancedColor.ToPixel<Rgba32>());
        AdvancedIndicatorImage.Mutate(processCtx =>
        {
            processCtx.Paint(canvas =>
            {
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 48, "adv",
                    new PointF(215, 45f), HorizontalAlignment.Right, 0.2f, float.MaxValue,
                    ColorPalette.AdvancedFontColor);
            });
        });

        BasicIndicatorImage =
            new Image<Rgba32>(231, 224, ColorPalette.BasicColor.ToPixel<Rgba32>());
        BasicIndicatorImage.Mutate(processCtx =>
        {
            processCtx.Paint(canvas =>
            {
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 48, "bas",
                    new PointF(215, 135), HorizontalAlignment.Right, 0.2f, float.MaxValue,
                    ColorPalette.BasicFontColor);
            });
        });
    }

    public static async Task<string> Generate(MaiSongDto song, MaiPlayRecord[]? records, long? userId, string? userName,
        bool? useJp)
    {
        using var backgroundImage = new Image<Rgba32>(1440, 2048, ColorPalette.BackgroundColor.ToPixel<Rgba32>());
        backgroundImage.Mutate(processCtx =>
        {
            using var maskImage = new Image<Rgba32>(1440, 828,
                ColorPalette.GetColorWithNewTransparency(ColorPalette.MiddleLayerColor, 0.2f).ToPixel<Rgba32>());

            processCtx.DrawImage(maskImage, new Point(0, 1220), 1f);

            processCtx.DrawImage(BasicIndicatorImage, new Point(0, 1220), 1);
            processCtx.DrawImage(AdvancedIndicatorImage, new Point(0, 1442), 1);
            processCtx.DrawImage(ExpertIndicatorImage, new Point(0, 1570), 1);
            processCtx.DrawImage(MasterIndicatorImage, new Point(0, 1698), 1);
            processCtx.DrawImage(RemasterIndicatorImage, new Point(0, 1826), 1);

            processCtx.Paint(canvas =>
            {
                if (records != null && userName != null)
                    ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 26.8f, userName,
                        new PointF(1212, 180),
                        HorizontalAlignment.Right);

                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 374.8f, "info",
                    new PointF(-53, -97.8f), HorizontalAlignment.Left, 0.1f);

                var sidTitle = "SID";
                var sid = song.SapphireId.ToString();

                var idTitle = "ID";
                var id = song.SongId?.ToString();

                var songType = song.SongType switch
                {
                    MaiSongType.Dx => "dx",
                    MaiSongType.Std => "std",
                    MaiSongType.Utage => "utage",
                    _ => "unknown"
                };

                var watermarkTextOptions = new RichTextOptions(ColorPalette.RegularFont.CreateFont(36f))
                {
                    Origin = new PointF(1334.7f, 1073.2f),
                    Dpi = 96f,
                    FallbackFontFamilies = [ColorPalette.FallbackFont, ColorPalette.EmojiFont],
                    MaxLines = 2,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    TextAlignment = TextAlignment.End
                };

                canvas.DrawText(watermarkTextOptions,
                    $"generated by {SpEnvironmentHandler.Environment.Sapphire.BotName}\npowered by Sapphire",
                    Brushes.Solid(ColorPalette.GetColorWithNewTransparency(ColorPalette.FontColor, 0.1f)), null);

                var idInformationString =
                    id == null ? $"{sidTitle}{sid}·{songType}" : $"{sidTitle}{sid}·{idTitle}{id}·{songType}";

                var idInformationTextOptions = id == null
                    ? new RichTextOptions(ColorPalette.RegularFont.CreateFont(39f))
                    {
                        Origin = new PointF(1322, 275f),
                        Dpi = 96f,
                        FallbackFontFamilies = [ColorPalette.FallbackFont, ColorPalette.EmojiFont],
                        HorizontalAlignment = HorizontalAlignment.Right,
                        TextRuns =
                        [
                            new RichTextRun
                            {
                                Font = ColorPalette.NormalFont.CreateFont(39f),
                                Start = 0,
                                End = sidTitle.Length,
                                Brush = Brushes.Solid(
                                    ColorPalette.GetColorWithNewTransparency(ColorPalette.FontColor, 0.5f))
                            },
                            new RichTextRun
                            {
                                Font = ColorPalette.RegularFont.CreateFont(39f),
                                Start = sidTitle.Length,
                                End = sidTitle.Length + sid.Length + 1,
                                Brush = Brushes.Solid(ColorPalette.FontColor)
                            },
                            new RichTextRun
                            {
                                Font = ColorPalette.RegularFont.CreateFont(39f),
                                Start = sidTitle.Length + sid.Length + 1,
                                End = sidTitle.Length + sid.Length + songType.Length + 1,
                                Brush = Brushes.Solid(ColorPalette.FontColor)
                            }
                        ]
                    }
                    : new RichTextOptions(ColorPalette.RegularFont.CreateFont(39f))
                    {
                        Origin = new PointF(1322, 275f),
                        Dpi = 96f,
                        FallbackFontFamilies = [ColorPalette.FallbackFont, ColorPalette.EmojiFont],
                        HorizontalAlignment = HorizontalAlignment.Right,
                        TextRuns =
                        [
                            new RichTextRun
                            {
                                Font = ColorPalette.NormalFont.CreateFont(39f),
                                Start = 0,
                                End = sidTitle.Length,
                                Brush = Brushes.Solid(
                                    ColorPalette.GetColorWithNewTransparency(ColorPalette.FontColor, 0.5f))
                            },
                            new RichTextRun
                            {
                                Font = ColorPalette.RegularFont.CreateFont(39f),
                                Start = sidTitle.Length,
                                End = sidTitle.Length + sid.Length + 1,
                                Brush = Brushes.Solid(ColorPalette.FontColor)
                            },
                            new RichTextRun
                            {
                                Font = ColorPalette.NormalFont.CreateFont(39f),
                                Start = sidTitle.Length + sid.Length + 1,
                                End = sidTitle.Length + sid.Length + idTitle.Length + 1,
                                Brush = Brushes.Solid(
                                    ColorPalette.GetColorWithNewTransparency(ColorPalette.FontColor, 0.5f))
                            },
                            new RichTextRun
                            {
                                Font = ColorPalette.RegularFont.CreateFont(39f),
                                Start = sidTitle.Length + sid.Length + idTitle.Length + 1,
                                End = sidTitle.Length + sid.Length + idTitle.Length + id.Length + 2,
                                Brush = Brushes.Solid(ColorPalette.FontColor)
                            },
                            new RichTextRun
                            {
                                Font = ColorPalette.RegularFont.CreateFont(39f),
                                Start = sidTitle.Length + sid.Length + idTitle.Length + id.Length + 2,
                                End = sidTitle.Length + sid.Length + idTitle.Length + id.Length + songType.Length + 2,
                                Brush = Brushes.Solid(ColorPalette.FontColor)
                            }
                        ]
                    };

                canvas.DrawText(idInformationTextOptions, idInformationString, Brushes.Solid(ColorPalette.FontColor),
                    null);

                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 39f, "曲名",
                    new PointF(527.7f, 414.2f),
                    HorizontalAlignment.Left, 0.5f);
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 39f, song.SongName ?? "未知",
                    new PointF(527.7f, 467.5f),
                    HorizontalAlignment.Left, 1, 780);

                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 39f, "艺术家",
                    new PointF(527.7f, 561.1f),
                    HorizontalAlignment.Left, 0.5f);
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 39f, song.ArtistName ?? "未知",
                    new PointF(527.7f, 613.9f),
                    HorizontalAlignment.Left, 1, 780);

                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 36f, "分类",
                    new PointF(102f, 717.8f),
                    HorizontalAlignment.Left, 0.5f);

                var categories = MaiDataManager.Instance?.Categories;
                var categoryName = song.CategoryIndex == null || categories == null
                    ? "未知"
                    : categories[song.CategoryIndex ?? 0].Name ?? "未知";

                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 36f, categoryName,
                    new PointF(102f, 774.9f),
                    HorizontalAlignment.Left);

                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 36f, "登场版本",
                    new PointF(102f, 837.7f),
                    HorizontalAlignment.Left, 0.5f);

                var versionStringBuilder = new StringBuilder();
                var versions = MaiDataManager.Instance?.Versions;
                if (versions != null)
                {
                    var cnVersion = song.CnVersionIndex == null ? null : versions[song.CnVersionIndex ?? 0];
                    var jpVersion = song.JpVersionIndex == null ? null : versions[song.JpVersionIndex ?? 0];

                    if (jpVersion != null)
                        versionStringBuilder.Append($"{jpVersion.JpName ?? "未知"}/{jpVersion.JpBriefName ?? "未知"}(JP)·");
                    if (cnVersion != null)
                        versionStringBuilder.Append($"{cnVersion.CnName ?? "未知"}/{cnVersion.CnBriefName ?? "未知"}(CN)");
                }

                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 36f,
                    versionStringBuilder.ToString().Trim('·'),
                    new PointF(102f, 888.5f),
                    HorizontalAlignment.Left);

                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 36f, "可玩地区",
                    new PointF(102f, 957.1f),
                    HorizontalAlignment.Left, 0.5f);
                var availabilityStringBuilder = new StringBuilder();
                if (song.AvailableInCn ?? false)
                    availabilityStringBuilder.Append("·CN");
                if (song.AvailableInJp ?? false)
                    availabilityStringBuilder.Append("·JP");
                if (song.AvailableInIntl ?? false)
                    availabilityStringBuilder.Append("·INTL");
                if (song.AvailableInUsa ?? false)
                    availabilityStringBuilder.Append("·USA");
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 36f,
                    availabilityStringBuilder.ToString().Trim('·'),
                    new PointF(102f, 1010.4f),
                    HorizontalAlignment.Left);

                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 36f, "登场日期（JP）",
                    new PointF(102f, 1073.2f),
                    HorizontalAlignment.Left, 0.5f);
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 36f,
                    song.ReleaseDate.ToString() ?? "未知",
                    new PointF(102f, 1124.8f),
                    HorizontalAlignment.Left);

                if (records != null)
                    DrawPlayRecord(canvas, song, records, useJp ?? false);
                else
                    DrawChartInformation(canvas, song);
            });

            using var cover = MaiImageGeneratorFunctions.GetCoverHdImage(song.SapphireId);

            processCtx.DrawImage(cover, new Point(104, 280),
                1);

            if (records != null && userId != null)
            {
                using var userAvatar = ImageGeneratorFunctions.GetUserAvatar((long)userId);

                if (userAvatar.Width != 100 || userAvatar.Height != 100)
                    userAvatar.Mutate(y => y.Resize(100, 100));

                processCtx.DrawImage(userAvatar, new Point(1222, 117), 1f);
            }
        });

        return await ImageGeneratorFunctions.ToBase64Async(backgroundImage);
    }

    private static void DrawPlayRecord(DrawingCanvas canvas, MaiSongDto song, MaiPlayRecord[] record, bool useJp)
    {
        var baseTop = 1331;
        var baseLeft = 279;
        var baseRight = 1324;

        for (var i = 0; i < 5; i++)
        {
            ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 32f, "达成率",
                new PointF(baseLeft, baseTop + 5), HorizontalAlignment.Left, 0.5f);
            if (i > song.Charts.Count - 1)
            {
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 36, "不存在的难度",
                    new PointF(baseLeft, baseTop + 55), HorizontalAlignment.Left);
                baseTop += 124;
                continue;
            }

            var chart = song.Charts[i];
            var playRecord = record[i];

            if (playRecord.ClearState == ClearState.NotPlayed)
            {
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 36, "未游玩",
                    new PointF(baseLeft, baseTop + 55), HorizontalAlignment.Left);
                baseTop += 124;
                continue;
            }

            var achievementText = MaiImageGeneratorFunctions.GetAchievementText(playRecord.Score);
            var rateText = MaiImageGeneratorFunctions.GetRateText(playRecord.Score);

            var clearStateStringBuilder = new StringBuilder();
            if (playRecord.ClearState != ClearState.Empty)
                clearStateStringBuilder.Append(
                    playRecord.ClearState switch

                    {
                        ClearState.AllPerfectPlus => "AP+",
                        ClearState.AllPerfect => "AP",
                        ClearState.FullComboPlus => "FC+",
                        ClearState.FullCombo => "FC",
                        _ => ""
                    }
                );
            if (playRecord.SyncState != SyncState.Empty)
                clearStateStringBuilder.Append(
                    "·" + playRecord.SyncState switch
                    {
                        SyncState.FullSyncDxPlus => "FDX+",
                        SyncState.FullSyncDx => "FDX",
                        SyncState.FullSyncPlus => "FS+",
                        SyncState.FullSync => "FS",
                        SyncState.Sync => "SYNC",
                        _ => ""
                    }
                );

            var achievementTextOption = new RichTextOptions(ColorPalette.RegularFont.CreateFont(45f))
            {
                TextRuns =
                [
                    new RichTextRun
                    {
                        Font = ColorPalette.RegularFont.CreateFont(45f),
                        Start = 0,
                        End = achievementText.Length - 6 < 1 ? 1 : achievementText.Length - 6
                    },
                    new RichTextRun
                    {
                        Font = ColorPalette.RegularFont.CreateFont(36f),
                        Start = achievementText.Length - 6 < 1 ? 1 : achievementText.Length - 6,
                        End = achievementText.Length + 1
                    },
                    new RichTextRun
                    {
                        Font = ColorPalette.HeavyFont.CreateFont(36f),
                        Start = achievementText.Length + 1,
                        End = achievementText.Length + 1 + rateText.Length
                    },
                    new RichTextRun
                    {
                        Font = ColorPalette.RegularFont.CreateFont(36f),
                        Start = achievementText.Length + 1 + rateText.Length,
                        End = achievementText.Length + 1 + rateText.Length + 1 + clearStateStringBuilder.Length
                    }
                ],
                Dpi = 96,
                Origin = new PointF(baseLeft, baseTop + 55)
            };

            canvas.DrawText(achievementTextOption,
                $"{achievementText}·{rateText}·{clearStateStringBuilder.ToString().Trim('·')}",
                Brushes.Solid(ColorPalette.FontColor), null);

            var levelText = MaiImageGeneratorFunctions.GetLevelText(useJp ? chart.JpLevel : chart.CnLevel) ?? "未知";
            var levelTextOption = new RichTextOptions(ColorPalette.RegularFont.CreateFont(32f))
            {
                TextRuns =
                [
                    new RichTextRun
                    {
                        Font = ColorPalette.NormalFont.CreateFont(32f),
                        Start = 0,
                        End = 2,
                        Brush = Brushes.Solid(ColorPalette.GetColorWithNewTransparency(ColorPalette.FontColor, 0.5f))
                    },
                    new RichTextRun
                    {
                        Font = ColorPalette.RegularFont.CreateFont(32f),
                        Start = 2,
                        End = 2 + levelText.Length,
                        Brush = Brushes.Solid(ColorPalette.FontColor)
                    }
                ],
                Dpi = 96,
                Origin = new PointF(424, baseTop + 5)
            };
            canvas.DrawText(levelTextOption, $"等级 {levelText}",
                Brushes.Solid(ColorPalette.FontColor), null);
            var ratingText = MaiDataManager.GetRating((useJp ? chart.JpLevel : chart.CnLevel) ?? 0, playRecord.Score)
                .ToString();
            var ratingTextOption = new RichTextOptions(ColorPalette.RegularFont.CreateFont(32f))
            {
                TextRuns =
                [
                    new RichTextRun
                    {
                        Font = ColorPalette.NormalFont.CreateFont(32f),
                        Start = 0,
                        End = 2,
                        Brush = Brushes.Solid(ColorPalette.GetColorWithNewTransparency(ColorPalette.FontColor, 0.5f))
                    },
                    new RichTextRun
                    {
                        Font = ColorPalette.RegularFont.CreateFont(32f),
                        Start = 2,
                        End = 2 + ratingText.Length,
                        Brush = Brushes.Solid(ColorPalette.FontColor)
                    }
                ],
                Dpi = 96,
                Origin = new PointF(618, baseTop + 5)
            };
            canvas.DrawText(ratingTextOption, $"评分 {ratingText}",
                Brushes.Solid(ColorPalette.FontColor), null);

            if (playRecord.PlayCount != 0 || playRecord.ClearState == ClearState.NotPlayed)
            {
                var playCountText = playRecord.PlayCount.ToString();
                var playCountTextOption = new RichTextOptions(ColorPalette.RegularFont.CreateFont(32f))
                {
                    TextRuns =
                    [
                        new RichTextRun
                        {
                            Font = ColorPalette.NormalFont.CreateFont(32f),
                            Start = 0,
                            End = 4,
                            Brush = Brushes.Solid(
                                ColorPalette.GetColorWithNewTransparency(ColorPalette.FontColor, 0.5f))
                        },
                        new RichTextRun
                        {
                            Font = ColorPalette.RegularFont.CreateFont(32f),
                            Start = 4,
                            End = 4 + playCountText.Length,
                            Brush = Brushes.Solid(ColorPalette.FontColor)
                        }
                    ],
                    Dpi = 96,
                    Origin = new PointF(810, baseTop + 5)
                };
                canvas.DrawText(playCountTextOption, $"游玩次数 {playCountText}",
                    Brushes.Solid(ColorPalette.FontColor), null);
            }

            ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 28, "dx 分数",
                new PointF(baseRight, baseTop + 5), HorizontalAlignment.Right, 0.5f);
            ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 28,
                $"{playRecord.DxScore}/{MaiImageGeneratorFunctions.GetMaxDxScore(chart)}",
                new PointF(baseRight, baseTop + 45), HorizontalAlignment.Right);

            var starRate = (float)playRecord.DxScore /
                           MaiImageGeneratorFunctions.GetMaxDxScore(chart);

            var starCount = MaiImageGeneratorFunctions.GetStarCount(starRate);
            var starTextStringBuilder = new StringBuilder();

            for (var j = 0; j < starCount; j++) starTextStringBuilder.Append("★");

            ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 24,
                starTextStringBuilder.ToString(),
                new PointF(baseRight, baseTop + 85), HorizontalAlignment.Right);

            baseTop += 124;
        }
    }

    private static void DrawChartInformation(DrawingCanvas canvas, MaiSongDto song)
    {
        var baseTop = 1331;
        var baseLeft = 279;
        var baseRight = 1324;

        for (var i = 0; i < 5; i++)
        {
            if (i > song.Charts.Count - 1)
            {
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 32f, "等级",
                    new PointF(baseLeft, baseTop + 5), HorizontalAlignment.Left, 0.5f);
                ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 32f, "NaN",
                    new PointF(baseLeft, baseTop + 55), HorizontalAlignment.Left);
                baseTop += 124;
                continue;
            }

            var chart = song.Charts[i];

            var levelStringBuilder = new StringBuilder();
            if (chart.CnLevel != null)
                levelStringBuilder.Append(MaiImageGeneratorFunctions.GetLevelText(chart.CnLevel) + " (CN)·");
            if (chart.JpLevel != null)
                levelStringBuilder.Append(MaiImageGeneratorFunctions.GetLevelText(chart.JpLevel) + " (JP)·");
            if (chart.FitLevel != null)
                levelStringBuilder.Append(MaiImageGeneratorFunctions.GetLevelText(chart.FitLevel, true) + " (fit)");

            var levelString = levelStringBuilder.ToString().Trim('·');
            var levelStringLength = TextMeasurer.Measure(levelString,
                new TextOptions(ColorPalette.RegularFont.CreateFont(32f))
                {
                    Dpi = 96
                }).Bounds.Width;

            var charterMaxLength = 1440 - 275 - levelStringLength - 110 - 50;

            ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 32f, "等级",
                new PointF(baseLeft, baseTop + 5), HorizontalAlignment.Left, 0.5f);
            ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 32f, levelString,
                new PointF(baseLeft, baseTop + 55), HorizontalAlignment.Left);

            ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.NormalFont, 32f, "谱师",
                new PointF(baseRight, baseTop + 5), HorizontalAlignment.Right, 0.5f);
            ImageGeneratorFunctions.DrawSimpleText(canvas, ColorPalette.RegularFont, 32f, chart.CharterName ?? "未知",
                new PointF(baseRight, baseTop + 55), HorizontalAlignment.Right, 1, charterMaxLength);

            baseTop += 124;
        }
    }
}