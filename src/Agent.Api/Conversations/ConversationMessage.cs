namespace Agent.Api.Conversations;

public sealed record ConversationMessage(
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> UsedTools);
