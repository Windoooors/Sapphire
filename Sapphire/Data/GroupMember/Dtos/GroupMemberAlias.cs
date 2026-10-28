namespace Sapphire.Data.GroupMember.Dtos;

public record GroupMemberAlias
{
    public required string Alias { get; init; }
    public long UserId { get; init; }
    public long GroupId { get; init; }
    public int MemberAliasId { get; init; }
}