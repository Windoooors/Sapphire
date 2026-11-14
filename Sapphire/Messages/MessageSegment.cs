namespace Sapphire.Messages;

/// <summary>
///     一条待发送消息中的片段。命令层可以把文本和图片按顺序放进同一个集合，
///     由 <see cref="Commands.CommandBase" /> 按官方 API 的能力拆成文本/富媒体消息发送。
/// </summary>
public abstract record MessageSegment;
