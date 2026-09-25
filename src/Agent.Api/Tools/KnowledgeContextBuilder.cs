using Agent.Api.OpenAI;
using System.Text;
using System.Text.Json;

namespace Agent.Api.Tools;

public sealed record KnowledgeContextChunk(string? DocumentId, string? DocumentName,
    int? ChunkIndex, int ContentCharacters, bool Included, string Reason);
public sealed record KnowledgeContextAnalytics(int ReturnedChunks, int IncludedChunks,
    int OmittedChunks, int ContextCharacters, IReadOnlyList<KnowledgeContextChunk> Chunks);

public static class KnowledgeContextBuilder
{
    public static ToolExecutionResult Build(string raw)
    {
        using var document = JsonDocument.Parse(raw);
        if (!document.RootElement.TryGetProperty("matches", out var matches) || matches.ValueKind != JsonValueKind.Array)
            return new ToolExecutionResult { RawContent = raw, Content = raw };

        var builder = new StringBuilder();
        var chunks = new List<KnowledgeContextChunk>();
        foreach (var match in matches.EnumerateArray())
        {
            var name = String(match, "documentName");
            var content = String(match, "content");
            var indexValue = Property(match, "chunkIndex");
            int? index = indexValue.ValueKind == JsonValueKind.Number && indexValue.TryGetInt32(out var number) ? number : null;
            var included = !string.IsNullOrWhiteSpace(content);
            if (included)
            {
                if (!string.IsNullOrWhiteSpace(name)) builder.AppendLine($"Source: {name}");
                builder.AppendLine(content);
                builder.AppendLine();
            }
            chunks.Add(new(String(match, "documentId"), name, index, content?.Length ?? 0,
                included, included ? "included" : "empty_content"));
        }

        // Preserve the existing raw-JSON fallback; record that accurately too.
        var normalized = builder.ToString().Trim();
        var fallback = normalized.Length == 0;
        var modelContent = fallback ? raw : normalized;
        if (fallback)
            chunks = chunks.Select(chunk => chunk with { Included = true, Reason = "raw_payload_fallback" }).ToList();
        var includedCount = chunks.Count(chunk => chunk.Included);
        return new ToolExecutionResult {
            RawContent = raw, Content = modelContent,
            Analytics = new(chunks.Count, includedCount, chunks.Count - includedCount, modelContent.Length, chunks)
        };
    }

    private static JsonElement Property(JsonElement element, string name) => element.ValueKind == JsonValueKind.Object
        ? element.EnumerateObject().FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Value
        : default;
    private static string? String(JsonElement element, string name)
    {
        var value = Property(element, name);
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
