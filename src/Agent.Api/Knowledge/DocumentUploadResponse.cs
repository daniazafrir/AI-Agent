namespace Agent.Api.Features.Knowledge;

public sealed class DocumentUploadResponse
{
    public bool Success { get; init; }

    public bool AlreadyExists { get; init; }

    public Guid DocumentId { get; init; }

    public string FileName { get; init; } = "";

    public int ChunkCount { get; init; }

    public string Message { get; init; } = "";
}