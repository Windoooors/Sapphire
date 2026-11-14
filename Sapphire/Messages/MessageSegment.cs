namespace Sapphire.Messages;

/// <summary>
///     一条待发送消息中的片段。
/// </summary>
/// <remarks>
///     与 EleCho.GoCqHttpSdk 的 <c>CqMsg</c> 相同，基类提供从 <see cref="string" /> 的隐式转换，
///     因此集合表达式里的字符串字面量会直接成为 <see cref="TextMessage" />：
///     <c>["文字", new ImageMessage(base64), "文字"]</c>。
///     片段按顺序放进 <see cref="MessageChain" />，由 <see cref="Sapphire.Commands.CommandBase" /> 负责发送。
/// </remarks>
public abstract record MessageSegment
{
    /// <summary>
    ///     字符串可以直接作为文本片段使用。
    /// </summary>
    public static implicit operator MessageSegment(string text)
    {
        return new TextMessage(text);
    }
}