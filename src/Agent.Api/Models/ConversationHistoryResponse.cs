namespace Agent.Api.Models;

public sealed record ConversationHistoryItem(
    string Role,
    string Content,
    IReadOnlyList<string> UsedTools,
    DateTimeOffset CreatedAt);

public sealed record ConversationHistoryResponse(
    string ConversationId,
    IReadOnlyList<ConversationHistoryItem> Messages);
