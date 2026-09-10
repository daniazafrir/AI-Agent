using Agent.Api.Features.Conversation;

namespace Agent.Api.Chat.Models;

public sealed class AgentRunResult
{
    public string AssistantMessage { get; init; } =
        string.Empty;

    public IReadOnlyList<string> UsedTools { get; init; } =
        [];

    public List<KnowledgeSource> Sources { get; init; } = [];
}