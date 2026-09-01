namespace Agent.Api.OpenAI;

public sealed class ToolExecutionResult
{
    public string Content { get; init; } = string.Empty;

    public string RawContent { get; init; } = string.Empty;
}