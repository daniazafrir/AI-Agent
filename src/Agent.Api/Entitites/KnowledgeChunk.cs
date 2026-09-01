namespace Agent.Api.Entitites;

public sealed class KnowledgeChunk
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public KnowledgeDocument? Document { get; set; }
}