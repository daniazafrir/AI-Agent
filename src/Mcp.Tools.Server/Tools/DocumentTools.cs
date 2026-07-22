using System.ComponentModel;
using Mcp.Tools.Server.Features.Rag;
using ModelContextProtocol.Server;

namespace Mcp.Tools.Server.Tools;

[McpServerToolType]
public sealed class DocumentTools
{
    [McpServerTool(Name = "index_document")]
    [Description(
        "Indexes a plain-text document in the private knowledge base. " +
        "The document is split into chunks, embedded, and stored in Qdrant.")]
    public static Task<DocumentIndexResult> IndexDocumentAsync(
        IRagService ragService,
        [Description("Original file name, including the .txt extension.")]
        string fileName,
        [Description("Complete UTF-8 plain-text content of the document.")]
        string content,
        CancellationToken cancellationToken = default)
    {
        return ragService.IndexDocumentAsync(
            fileName,
            content,
            cancellationToken);
    }
}
