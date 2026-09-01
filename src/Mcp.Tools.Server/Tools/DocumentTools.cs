using ModelContextProtocol.Server;

namespace Mcp.Tools.Server.Features.Rag;

[McpServerToolType]
public static class RagTools
{
    [McpServerTool(Name = "index_document")]
    public static Task<DocumentIndexResult> IndexDocumentAsync(
        IRagService ragService,
        Guid documentId,
        string fileName,
        IReadOnlyList<string> chunks,
        CancellationToken cancellationToken = default)
    {
        return ragService.IndexDocumentAsync(
            documentId,
            fileName,
            chunks,
            cancellationToken);
    }

    [McpServerTool(Name = "delete_vectors")]
    public static Task<bool> DeleteVectorsAsync(
        IRagService ragService,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return ragService.DeleteVectorsAsync(
            documentId,
            cancellationToken);
    }
}