namespace Mcp.Tools.Server.Features.Rag;

public interface IRagService
{
    Task<DocumentInfo> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<DocumentIndexResult> IndexDocumentAsync(
        string fileName,
        string content,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RagSearchResult>> SearchAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentInfo>> ListDocumentsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}
