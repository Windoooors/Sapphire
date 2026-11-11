namespace Sapphire.Data.GroupMember.Dtos;

public class GroupMemberMarriageDto
{
    public int MarriageId { get; init; }
    public long GroupId { get; init; }
    public long UserId { get; init; }
    public long PartnerId { get; init; }
    public DateTime MarriedAt { get; init; }
}
