namespace Mcp.Tools.Server.Features.Rag;

public interface IRagDocumentStore
{
    Task SaveAsync(
        DocumentInfo document,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentInfo>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}
