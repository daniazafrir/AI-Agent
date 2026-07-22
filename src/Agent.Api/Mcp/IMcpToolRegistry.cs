using OpenAI.Chat;

namespace Agent.Api.Mcp;

public interface IMcpToolRegistry
{
    IReadOnlyList<ChatTool> Definitions { get; }

    IReadOnlyCollection<string> ToolNames { get; }

    Task InitializeAsync(
        CancellationToken cancellationToken = default);

    bool Contains(string toolName);
}