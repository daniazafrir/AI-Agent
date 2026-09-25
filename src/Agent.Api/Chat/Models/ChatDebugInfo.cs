namespace Agent.Api.Chat.Models;

public sealed class ChatDebugInfo
{
    public string ToolName { get; init; } = string.Empty;

    public string Query { get; init; } = string.Empty;

    public int RawVectorResults { get; init; }

    public int RelevantVectorResults { get; init; }

    public int VectorResults { get; init; }

    public int KeywordResults { get; init; }

    public int MergedResults { get; init; }

    public double MinimumVectorScore { get; init; }

    public long SearchTimeMs { get; init; }
    public long? EmbeddingTimeMs { get; init; }
    public long? VectorSearchTimeMs { get; init; }
    public long? KeywordSearchTimeMs { get; init; }
    public long? RankingTimeMs { get; init; }
    public IReadOnlyList<ChatSourceInfo> Matches { get; init; } = [];
}
public sealed class ChatSourceInfo
{
    public string DocumentName { get; init; } = string.Empty;

    public int ChunkIndex { get; init; }

    public double Score { get; init; }

    public string SearchEngine { get; init; } = string.Empty;
}
