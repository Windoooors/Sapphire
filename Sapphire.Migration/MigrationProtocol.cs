namespace Sapphire.Migration;

public sealed record MigrationRequest
{
    public string Type { get; init; } = "migrate";
    public required string RequestId { get; init; }
    public required long QqNumber { get; init; }
    public required string CanonicalUserId { get; init; }
    public required string SceneUserId { get; init; }
    public required string ChatId { get; init; }
    public bool IsGroup { get; init; } = true;
    public long? LegacyGroupId { get; init; }
}

public sealed record MigrationResponse
{
    public string Type { get; init; } = "completed";
    public required string RequestId { get; init; }
    public required bool Success { get; init; }
    public string? Message { get; init; }
    public int Scores { get; init; }
    public int Aliases { get; init; }
    public int Members { get; init; }
    public int Settings { get; init; }
}
