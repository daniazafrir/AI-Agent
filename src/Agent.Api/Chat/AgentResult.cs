namespace Agent.Api.Features.Chat;

public sealed record AgentResult(
    Guid ConversationId,
    string Answer,
    IReadOnlyList<string> UsedTools);