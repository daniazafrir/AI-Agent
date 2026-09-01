using Agent.Api.Entitites;

namespace Agent.Api.Knowledge;

public interface IKnowledgeChunkRepository
{
    Task AddRangeAsync(
        IReadOnlyCollection<KnowledgeChunk> chunks,
        CancellationToken cancellationToken);

    Task<KnowledgeChunk?> GetAsync(
        Guid documentId,
        int chunkIndex,
        CancellationToken cancellationToken);

    Task DeleteByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeChunk>> ListByDocumentAsync(
    Guid documentId,
    CancellationToken cancellationToken = default);
}