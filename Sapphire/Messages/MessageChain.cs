namespace Sapphire.Messages;

/// <summary>
///     一条图文混排的消息链。
/// </summary>
/// <remarks>
///     <para>
///         借鉴 EleCho.GoCqHttpSdk 的 <c>CqMessage</c>：继承 <see cref="List{T}" /> 因而带有 <c>Add</c>，
///         可以作为集合表达式的目标类型，元素类型由编译器缺省推断为 <see cref="MessageSegment" />；
///         再配合 <see cref="MessageSegment" /> 上从 <see cref="string" /> 的隐式转换，
///         就能直接写 <c>["文字", new ImageMessage(base64), "文字"]</c>，不必手动包 <see cref="TextMessage" />。
///     </para>
///     <para>
///         官方 API 一条 media 消息只能带一个附件，因此发送时并不是按片段逐条发：
///         <see cref="Sapphire.Commands.CommandBase" /> 会把链里的文字合并成 content，
///         把图片作为 attachment，[文字, 图片, 文字] 最终是一条带说明的图片消息。
///     </para>
/// </remarks>
public sealed class MessageChain : List<MessageSegment>
{
    public MessageChain()
    {
    }

    public MessageChain(int capacity) : base(capacity)
    {
    }

    public MessageChain(string text) : base(1)
    {
        Add(new TextMessage(text));
    }

    public MessageChain(MessageSegment segment) : base(1)
    {
        Add(segment);
    }

    public MessageChain(params MessageSegment[] segments) : base(segments)
    {
    }

    public MessageChain(IEnumerable<MessageSegment> segments) : base(segments)
    {
    }

    /// <summary>
    ///     链中所有文本片段按顺序拼接后的内容。
    /// </summary>
    public string Text => string.Concat(this.OfType<TextMessage>().Select(textMessage => textMessage.Text));
}