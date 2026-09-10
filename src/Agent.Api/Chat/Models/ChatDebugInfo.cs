namespace Agent.Api.Chat.Models;

public sealed class ChatDebugInfo
{
    public string ToolName { get; init; } = string.Empty;

    public string Query { get; init; } = string.Empty;

    public int VectorResults { get; init; }

    public int KeywordResults { get; init; }

    public int MergedResults { get; init; }

    public long SearchTimeMs { get; init; }
}

public sealed class ChatSourceInfo
{
    public string DocumentName { get; init; } = string.Empty;

    public int ChunkIndex { get; init; }

    public double Score { get; init; }

    public string SearchEngine { get; init; } = string.Empty;
}