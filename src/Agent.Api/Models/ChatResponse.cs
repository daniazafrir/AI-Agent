namespace Agent.Api.Contracts;

public sealed class ChatResponse
{
    public Guid ConversationId { get; init; }

    // Kept as Answer for compatibility with the Angular UI.
    public string Answer { get; init; } = string.Empty;

    public IReadOnlyList<string> UsedTools { get; init; } = Array.Empty<string>();
}
