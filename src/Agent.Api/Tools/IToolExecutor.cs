namespace Agent.Api.Tools;

public interface IToolExecutor
{
    Task<string> ExecuteAsync(
        string toolName,
        BinaryData arguments,
        CancellationToken cancellationToken = default);
}