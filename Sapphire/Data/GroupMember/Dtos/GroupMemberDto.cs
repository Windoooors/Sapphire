using System.ComponentModel.DataAnnotations;

namespace Sapphire.Data.GroupMember.Dtos;

public class GroupMemberDto
{
    [MaxLength(64)] public string GroupId { get; init; } = "";

    [MaxLength(64)] public string UserId { get; init; } = "";

    [MaxLength(64)] public string SceneUserId { get; set; } = "";

    public int MemberId { get; init; }
}
