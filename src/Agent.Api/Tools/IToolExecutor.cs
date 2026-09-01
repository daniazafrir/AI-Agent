using Agent.Api.OpenAI;

namespace Agent.Api.Tools;

public interface IToolExecutor
{
    Task<ToolExecutionResult> ExecuteAsync(
        string toolName,
        BinaryData arguments,
        CancellationToken cancellationToken = default);
}