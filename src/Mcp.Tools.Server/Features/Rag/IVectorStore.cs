using Agent.Knowledge.Search.Models;

namespace Mcp.Tools.Server.Features.Rag;

public interface IVectorStore
{
    Task EnsureCollectionAsync(CancellationToken cancellationToken = default);

    Task UpsertAsync(
        IReadOnlyList<VectorChunk> chunks,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        ReadOnlyMemory<float> queryVector,
        int topK,
        CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}
