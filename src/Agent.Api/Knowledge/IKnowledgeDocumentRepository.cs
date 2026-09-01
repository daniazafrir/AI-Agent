using Agent.Api.Entitites;

namespace Agent.Api.Features.Knowledge;

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