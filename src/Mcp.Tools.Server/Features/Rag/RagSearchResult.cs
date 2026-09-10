using Agent.Knowledge.Search.Models;

namespace Mcp.Tools.Server.Features.Rag;

public sealed class RagSearchResult
{
    public IReadOnlyList<KnowledgeSearchResult> Matches { get; init; } = [];

    public int VectorResults { get; init; }

    public int KeywordResults { get; init; }

    public int MergedResults { get; init; }

    public long SearchTimeMs { get; init; }
}