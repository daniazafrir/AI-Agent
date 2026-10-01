namespace Mcp.Tools.Server.Features.Rag;

public interface IEmbeddingService
{
    string? ModelName => null;
    Task<ReadOnlyMemory<float>> CreateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}
