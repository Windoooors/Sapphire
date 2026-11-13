using Sapphire.Environments;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace Sapphire.ImageGeneration;

public static class ImageGeneratorFunctions
{
    public static async Task<string> ToBase64Async(Image image)
    {
        using var ms = new MemoryStream();

        await image.SaveAsync(ms, new JpegEncoder
        {
            Quality = 50
        });

        return Convert.ToBase64String(ms.ToArray());
    }

    public static void DrawSimpleText(DrawingCanvas canvas, FontFamily fontFamily, float sizeInPt, string text,
        PointF point,
        HorizontalAlignment alignment, float opacity = 1f, float maxWidth = float.MaxValue, Color? color = null)
    {
        var textOptions = new RichTextOptions(fontFamily.CreateFont(sizeInPt))
        {
            Origin = point,
            Dpi = 96f,
            WrappingLength = maxWidth,
            TextEllipsis = TextEllipsis.Standard,
            HorizontalAlignment = alignment,
            MaxLines = 1,
            FallbackFontFamilies = [ColorPalette.FallbackFont, ColorPalette.EmojiFont]
        };

        canvas.DrawText(textOptions, text,
            Brushes.Solid(
                ColorPalette.GetColorWithNewTransparency(color ?? ColorPalette.FontColor, opacity))
            , null);
    }

    private static string GetAvatarUrl(string openId, int spec)
    {
        var appId = SpEnvironmentHandler.Environment.QQBot.AppId;
        return $"https://q.qlogo.cn/qqapp/{appId}/{openId}/{spec}";
    }

    public static async Task<string> GetUserAvatarBase64Async(string openId, int spec = 100)
    {
        if (string.IsNullOrWhiteSpace(openId))
            return "";

        using var resp = await Program.HttpClient.GetAsync(GetAvatarUrl(openId, spec));
        await using var stream = await resp.Content.ReadAsStreamAsync();

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        return Convert.ToBase64String(memoryStream.ToArray());
    }

    public static async Task<Image> GetUserAvatarAsync(string openId, int spec = 100)
    {
        if (string.IsNullOrWhiteSpace(openId))
            return new Image<Rgba32>(100, 100, Color.Gray.ToPixel<Rgba32>());

        using var resp = await Program.HttpClient.GetAsync(GetAvatarUrl(openId, spec));
        await using var stream = await resp.Content.ReadAsStreamAsync();

        return await Image.LoadAsync(stream);
    }

    public static Image GetUserAvatar(string openId, int spec = 100)
    {
        if (string.IsNullOrWhiteSpace(openId))
            return new Image<Rgba32>(100, 100, Color.Gray.ToPixel<Rgba32>());

        using var resp = Program.HttpClient.GetAsync(GetAvatarUrl(openId, spec)).Result;
        using var stream = resp.Content.ReadAsStream();

        return Image.Load(stream);
    }
}
