namespace Agent.Api.Entitites;

public sealed class KnowledgeDocument
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = "";

    public long SizeBytes { get; set; }

    public int ChunkCount { get; set; }

    public string ContentHash { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<KnowledgeChunk> Chunks { get; set; }
    = new List<KnowledgeChunk>();
}
