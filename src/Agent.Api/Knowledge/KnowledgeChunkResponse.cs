namespace Agent.Api.Features.Knowledge;

public sealed class KnowledgeChunkResponse
{
    public Guid DocumentId { get; init; }

    public string DocumentName { get; init; } =
        string.Empty;

    public int ChunkIndex { get; init; }

    public string Content { get; init; } =
        string.Empty;
}