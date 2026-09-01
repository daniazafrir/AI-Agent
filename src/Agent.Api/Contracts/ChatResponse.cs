using Agent.Api.Features.Conversation;

namespace Agent.Api.Contracts;

public sealed class ChatResponse
{
    public Guid ConversationId { get; init; }

    public string Answer { get; init; } = "";

    public string[] UsedTools { get; init; } = [];

    public List<KnowledgeSource> Sources { get; init; } = [];
}
