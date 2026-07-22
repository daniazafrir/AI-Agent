using System.ComponentModel;
using Mcp.Tools.Server.Features.Rag;
using ModelContextProtocol.Server;

namespace Mcp.Tools.Server.Tools;

[McpServerToolType]
public sealed class KnowledgeTools
{
    [McpServerTool(Name = "search_knowledge")]
    [Description(
        "Searches uploaded private documents using semantic vector search. " +
        "Use this tool for questions about company documents, policies, " +
        "vacation, remote work, security, sick leave, expenses, " +
        "or any information stored in the knowledge base.")]
    public static async Task<object> SearchKnowledgeAsync(
        IRagService ragService,
        [Description(
            "The complete question or semantic search query.")]
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
}