using Agent.Api.Documents;
using Agent.Api.Entitites;
using System.Threading.Tasks;

namespace Agent.Api.Features.Knowledge;

public interface IKnowledgeDocumentService
{
    Task<DocumentUploadResponse> UploadAsync(
    string fileName,
    Stream stream,
    CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KnowledgeDocument>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<KnowledgeChunkResponse?> GetChunkAsync(
    Guid documentId,
    int chunkIndex,
    CancellationToken cancellationToken = default);

    Task<bool> ReindexAsync(
    Guid documentId,
    CancellationToken cancellationToken = default);
}