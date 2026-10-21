using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace Sapphire.ImageGeneration;

public static class ImageGeneratorFunctions
{
    public static async Task<string> ToBase64Async(Image image)
    {
        using var ms = new MemoryStream();

        await image.SaveAsync(ms, new PngEncoder());

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

    public static async Task<Image> GetUserAvatarAsync(long userId, int spec = 100)
    {
        var url = $"http://q.qlogo.cn/headimg_dl?dst_uin={userId}&spec={spec}&img_type=jpg";

        var resp = await Program.HttpClient.GetAsync(url);
        var stream = await resp.Content.ReadAsStreamAsync();

        return await Image.LoadAsync(stream);
    }

    public static Image GetUserAvatar(long userId, int spec = 100)
    {
        var url = $"http://q.qlogo.cn/headimg_dl?dst_uin={userId}&spec={spec}&img_type=jpg";

        var resp = Program.HttpClient.GetAsync(url).Result;
        var stream = resp.Content.ReadAsStream();

        return Image.Load(stream);
    }
}