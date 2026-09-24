using Agent.Api.Features.Conversation;

namespace Agent.Api.Chat.Models;

public sealed class AgentRunResult
{
    public IReadOnlyList<PromptSnapshot> Prompts { get; init; } = [];
    public ChatDebugInfo? Debug { get; init; }
    public string AssistantMessage { get; init; } =
        string.Empty;

    public IReadOnlyList<string> UsedTools { get; init; } =
        [];

    public List<KnowledgeSource> Sources { get; init; } = [];
}
