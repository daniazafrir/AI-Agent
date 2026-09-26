using Agent.Api.Chat.Models;
using Agent.Api.Conversations;
using Agent.Api.Features.Conversation;
using Agent.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Agent.Api.Tests.Chat;

public sealed class ConversationTraceTests
{
    [Fact]
    public void MigrationAddsOnlyNullableTraceColumn()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Agent.Api.Infrastructure.Persistence.AgentDbContext>();
        Microsoft.EntityFrameworkCore.NpgsqlDbContextOptionsBuilderExtensions.UseNpgsql(options, "Host=localhost;Database=unused");
        using var db = new Agent.Api.Infrastructure.Persistence.AgentDbContext(options.Options);
        var migrator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>(db);
        var sql = migrator.GenerateScript("20260830114050_AddKnowlageChunk", "20260925090000_AddConversationTrace");
        Assert.Contains("ADD \"TraceJson\" jsonb", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
    }
    [Fact]
    public async Task EachAnswerRetainsItsOwnTraceAndOldMessagesRemainReadable()
    {
        var store = new InMemoryConversationStore();
        var service = new ConversationService(store);
        var id = Guid.NewGuid();
        await service.SaveUserMessageAsync(id, "old question");
        await service.SaveAssistantMessageAsync(id, "old answer");
        await service.SaveAssistantTraceAsync(id, "new answer", new ConversationTrace {
            Prompts = [new PromptSnapshot(1, "model", DateTimeOffset.UtcNow, "[]", "{}")],
            ToolCalls = [new ToolTrace("search_knowledge", "{}", "Source: Policy\n19 days", null)],
            UsedTools = ["search_knowledge"], Sources = [new KnowledgeSource { DocumentId = Guid.NewGuid(), DocumentName = "Policy", ChunkIndex = 0, Score = .03 }],
            TotalMs = 123
        });
        await service.SaveAssistantTraceAsync(id, "stopped", new ConversationTrace { Status = "cancelled", TotalMs = 45 });
        var controller = new ConversationsController(store);
        var response = await controller.GetById(id, default);
        var messages = Assert.IsType<ConversationDetailsResponse>(Assert.IsType<OkObjectResult>(response.Result).Value).Messages;
        Assert.Null(messages[1].Trace);
        Assert.Equal(123, messages[2].Trace!.TotalMs);
        Assert.Single(messages[2].Trace!.Prompts);
        Assert.Contains("19 days", messages[2].Trace!.ToolCalls[0].Result);
        Assert.Single(messages[2].Sources);
        Assert.True(messages[3].Incomplete);
        Assert.Empty(messages[3].Trace!.ToolCalls);
        // Replay metadata must not silently become future conversation input.
        var input = await service.BuildMessagesAsync(id, "next", null);
        Assert.Equal(5, input.Count);
        await store.DeleteConversationAsync(id);
        Assert.Empty(await store.GetMessagesAsync(id));
    }
}
