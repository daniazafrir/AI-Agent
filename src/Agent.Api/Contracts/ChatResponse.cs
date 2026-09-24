using Agent.Api.Chat.Models;
using Agent.Api.Features.Conversation;

namespace Agent.Api.Contracts;

public sealed class ChatResponse
{
    public Guid ConversationId { get; init; }

    public string Answer { get; init; } = "";

    public string[] UsedTools { get; init; } = [];

    public List<KnowledgeSource> Sources { get; init; } = [];
    public ChatDebugInfo? Debug { get; init; }
    public IReadOnlyList<PromptSnapshot> Prompts { get; init; } = [];

}
