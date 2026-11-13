using QQBot;

namespace Sapphire.Commands;

/// <summary>
///     与具体接入协议无关的一条消息上下文。
/// </summary>
/// <remarks>
///     <see cref="SceneUserId" /> 是官方开放平台按场景下发的 OpenID（群消息里是 member_openid，
///     单聊里是 user_openid）；<see cref="UserId" /> 是跨场景统一后的用户标识，用于成绩、DivingFish、
///     WCK 等需要跨群一致的数据。
/// </remarks>
public sealed class MessageContext
{
    public required bool IsGroup { get; init; }

    /// <summary>
    ///     群聊为 group_openid，单聊为 user_openid。
    /// </summary>
    public required string ChatId { get; init; }

    /// <summary>
    ///     当前场景下的用户 OpenID。
    /// </summary>
    public required string SceneUserId { get; init; }

    /// <summary>
    ///     跨场景统一的用户标识。无法统一时退化为 <see cref="SceneUserId" />。
    /// </summary>
    public required string UserId { get; init; }

    public required string MessageId { get; init; }

    public required string Content { get; init; }

    public required IMessageChannel Channel { get; init; }

    /// <summary>
    ///     触发本次处理的原消息，用于被动回复。
    /// </summary>
    public required IUserMessage Message { get; init; }

    public string? UserName { get; init; }

    public string? GroupNickname { get; init; }

    /// <summary>
    ///     群成员角色：member / admin / owner。
    /// </summary>
    public string? MemberRole { get; init; }

    public string? UnionOpenId { get; init; }

    public string? Avatar { get; init; }

    public string DisplayName => GroupNickname ?? UserName ?? $"用户{SceneUserId[..Math.Min(6, SceneUserId.Length)]}";
}
