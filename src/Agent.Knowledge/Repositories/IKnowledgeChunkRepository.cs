using Agent.Knowledge.Entitites;
using Agent.Knowledge.Search.Models;

namespace Agent.Knowledge.Repositories;


public interface IKnowledgeChunkRepository
{
    Task AddRangeAsync(
        IReadOnlyCollection<KnowledgeChunk> chunks,
        CancellationToken cancellationToken = default);

    Task<KnowledgeChunk?> GetAsync(
        Guid documentId,
        int chunkIndex,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeChunk>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task DeleteByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default);
}