namespace Agent.Api.Documents;

public sealed class DocumentUploadResponse
{
    public bool Success { get; init; }

    public Guid DocumentId { get; init; }

    public string FileName { get; init; } = string.Empty;

    public int ChunkCount { get; init; }

    public string Message { get; init; } = string.Empty;
}
