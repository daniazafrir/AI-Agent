using Agent.Api.Mcp;
using Agent.Api.OpenAI;
using System.Text;
using System.Text.Json;

namespace Agent.Api.Tools;

public sealed class ToolExecutor(
    IMcpToolClient mcpToolClient,
    IMcpToolRegistry toolRegistry,
    ILogger<ToolExecutor> logger)
    : IToolExecutor
{
    public async Task<ToolExecutionResult> ExecuteAsync(
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

            var rawResult =
     await mcpToolClient.CallToolAsync(
         toolName,
         parsedArguments,
         cancellationToken);

            var normalizedResult =
                NormalizeToolResult(
                    toolName,
                    rawResult);

            return new ToolExecutionResult
            {
                RawContent = rawResult,
                Content = normalizedResult
            };
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            logger.LogError(
                exception,
                "MCP tool {ToolName} timed out or canceled independently of the request.",
                toolName);

            return CreateError(
                toolName,
                "The MCP tool timed out before returning a result.");
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

    private static ToolExecutionResult CreateError(
    string? toolName,
    string message)
    {
        var errorJson =
            JsonSerializer.Serialize(
                new
                {
                    success = false,
                    tool = toolName,
                    error = message
                });

        return new ToolExecutionResult
        {
            RawContent = errorJson,
            Content = errorJson
        };
    }
    private static string NormalizeToolResult(
    string toolName,
    string rawResult)
    {
        if (!string.Equals(
                toolName,
                "search_knowledge",
                StringComparison.OrdinalIgnoreCase))
        {
            return rawResult;
        }

        try
        {
            using var document =
                JsonDocument.Parse(rawResult);

            if (!document.RootElement.TryGetProperty(
                    "matches",
                    out var matches) ||
                matches.ValueKind != JsonValueKind.Array)
            {
                return rawResult;
            }

            var builder =
                new StringBuilder();

            foreach (var match in matches.EnumerateArray())
            {
                var documentName =
                    TryGetPropertyIgnoreCase(
                        match,
                        "documentName",
                        out var documentNameElement)
                        ? documentNameElement.GetString()
                        : null;

                var content =
                    TryGetPropertyIgnoreCase(
                        match,
                        "content",
                        out var contentElement)
                        ? contentElement.GetString()
                        : null;

                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(documentName))
                {
                    builder.AppendLine(
                        $"Source: {documentName}");
                }

                builder.AppendLine(content);
                builder.AppendLine();
            }

            var normalized =
                builder.ToString().Trim();

            return string.IsNullOrWhiteSpace(normalized)
                ? rawResult
                : normalized;
        }
        catch (JsonException)
        {
            return rawResult;
        }
    }

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }


}
