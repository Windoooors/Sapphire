using System.ComponentModel.DataAnnotations;

namespace Sapphire.Data.Settings.Dtos;

public record SettingsStorageItemDto
{
    public bool IsGroup { get; init; }

    [MaxLength(64)] public string ChatId { get; init; } = "";

    [MaxLength(128)] public string? Identifier { get; init; }

    public bool Value { get; set; }
}
