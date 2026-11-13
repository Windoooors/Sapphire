using System.ComponentModel.DataAnnotations;

namespace Sapphire.Data.GroupMember.Dtos;

public class GroupMemberMarriageDto
{
    public int MarriageId { get; init; }

    [MaxLength(64)] public string GroupId { get; init; } = "";

    [MaxLength(64)] public string UserId { get; init; } = "";

    [MaxLength(64)] public string PartnerId { get; init; } = "";

    public DateTime MarriedAt { get; init; }
}
