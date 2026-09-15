using Agent.Knowledge.Entitites;
using Agent.Knowledge.Infrastructure.Persistence;
using Agent.Knowledge.Search.Models;
using Microsoft.EntityFrameworkCore;

namespace Agent.Knowledge.Repositories;

public sealed class PostgresKnowledgeChunkRepository(
    AgentDbContext dbContext)
    : IKnowledgeChunkRepository
{
    public async Task AddRangeAsync(
        IReadOnlyCollection<KnowledgeChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
        {
            return;
        }

        await dbContext.KnowledgeChunks
            .AddRangeAsync(
                chunks,
                cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public Task<KnowledgeChunk?> GetAsync(
        Guid documentId,
        int chunkIndex,
        CancellationToken cancellationToken = default)
    {
        return dbContext.KnowledgeChunks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.DocumentId == documentId &&
                    x.ChunkIndex == chunkIndex,
                cancellationToken);
    }

    public async Task DeleteByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        await dbContext.KnowledgeChunks
            .Where(x =>
                x.DocumentId == documentId)
            .ExecuteDeleteAsync(
                cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeChunk>> ListByDocumentAsync(
    Guid documentId,
    CancellationToken cancellationToken = default)
    {
        return await dbContext.KnowledgeChunks
            .AsNoTracking()
            .Where(x => x.DocumentId == documentId)
            .OrderBy(x => x.ChunkIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeSearchResult>>
    SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        return await dbContext.KnowledgeChunks
            .AsNoTracking()
            .Where(x =>
                EF.Functions.ILike(
                    x.Content,
                    $"%{query}%"))
            .OrderBy(x => x.ChunkIndex)
            .Take(topK)
            .Select(x =>
                new KnowledgeSearchResult(
                    x.DocumentId,
                    x.Document!.FileName,
                    x.ChunkIndex,
                    x.Content,
                    1.0,
                    SearchEngineType.Keyword,
                    null
                )
)
            .ToListAsync(
                cancellationToken);
    }
}