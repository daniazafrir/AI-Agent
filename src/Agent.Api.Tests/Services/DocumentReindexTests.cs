using Agent.Api.Features.Knowledge;
using Agent.Api.Features.Knowledge.Chunking;
using Agent.Api.Features.Knowledge.Extraction;
using Agent.Api.Mcp;
using Agent.Knowledge.Entitites;
using Agent.Knowledge.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using Xunit;

namespace Agent.Api.Tests.Services;

public sealed class DocumentReindexTests
{
    [Fact]
    public void MigrationProducesNullableMetadataColumn()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Agent.Api.Infrastructure.Persistence.AgentDbContext>();
        Microsoft.EntityFrameworkCore.NpgsqlDbContextOptionsBuilderExtensions.UseNpgsql(options, "Host=localhost;Database=unused");
        using var db = new Agent.Api.Infrastructure.Persistence.AgentDbContext(options.Options);
        var migrator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>(db);
        var sql = migrator.GenerateScript("20260925090000_AddConversationTrace", "20260926090000_AddDocumentIndexingDetails");
        Assert.Contains("ADD \"IndexingDetailsJson\" text", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReindexPreservesChunksAndRecordsMetadataOnlyAfterSuccess(bool success)
    {
        var id = Guid.NewGuid();
        var documents = new Mock<IKnowledgeDocumentRepository>();
        documents.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeDocument { Id = id, FileName = "policy.txt" });
        var chunks = new Mock<IKnowledgeChunkRepository>();
        chunks.Setup(x => x.ListByDocumentAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new KnowledgeChunk { ChunkIndex = 1, Content = "second" }, new KnowledgeChunk { ChunkIndex = 0, Content = "first" } });
        var mcp = new Mock<IMcpToolClient>();
        mcp.Setup(x => x.CallToolAsync("delete_vectors", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>())).ReturnsAsync("true");
        mcp.Setup(x => x.CallToolAsync("index_document", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyDictionary<string, object?>, CancellationToken>((_, args, _) => Assert.Equal(new[] { "first", "second" }, (IEnumerable<string>)args["chunks"]!))
            .ReturnsAsync(JsonSerializer.Serialize(new { success, chunkCount = 2, embeddingModel = "test-model", embeddingDimensions = 42 }));
        var sut = new KnowledgeDocumentService(mcp.Object, documents.Object, chunks.Object,
            new DocumentTextExtractorFactory([]), Mock.Of<IChunkingService>(), NullLogger<KnowledgeDocumentService>.Instance);
        if (success)
        {
            Assert.True(await sut.ReindexAsync(id));
            documents.Verify(x => x.UpdateIndexingDetailsAsync(id,
                It.Is<string>(json => json.Contains("test-model") && json.Contains("persisted-chunks") && json.Contains("\"ChunkSize\":null")), It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ReindexAsync(id));
            documents.Verify(x => x.UpdateIndexingDetailsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        chunks.Verify(x => x.DeleteByDocumentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
