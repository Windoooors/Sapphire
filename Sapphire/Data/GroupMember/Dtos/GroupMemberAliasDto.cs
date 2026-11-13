using System.ComponentModel.DataAnnotations;

namespace Sapphire.Data.GroupMember.Dtos;

public record GroupMemberAliasDto
{
    public required string Alias { get; init; }

    /// <summary>
    ///     跨场景统一的用户标识（canonical）。
    /// </summary>
    [MaxLength(64)]
    public string UserId { get; init; } = "";

    /// <summary>
    ///     当前群内该用户的 member_openid，用于头像/昵称等场景数据。
    /// </summary>
    [MaxLength(64)]
    public string SceneUserId { get; init; } = "";

    [MaxLength(64)] public string GroupId { get; init; } = "";

    public int MemberAliasId { get; init; }
}
