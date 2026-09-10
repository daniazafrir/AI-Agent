using Mcp.Tools.Server.Features.Rag;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace Mcp.Tools.Server.Tools;

[McpServerToolType]
public sealed class KnowledgeTools
{
    [McpServerTool]
    [Description("Search the indexed knowledge base.")]
    public static async Task<string> SearchKnowledgeAsync(
    IRagService ragService,
    string query,
    int topK = 5,
    CancellationToken cancellationToken = default)
    {
        var result =
            await ragService.SearchAsync(
                query,
                topK,
                cancellationToken);

        return JsonSerializer.Serialize(
            new
            {
                success = true,
                query,

                vectorResults =
                    result.VectorResults,

                keywordResults =
                    result.KeywordResults,

                mergedResults =
                    result.MergedResults,

                searchTimeMs =
                    result.SearchTimeMs,

                matchCount =
                    result.Matches.Count,

                matches =
                    result.Matches
            });
    }

    [McpServerTool(Name = "delete_vectors")]
    [Description(
        "Deletes an indexed document from the RAG knowledge base by document ID.")]
    public static Task<bool> DeleteDocumentAsync(
        IRagService ragService,
        [Description("The ID of the document to delete.")]
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return ragService.DeleteVectorsAsync(
            documentId,
            cancellationToken);
    }

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
}