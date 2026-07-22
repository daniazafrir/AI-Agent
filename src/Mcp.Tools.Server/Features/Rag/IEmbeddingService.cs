namespace Mcp.Tools.Server.Features.Rag;

public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> CreateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}
