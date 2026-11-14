namespace Sapphire.Messages;

public sealed record ImageMessage : MessageSegment
{
    public ImageMessage(string base64, string filename = "image.jpg")
    {
        Base64 = base64;
        Filename = filename;
    }

    public string Base64 { get; }

    public string Filename { get; }
}
