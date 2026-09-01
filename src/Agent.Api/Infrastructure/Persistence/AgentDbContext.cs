using Agent.Api.Entitites;
using Microsoft.EntityFrameworkCore;

namespace Agent.Api.Infrastructure.Persistence;

public sealed class AgentDbContext : DbContext
{
    public AgentDbContext(DbContextOptions<AgentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    public DbSet<KnowledgeChunk> KnowledgeChunks =>
    Set<KnowledgeChunk>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments =>
    Set<KnowledgeDocument>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("Conversations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.ToTable("ConversationMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Role).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.ConversationId, x.CreatedAtUtc });

            entity.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>()
         .HasIndex(x => x.ContentHash)
         .IsUnique();
            });

        modelBuilder.Entity<KnowledgeDocument>(
    entity =>
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(512);

        entity.Property(x => x.ContentHash)
            .IsRequired()
            .HasMaxLength(64);

        entity.HasIndex(x => x.ContentHash)
            .IsUnique();

        entity.Property(x => x.CreatedAtUtc)
            .IsRequired();
    });

        modelBuilder.Entity<KnowledgeChunk>(
    entity =>
    {
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Content)
            .IsRequired();

        entity.Property(x => x.ChunkIndex)
            .IsRequired();

        entity.Property(x => x.CreatedAtUtc)
            .IsRequired();

        entity.HasIndex(x => new
        {
            x.DocumentId,
            x.ChunkIndex
        })
        .IsUnique();

        entity.HasOne(x => x.Document)
            .WithMany(x => x.Chunks)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    });
    }
}
