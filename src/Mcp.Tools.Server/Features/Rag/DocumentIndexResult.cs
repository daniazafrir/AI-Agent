namespace Mcp.Tools.Server.Features.Rag;

public sealed class DocumentIndexResult
{
    public Guid DocumentId { get; init; }

    public string FileName { get; init; } = string.Empty;

    public int ChunkCount { get; init; }

    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;
}