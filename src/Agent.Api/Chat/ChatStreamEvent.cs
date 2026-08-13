namespace Agent.Api.Chat;

public sealed record ChatStreamEvent
{
    public required string Type { get; init; }

    public string? Content { get; init; }

    public string? ToolName { get; init; }

    public IReadOnlyList<string>? UsedTools { get; init; }
}