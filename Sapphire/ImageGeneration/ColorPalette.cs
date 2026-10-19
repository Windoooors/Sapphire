using Sapphire.Environments;
using Serilog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Sapphire.ImageGeneration;

public static class ColorPalette
{
    static ColorPalette()
    {
        ParseHex(SpEnvironmentHandler.Environment.ImageGeneration.BackgroundColorInHex, Color.Black,
            out var backgroundColor);
        ParseHex(SpEnvironmentHandler.Environment.ImageGeneration.FontColorInHex, Color.White,
            out var fontColor);
        ParseHex(SpEnvironmentHandler.Environment.ImageGeneration.MiddleLayerColorInHex, Color.Black,
            out var middleLayerColor);

        ParseHex(SpEnvironmentHandler.Environment.ImageGeneration.RemasterColorInHex, Color.Black,
            out var remasterColor);
        ParseHex(SpEnvironmentHandler.Environment.ImageGeneration.MasterColorInHex, Color.Black,
            out var masterColor);
        ParseHex(SpEnvironmentHandler.Environment.ImageGeneration.ExpertColorInHex, Color.Black,
            out var expertColor);
        ParseHex(SpEnvironmentHandler.Environment.ImageGeneration.AdvancedColorInHex, Color.Black,
            out var advancedColor);
        ParseHex(SpEnvironmentHandler.Environment.ImageGeneration.BasicColorInHex, Color.Black,
            out var basicColor);

        ParseHex(SpEnvironmentHandler.Environment.ImageGeneration.RemasterFontColorInHex, Color.Black,
            out var remasterFontColor);
        ParseHex(SpEnvironmentHandler.Environment.ImageGeneration.MasterFontColorInHex, Color.Black,
            out var masterFontColor);
        ParseHex(SpEnvironmentHandler.Environment.ImageGeneration.ExpertFontColorInHex, Color.Black,
            out var expertFontColor);
        ParseHex(SpEnvironmentHandler.Environment.ImageGeneration.AdvancedFontColorInHex, Color.Black,
            out var advancedFontColor);
        ParseHex(SpEnvironmentHandler.Environment.ImageGeneration.BasicFontColorInHex, Color.Black,
            out var basicFontColor);

        FontCollection collection = new();
        LightFont = collection.Add(Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", "light.otf"));
        NormalFont = collection.Add(Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", "normal.otf"));
        RegularFont = collection.Add(Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", "regular.otf"));
        HeavyFont = collection.Add(Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", "heavy.otf"));

        FallbackFont = collection.Add(Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", "fallback.ttf"));
        EmojiFont = collection.Add(Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", "emoji.ttf"));

        FontCollection = collection;

        MiddleLayerColor = middleLayerColor;
        BackgroundColor = backgroundColor;
        FontColor = fontColor;

        RemasterColor = remasterColor;
        MasterColor = masterColor;
        ExpertColor = expertColor;
        AdvancedColor = advancedColor;
        BasicColor = basicColor;

        RemasterFontColor = remasterFontColor;
        MasterFontColor = masterFontColor;
        ExpertFontColor = expertFontColor;
        AdvancedFontColor = advancedFontColor;
        BasicFontColor = basicFontColor;
    }

    public static FontFamily LightFont { get; private set; }
    public static FontFamily NormalFont { get; private set; }
    public static FontFamily RegularFont { get; private set; }
    public static FontFamily HeavyFont { get; private set; }
    public static FontFamily FallbackFont { get; private set; }
    public static FontFamily EmojiFont { get; private set; }
    public static FontCollection FontCollection { get; private set; }

    public static Color BackgroundColor { get; private set; }
    public static Color FontColor { get; private set; }
    public static Color MiddleLayerColor { get; private set; }

    public static Color RemasterColor { get; private set; }
    public static Color MasterColor { get; private set; }
    public static Color ExpertColor { get; private set; }
    public static Color AdvancedColor { get; private set; }
    public static Color BasicColor { get; private set; }

    public static Color RemasterFontColor { get; private set; }
    public static Color MasterFontColor { get; private set; }
    public static Color ExpertFontColor { get; private set; }
    public static Color AdvancedFontColor { get; private set; }
    public static Color BasicFontColor { get; private set; }

    private static void ParseHex(string hex, Color defaultColor, out Color color)
    {
        if (!Color.TryParseHex(hex,
                out var targetColor))
        {
            targetColor = defaultColor;
            Log.Warning(
                "Color value \"{BackgroundColorInHex}\" is invalid",
                SpEnvironmentHandler.Environment.ImageGeneration.BackgroundColorInHex);
        }

        color = targetColor;
    }

    public static Color GetColorWithNewTransparency(Color color, float opacity)
    {
        var rgbaColor = color.ToPixel<Rgba32>();

        var result =
            Color.FromPixel(new Rgba32(rgbaColor.R / 255f, rgbaColor.G / 255f, rgbaColor.B / 255f, opacity));

        return result;
    }
}