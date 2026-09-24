using Agent.Api.Chat.Models;
using System.Text.Json;

namespace Agent.Api.Chat;

public static class KnowledgeSearchRetry
{
    public static void Observe(AgentContext context, BinaryData arguments, string rawResult)
    {
        if (context.EnglishKnowledgeRetryRequested) return;
        try
        {
            using var args = JsonDocument.Parse(arguments.ToString());
            using var result = JsonDocument.Parse(rawResult);
            var root = result.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                args.RootElement.ValueKind != JsonValueKind.Object) return;
            context.EnglishKnowledgeRetryPending =
                root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True &&
                !(root.TryGetProperty("isError", out var error) && error.ValueKind == JsonValueKind.True) &&
                root.TryGetProperty("matches", out var matches) && matches.ValueKind == JsonValueKind.Array &&
                matches.GetArrayLength() == 0 &&
                args.RootElement.TryGetProperty("query", out var query) && query.ValueKind == JsonValueKind.String &&
                query.GetString()!.Any(c => c >= '\u0590' && c <= '\u05ff');
        }
        catch (JsonException) { context.EnglishKnowledgeRetryPending = false; }
    }
}
