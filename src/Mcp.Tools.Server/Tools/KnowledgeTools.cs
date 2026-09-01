using Mcp.Tools.Server.Features.Rag;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Mcp.Tools.Server.Tools;

[McpServerToolType]
public sealed class KnowledgeTools
{
    [McpServerTool(Name = "search_knowledge")]
    [Description(
        """
        Search the uploaded and indexed knowledge-base documents.

        Use this tool ONLY when the user's question is likely to be answered
        by information stored in uploaded documents or internal knowledge.

        Good examples:
        - company policies
        - HR documents
        - employee handbooks
        - contracts
        - procedures
        - vacation or sick-leave policies
        - questions explicitly referring to an uploaded file or document

        Do NOT use this tool for:
        - general knowledge
        - programming concepts
        - definitions
        - mathematics
        - science
        - history
        - public information that does not depend on uploaded documents

        For example:
        - "What is MCP?" -> do NOT use this tool.
        - "What is Angular?" -> do NOT use this tool.
        - "What does the employee handbook say about vacation days?"
          -> use this tool.

        When calling this tool, build a short semantic query that preserves
        the user's original meaning.

        Do not invent additional legal, geographic, temporal, or other
        constraints that the user did not provide.
        """)]
    public static async Task<object> SearchKnowledgeAsync(
        IRagService ragService,
        [Description(
            "A short semantic search query based directly on the user's question.")]
        string query,
        [Description(
            "Maximum number of relevant document chunks to return, between 1 and 10.")]
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                "Query is required.",
                nameof(query));
        }

        topK = Math.Clamp(
            topK,
            1,
            10);

        var matches =
            await ragService.SearchAsync(
                query,
                topK,
                cancellationToken);

        return new
        {
            success = true,
            query,
            matchCount = matches.Count,
            matches
        };
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