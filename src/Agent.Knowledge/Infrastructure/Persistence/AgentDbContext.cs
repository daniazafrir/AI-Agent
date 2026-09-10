using Agent.Knowledge.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Agent.Knowledge.Infrastructure.Persistence;

public sealed class AgentDbContext
    : DbContext
{
    public AgentDbContext(
        DbContextOptions<AgentDbContext> options)
        : base(options)
    {
    }

    public DbSet<KnowledgeDocument> KnowledgeDocuments =>
        Set<KnowledgeDocument>();

    public DbSet<KnowledgeChunk> KnowledgeChunks =>
        Set<KnowledgeChunk>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AgentDbContext).Assembly);
    }
}