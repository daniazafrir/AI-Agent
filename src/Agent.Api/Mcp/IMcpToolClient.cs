namespace Agent.Api.Mcp;

public interface IMcpToolClient
{
    Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(
        CancellationToken cancellationToken = default);

    Task<string> CallToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);
    Task<T> CallToolAsync<T>(
    string toolName,
    IReadOnlyDictionary<string, object?> arguments,
    CancellationToken cancellationToken = default);
}