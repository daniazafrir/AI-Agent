using Agent.Knowledge.Infrastructure.Persistence;
using Agent.Knowledge.Search.Keyword;
using Agent.Knowledge.Search.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mcp.Tools.Server.Features.Rag.Keyword;

public sealed class KeywordSearchService(
    AgentDbContext dbContext,
    ILogger<KeywordSearchService> logger)
    : IKeywordSearchService
{
    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        query = query.Trim();

        var results =
            await dbContext.KnowledgeChunks
                .AsNoTracking()
                .Where(x =>
                    EF.Functions
                        .ToTsVector(
                            "english",
                            x.Content)
                        .Matches(
                            EF.Functions.WebSearchToTsQuery(
                                "english",
                                query)))
                .Select(x => new
                {
                    x.DocumentId,
                    DocumentName = x.Document!.FileName,
                    x.ChunkIndex,
                    x.Content,

                    Rank =
                        EF.Functions
                            .ToTsVector(
                                "english",
                                x.Content)
                            .Rank(
                                EF.Functions.WebSearchToTsQuery(
                                    "english",
                                    query))
                })
                .OrderByDescending(x => x.Rank)
                .Take(topK)
                .Select(x =>
                    new KnowledgeSearchResult(
                        x.DocumentId,
                        x.DocumentName,
                        x.ChunkIndex,
                        x.Content,
                        x.Rank,
                        SearchEngineType.Keyword,
                        null))
                .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Keyword search completed. Query={Query}, Results={Count}",
            query,
            results.Count);

        foreach (var result in results)
        {
            logger.LogInformation(
                "Keyword result. Document={Document}, Chunk={Chunk}, Score={Score}",
                result.DocumentName,
                result.ChunkIndex,
                result.Score);
        }

        return results;
    }
}