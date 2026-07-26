namespace Agent.Api.Chat;

public sealed class AgentRunResult
{
    public string AssistantMessage { get; init; } = string.Empty;

    public IReadOnlyList<string> UsedTools { get; init; }
        = Array.Empty<string>();
}