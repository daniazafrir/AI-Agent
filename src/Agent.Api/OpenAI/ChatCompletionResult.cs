using Agent.Api.OpenAI;
using OpenAI.Chat;

public sealed class ChatCompletionResult
{
    public ChatFinishReason FinishReason { get; init; }

    public string? AssistantMessage { get; init; }

    public IReadOnlyList<ToolCallResult> ToolCalls { get; init; }
        = [];

    public ChatCompletion RawCompletion { get; init; }
        = null!;
}