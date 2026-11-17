using System.ComponentModel.DataAnnotations;

namespace Sapphire.Data.GroupMember.Dtos;

public class KnownNameDto
{
    [MaxLength(64)] public string UserId { get; init; } = "";

    [MaxLength(128)] public string DisplayName { get; set; } = "";

    public DateTime UpdatedAt { get; set; }
}
