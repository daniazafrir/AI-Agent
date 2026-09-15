namespace Agent.Knowledge.Search.Models;

public sealed class KnowledgeSearchResult
{
    public KnowledgeSearchResult(
        Guid documentId,
        string documentName,
        int chunkIndex,
        string content,
        double score,
        SearchEngineType searchEngine,
        double? vectorScore = null)
    {
        DocumentId = documentId;
        DocumentName = documentName;
        ChunkIndex = chunkIndex;
        Content = content;
        Score = score;
        SearchEngine = searchEngine;
        VectorScore = vectorScore;
    }

    public Guid DocumentId { get; init; }

    public string DocumentName { get; init; } =
        string.Empty;

    public int ChunkIndex { get; init; }

    public string Content { get; init; } =
        string.Empty;

    /// <summary>
    /// Ranking score used by the current search stage.
    ///
    /// For raw vector results this is the Qdrant score.
    /// After RRF this becomes the RRF score.
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Original vector similarity returned by Qdrant.
    ///
    /// Null when the result did not originate from vector search.
    /// This value is preserved after RRF.
    /// </summary>
    public double? VectorScore { get; init; }

    public SearchEngineType SearchEngine { get; init; }
}

public enum SearchEngineType
{
    Keyword,
    Vector,
    Hybrid
}