using OpenAI.Chat;

namespace Agent.Api.Mcp;

public sealed class McpToolRegistry(
    IMcpToolClient mcpToolClient,
    ILogger<McpToolRegistry> logger)
    : IMcpToolRegistry
{
    private readonly SemaphoreSlim _initializationLock =
        new(1, 1);

    private IReadOnlyList<ChatTool> _definitions =
        Array.Empty<ChatTool>();

    private HashSet<string> _toolNames =
        new(StringComparer.Ordinal);

    private bool _initialized;

    public IReadOnlyList<ChatTool> Definitions =>
        _definitions;

    public IReadOnlyCollection<string> ToolNames =>
        _toolNames;

    public bool Contains(string toolName)
    {
        return _toolNames.Contains(toolName);
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(
            cancellationToken);

        try
        {
            if (_initialized)
            {
                return;
            }

            logger.LogInformation(
                "Loading MCP tool definitions.");

            var mcpTools =
                await mcpToolClient.ListToolsAsync(
                    cancellationToken);

            var chatTools = new List<ChatTool>();
            var toolNames = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (var tool in mcpTools)
            {
                ValidateTool(tool);

                var chatTool =
                    ChatTool.CreateFunctionTool(
                        functionName: tool.Name,
                        functionDescription:
                            string.IsNullOrWhiteSpace(tool.Description)
                                ? $"Execute MCP tool '{tool.Name}'."
                                : tool.Description,
                        functionParameters:
                            BinaryData.FromString(
                                tool.InputSchema.GetRawText()));

                chatTools.Add(chatTool);
                toolNames.Add(tool.Name);

                logger.LogInformation(
                    "Registered MCP tool {ToolName}.",
                    tool.Name);
            }

            _definitions = chatTools;
            _toolNames = toolNames;
            _initialized = true;

            logger.LogInformation(
                "Loaded {ToolCount} MCP tools: {ToolNames}.",
                _definitions.Count,
                string.Join(", ", _toolNames));
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private static void ValidateTool(
        McpToolDefinition tool)
    {
        if (string.IsNullOrWhiteSpace(tool.Name))
        {
            throw new InvalidOperationException(
                "MCP returned a tool without a name.");
        }

        if (tool.InputSchema.ValueKind is
            System.Text.Json.JsonValueKind.Undefined
            or System.Text.Json.JsonValueKind.Null)
        {
            throw new InvalidOperationException(
                $"MCP tool '{tool.Name}' does not contain an input schema.");
        }
    }
}