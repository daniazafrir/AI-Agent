namespace Agent.Api.Chat;

public sealed class ChatCompletionResult
{
    public string AssistantMessage { get; init; } = string.Empty;

    public IReadOnlyCollection<string> UsedTools { get; init; }
        = [];

    public int PromptTokens { get; init; }

    public int CompletionTokens { get; init; }
}