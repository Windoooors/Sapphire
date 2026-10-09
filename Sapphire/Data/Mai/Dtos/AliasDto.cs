using System.ComponentModel.DataAnnotations;

namespace Sapphire.Data.Mai.Dtos;

public record MaiAliasDto
{
    public int AliasId { get; init; }
    
    public long? CreatorUserId { get; init; }
    public required string Alias { get; init; }
    public required MaiSongDto Song { get; init; }
}