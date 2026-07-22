namespace Mcp.Tools.Server.Features.Rag;

public sealed record VectorChunk(
    Guid Id,
    Guid DocumentId,
    string DocumentName,
    int ChunkIndex,
    string Content,
    ReadOnlyMemory<float> Vector);
