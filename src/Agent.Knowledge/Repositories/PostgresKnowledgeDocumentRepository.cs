using Agent.Knowledge.Entitites;
using Agent.Knowledge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Agent.Knowledge.Repositories;

public sealed class PostgresKnowledgeDocumentRepository(
    AgentDbContext dbContext)
    : IKnowledgeDocumentRepository
{
    public Task<KnowledgeDocument?> FindByHashAsync(
        string contentHash,
        CancellationToken cancellationToken = default)
    {
        return dbContext.KnowledgeDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ContentHash == contentHash,
                cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.KnowledgeDocuments
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        KnowledgeDocument document,
        CancellationToken cancellationToken = default)
    {
        dbContext.KnowledgeDocuments.Add(document);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document =
            await dbContext.KnowledgeDocuments
                .FirstOrDefaultAsync(
                    x => x.Id == documentId,
                    cancellationToken);

        if (document is null)
        {
            return false;
        }

        dbContext.KnowledgeDocuments.Remove(document);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public Task<KnowledgeDocument?> GetByIdAsync(
    Guid documentId,
    CancellationToken cancellationToken = default)
    {
        return dbContext.KnowledgeDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == documentId,
                cancellationToken);
    }
}