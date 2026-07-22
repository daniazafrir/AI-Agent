using System.Text.Json;

namespace Agent.Api.Mcp;

public sealed record McpToolDefinition(
    string Name,
    string Description,
    JsonElement InputSchema);