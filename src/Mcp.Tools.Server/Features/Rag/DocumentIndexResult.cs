namespace Mcp.Tools.Server.Features.Rag;

public sealed class DocumentIndexResult
{
    public bool Success { get; init; }

    public Guid DocumentId { get; init; }

    public int ChunkCount { get; init; }

    public string Message { get; init; } = string.Empty;
}