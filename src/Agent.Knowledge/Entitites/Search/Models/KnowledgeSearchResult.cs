namespace Agent.Knowledge.Search.Models;

public sealed class KnowledgeSearchResult
{

    public KnowledgeSearchResult(
    Guid documentId,
    string documentName,
    int chunkIndex,
    string content,
    double score,
    SearchEngineType searchEngine)
    {
        DocumentId = documentId;
        DocumentName = documentName;
        ChunkIndex = chunkIndex;
        Content = content;
        Score = score;
        SearchEngine = searchEngine;
    }
    public Guid DocumentId { get; init; }

    public string DocumentName { get; init; } =
        string.Empty;

    public int ChunkIndex { get; init; }

    public string Content { get; init; } =
        string.Empty;

    public double Score { get; init; }

    public SearchEngineType SearchEngine { get; init; }
}

public enum SearchEngineType
{
    Keyword,
    Vector,
    Hybrid
}