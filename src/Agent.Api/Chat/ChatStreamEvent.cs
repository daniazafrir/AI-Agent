using Agent.Api.Features.Conversation;

namespace Agent.Api.Chat;

public sealed record ChatStreamEvent
{
    public required string Type { get; init; }

    public string? Content { get; init; }

    public string? ToolName { get; init; }

    public IReadOnlyList<string>? UsedTools { get; init; }

    public Guid? ConversationId { get; init; }
    public IReadOnlyList<KnowledgeSource>? Sources { get; init; }
}