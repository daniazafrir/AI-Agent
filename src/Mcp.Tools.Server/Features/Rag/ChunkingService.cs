using System.Text.RegularExpressions;

namespace Mcp.Tools.Server.Features.Rag;

public sealed partial class ChunkingService : IChunkingService
{
    public IReadOnlyList<string> Split(string text,int chunkSize=800,int overlap=150)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        if (chunkSize < 100) throw new ArgumentOutOfRangeException(nameof(chunkSize));
        if (overlap < 0 || overlap >= chunkSize) throw new ArgumentOutOfRangeException(nameof(overlap));

        var normalized = WhitespaceRegex().Replace(text," ").Trim();
        var chunks = new List<string>();
        var start = 0;
        while (start < normalized.Length)
        {
            var end = Math.Min(start + chunkSize, normalized.Length);
            if (end < normalized.Length)
            {
                var boundary = normalized.LastIndexOfAny(['.','!','?'], end-1, end-start);
                if (boundary > start + chunkSize/2) end = boundary + 1;
            }
            var chunk = normalized[start..end].Trim();
            if (chunk.Length > 0) chunks.Add(chunk);
            if (end >= normalized.Length) break;
            start = Math.Max(end-overlap,start+1);
        }
        return chunks;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
