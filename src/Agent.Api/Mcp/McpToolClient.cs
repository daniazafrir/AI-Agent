using System.Text.Json;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Agent.Api.Mcp;

public sealed class McpToolClient(
    IOptions<McpOptions> options,
    ILoggerFactory loggerFactory,
    ILogger<McpToolClient> logger)
    : IMcpToolClient
{
    private readonly McpOptions _options = options.Value;

    public async Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var client =
            await CreateClientAsync(cancellationToken);

        var tools = await client.ListToolsAsync(
            cancellationToken: cancellationToken);

        return tools
            .Select(tool => new McpToolDefinition(
                tool.Name,
                tool.Description ?? string.Empty,
                tool.JsonSchema.Clone()))
            .OrderBy(tool => tool.Name)
            .ToList();
    }

    public async Task<string> CallToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException(
                "Tool name is required.",
                nameof(toolName));
        }

        logger.LogInformation(
            "Calling MCP tool {ToolName} at {ServerUrl}.",
            toolName,
            _options.ServerUrl);

        await using var client =
            await CreateClientAsync(cancellationToken);

        var result = await client.CallToolAsync(
            toolName,
            arguments,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "MCP tool {ToolName} completed. IsError: {IsError}.",
            toolName,
            result.IsError);

        var textParts = result.Content
            .OfType<TextContentBlock>()
            .Select(content => content.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        if (textParts.Count > 0)
        {
            return string.Join(
                Environment.NewLine,
                textParts);
        }

        return JsonSerializer.Serialize(result);
    }

    private async Task<McpClient> CreateClientAsync(
        CancellationToken cancellationToken)
    {
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(_options.ServerUrl),
                TransportMode = HttpTransportMode.StreamableHttp,
                ConnectionTimeout = _options.ConnectionTimeout
            },
            loggerFactory);

        return await McpClient.CreateAsync(
            transport,
            cancellationToken: cancellationToken);
    }
}