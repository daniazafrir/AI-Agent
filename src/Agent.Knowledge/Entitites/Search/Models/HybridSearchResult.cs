namespace Agent.Knowledge.Search.Models;

public sealed class HybridSearchResult
{
    public IReadOnlyList<KnowledgeSearchResult> Matches { get; init; } = [];

    public int VectorResults { get; init; }

    public int KeywordResults { get; init; }

    public int MergedResults { get; init; }

    public long SearchTimeMs { get; init; }
}