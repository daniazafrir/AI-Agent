namespace Agent.Api.OpenAI;

public sealed class ToolExecutionResult
{
    public Agent.Api.Tools.KnowledgeContextAnalytics? Analytics { get; init; }
    public string Content { get; init; } = string.Empty;

    public string RawContent { get; init; } = string.Empty;
}
