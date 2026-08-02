using OpenAI.Chat;

namespace Agent.Api.OpenAI;

public sealed class ChatCompletionResult
{
    public ChatFinishReason FinishReason { get; init; }

    public string AssistantMessage { get; init; } = string.Empty;

    public IReadOnlyList<ToolCallResult> ToolCalls { get; init; } =
        Array.Empty<ToolCallResult>();
}