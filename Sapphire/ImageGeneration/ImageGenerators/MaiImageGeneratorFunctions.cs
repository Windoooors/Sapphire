using System.Text;
using Sapphire.Data.Mai.Dtos;
using Sapphire.Environments;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Sapphire.ImageGeneration.ImageGenerators;

public static class MaiImageGeneratorFunctions
{
    private static bool _imagesLoaded;

    private static readonly Dictionary<int, Image> SapphireIdToImages = [];

    private static Image<Rgba32>? _coverPlaceHolder;

    public static void LoadImages()
    {
        if (_imagesLoaded)
            return;

        var coverPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Covers");

        foreach (var imagePath in Directory.EnumerateFiles(coverPath))
        {
            if (!int.TryParse(Path.GetFileNameWithoutExtension(imagePath), out var sapphireId))
                continue;
            var image = Image.Load<Rgba32>(imagePath);
            SapphireIdToImages.Add(sapphireId, image);
        }

        _imagesLoaded = true;
    }

    public static Image GetCoverImage(int sapphireId)
    {
        if (SpEnvironmentHandler.Environment.ImageGeneration.PreloadCoverImages)
        {
            if (!_imagesLoaded)
                return _coverPlaceHolder ??= new Image<Rgba32>(100, 100, Color.Black.ToPixel<Rgba32>());

            var image = SapphireIdToImages.TryGetValue(sapphireId, out var coverImage)
                ? coverImage
                : SapphireIdToImages[0];
            return image;
        }

        var coverPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Covers", sapphireId + ".png");
        if (!File.Exists(coverPath))
            coverPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Covers", "0.png");

        return Image.Load<Rgba32>(coverPath);
    }

    public static Image GetCoverHdImage(int sapphireId)
    {
        var coverPath = Path.Combine(AppContext.BaseDirectory, "Resources", "CoversHd", sapphireId + ".png");
        if (!File.Exists(coverPath))
            coverPath = Path.Combine(AppContext.BaseDirectory, "Resources", "CoversHd", "0.png");

        var image = Image.Load<Rgba32>(coverPath);

        if (image.Width != 400 || image.Height != 400)
            image.Mutate(x => x.Resize(400, 400));

        return image;
    }

    public static string GetRateText(uint achievement)
    {
        if (achievement >= 1005000)
            return "SSS+";
        if (achievement is < 1005000 and >= 1000000)
            return "SSS";
        if (achievement is < 1000000 and >= 995000)
            return "SS+";
        if (achievement is < 995000 and >= 990000)
            return "SS";
        if (achievement is < 990000 and >= 980000)
            return "S+";
        if (achievement is < 980000 and >= 970000)
            return "S";
        if (achievement is < 970000 and >= 940000)
            return "AAA";
        if (achievement is < 940000 and >= 900000)
            return "AA";
        if (achievement is < 900000 and >= 800000)
            return "A";
        if (achievement is < 800000 and >= 750000)
            return "BBB";
        if (achievement is < 750000 and >= 700000)
            return "BB";
        if (achievement is < 700000 and >= 600000)
            return "B";
        if (achievement is < 600000 and >= 500000)
            return "C";

        return "D";
    }

    public static string GetAchievementText(uint achievement)
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append(achievement / 10000);
        stringBuilder.Append('.');

        var decimals = achievement % 10000;

        stringBuilder.Append($"{decimals:0000}");
        stringBuilder.Append('%');
        return stringBuilder.ToString();
    }

    public static string? GetLevelText(int? level, bool keepMoreDigits = false)
    {
        if (level == null)
            return null;

        var stringBuilder = new StringBuilder();

        stringBuilder.Append(level / 100);
        stringBuilder.Append('.');
        if (keepMoreDigits)
            stringBuilder.Append($"{level % 100:00}");
        else
            stringBuilder.Append($"{level % 100 / 10:0}");
        return stringBuilder.ToString();
    }

    public static int GetStarCount(float rate)
    {
        var stars = 0;
        var starRate = rate * 100;
        if (starRate is <= 100 and >= 97)
            stars = 5;
        if (starRate is < 97 and >= 95)
            stars = 4;
        if (starRate is < 95 and >= 93)
            stars = 3;
        if (starRate is < 93 and >= 90)
            stars = 2;
        if (starRate is < 90 and >= 85)
            stars = 1;
        if (starRate < 85)
            stars = 0;

        return stars;
    }

    public static int GetMaxDxScore(MaiChartDto chart)
    {
        return (chart.BreakCount + chart.HoldCount + chart.SlideCount + chart.TapCount + (chart.TouchCount ?? 0)) * 3 ??
               0;
    }
}