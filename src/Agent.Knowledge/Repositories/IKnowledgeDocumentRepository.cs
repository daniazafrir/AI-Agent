using Agent.Knowledge.Entitites;

namespace Agent.Knowledge.Repositories;

public interface IKnowledgeDocumentRepository
{
    Task<KnowledgeDocument?> FindByHashAsync(
        string contentHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeDocument>> ListAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        KnowledgeDocument document,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<KnowledgeDocument?> GetByIdAsync(
    Guid documentId,
    CancellationToken cancellationToken = default);


}