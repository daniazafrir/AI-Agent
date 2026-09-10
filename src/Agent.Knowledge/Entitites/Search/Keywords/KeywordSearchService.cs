using Agent.Knowledge.Infrastructure.Persistence;
using Agent.Knowledge.Search.Models;
using Microsoft.EntityFrameworkCore;

namespace Agent.Knowledge.Search.Keyword;

public sealed class KeywordSearchService(
    AgentDbContext dbContext)
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

        return await dbContext.KnowledgeChunks
            .AsNoTracking()
            .Include(x => x.Document)
            .Where(x =>
                EF.Functions
                    .ToTsVector(
                        "english",
                        x.Content)
                    .Matches(
                        EF.Functions
                            .WebSearchToTsQuery(
                                "english",
                                query)))
            .Take(topK)
            .Select(x =>
                new KnowledgeSearchResult(
                    x.DocumentId,
                    x.Document!.FileName,
                    x.ChunkIndex,
                    x.Content,
                    1.0,
                    SearchEngineType.Keyword))
            .ToListAsync(cancellationToken);
    }
}