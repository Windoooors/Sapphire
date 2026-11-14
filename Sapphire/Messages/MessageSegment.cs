namespace Sapphire.Messages;

/// <summary>
///     一条待发送消息中的片段。命令层可以把文本和图片按顺序放进同一个集合，
///     由 <see cref="Sapphire.Commands.CommandBase" /> 按官方 API 的能力发送；
///     单图时文本作为 content 与 attachment 一起发送。
/// </summary>
public abstract record MessageSegment;
