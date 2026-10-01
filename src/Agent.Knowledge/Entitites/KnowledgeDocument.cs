namespace Agent.Knowledge.Entitites;

public sealed class KnowledgeDocument
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = "";

    public long SizeBytes { get; set; }

    public int ChunkCount { get; set; }

    public string ContentHash { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string? IndexingDetailsJson { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DocumentIndexingDetails? IndexingDetails
    {
        get => IndexingDetailsJson is null ? null : System.Text.Json.JsonSerializer.Deserialize<DocumentIndexingDetails>(IndexingDetailsJson);
        set => IndexingDetailsJson = value is null ? null : System.Text.Json.JsonSerializer.Serialize(value);
    }

    public ICollection<KnowledgeChunk> Chunks { get; set; }
    = new List<KnowledgeChunk>();
}
public sealed record DocumentIndexingDetails(DateTime IndexedAtUtc, string? EmbeddingModel,
    int? EmbeddingDimensions, int? ChunkSize, int? ChunkOverlap, string ChunkingMode);
