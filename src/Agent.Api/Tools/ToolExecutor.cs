using System.Text.Json;
using Agent.Api.Mcp;

namespace Agent.Api.Tools;

public sealed class ToolExecutor(
    IMcpToolClient mcpToolClient,
    IMcpToolRegistry toolRegistry,
    ILogger<ToolExecutor> logger)
    : IToolExecutor
{
    public async Task<string> ExecuteAsync(
        string toolName,
        BinaryData arguments,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(toolName))
        {
            return CreateError(
                toolName,
                "Tool name is required.");
        }

        await toolRegistry.InitializeAsync(
            cancellationToken);

        if (!toolRegistry.Contains(toolName))
        {
            logger.LogWarning(
                "The model requested unknown MCP tool {ToolName}.",
                toolName);

            return CreateError(
                toolName,
                $"Unknown MCP tool: {toolName}");
        }

        try
        {
            var parsedArguments =
                ParseArguments(arguments);

            logger.LogInformation(
                "Forwarding tool {ToolName} to MCP with arguments {@Arguments}.",
                toolName,
                parsedArguments);

            return await mcpToolClient.CallToolAsync(
                toolName,
                parsedArguments,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Invalid JSON arguments for tool {ToolName}.",
                toolName);

            return CreateError(
                toolName,
                $"Invalid tool arguments: {exception.Message}");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "MCP tool {ToolName} failed.",
                toolName);

            return CreateError(
                toolName,
                exception.Message);
        }
    }

    private static IReadOnlyDictionary<string, object?>
        ParseArguments(BinaryData arguments)
    {
        using var document =
            JsonDocument.Parse(arguments);

        if (document.RootElement.ValueKind
            != JsonValueKind.Object)
        {
            throw new JsonException(
                "Tool arguments must be a JSON object.");
        }

        var result =
            new Dictionary<string, object?>(
                StringComparer.Ordinal);

        foreach (var property in
                 document.RootElement.EnumerateObject())
        {
            result[property.Name] =
                ConvertJsonValue(property.Value);
        }

        return result;
    }

    private static object? ConvertJsonValue(
        JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String =>
                value.GetString(),

            JsonValueKind.Number
                when value.TryGetInt64(out var integer) =>
                integer,

            JsonValueKind.Number =>
                value.GetDouble(),

            JsonValueKind.True =>
                true,

            JsonValueKind.False =>
                false,

            JsonValueKind.Null =>
                null,

            JsonValueKind.Object =>
                value.EnumerateObject()
                    .ToDictionary(
                        property => property.Name,
                        property =>
                            ConvertJsonValue(property.Value)),

            JsonValueKind.Array =>
                value.EnumerateArray()
                    .Select(ConvertJsonValue)
                    .ToList(),

            _ =>
                value.GetRawText()
        };
    }

    private static string CreateError(
        string? toolName,
        string message)
    {
        return JsonSerializer.Serialize(
            new
            {
                success = false,
                tool = toolName,
                error = message
            });
    }
}