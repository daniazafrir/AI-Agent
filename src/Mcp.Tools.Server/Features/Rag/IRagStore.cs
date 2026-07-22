namespace Mcp.Tools.Server.Features.Rag;

public interface IRagStore
{
    Task SaveAsync(DocumentInfo document,IReadOnlyList<DocumentChunk> chunks,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<DocumentInfo>> ListDocumentsAsync(CancellationToken cancellationToken=default);
    Task<IReadOnlyList<DocumentChunk>> GetAllChunksAsync(CancellationToken cancellationToken=default);
    Task<bool> DeleteDocumentAsync(Guid documentId,CancellationToken cancellationToken=default);
}
