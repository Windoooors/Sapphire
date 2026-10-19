namespace Sapphire.Data.Mai.Dtos;

public record MaiCategoryDto
{
    public int CategoryIndex { get; init; }
    public string? Name { get; init; }
}