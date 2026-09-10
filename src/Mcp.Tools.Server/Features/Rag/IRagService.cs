using Agent.Knowledge.Search.Models;

namespace Mcp.Tools.Server.Features.Rag;

public interface IRagService
{

    Task<DocumentIndexResult> IndexDocumentAsync(
    Guid documentId,
    string fileName,
    IReadOnlyList<string> chunks,
    CancellationToken cancellationToken = default);
    Task<RagSearchResult> SearchAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default);


    Task<bool> DeleteVectorsAsync(
    Guid documentId,
        CancellationToken cancellationToken = default);

   
}
