using Agent.Api.Chat;
using Agent.Api.Chat.Models;
using Agent.Api.Configuration;
using Agent.Api.Mcp;
using Agent.Api.OpenAI;
using Agent.Api.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI.Chat;
using Xunit;

namespace Agent.Api.Tests.Chat;

public sealed class ToolCallFinishReasonTests
{
    [Theory]
    [InlineData(ChatFinishReason.Stop, false, false)]
    [InlineData(ChatFinishReason.ToolCalls, false, false)]
    [InlineData(ChatFinishReason.Stop, true, false)]
    [InlineData(ChatFinishReason.ToolCalls, true, false)]
    [InlineData(ChatFinishReason.Stop, false, true)]
    [InlineData(ChatFinishReason.ToolCalls, false, true)]
    [InlineData(ChatFinishReason.Stop, true, true)]
    [InlineData(ChatFinishReason.ToolCalls, true, true)]
    public async Task ReturnedCalls_AreExecutedBeforeAnswer(ChatFinishReason reason, bool streaming, bool capture)
    {
        var completions = new Mock<IChatCompletionService>();
        var processor = new Mock<IToolProcessor>();
        processor.Setup(x => x.ProcessAsync(It.IsAny<ChatCompletionResult>(), It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        completions.SetupSequence(x => x.CompleteAsync(It.IsAny<ICollection<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatCompletionResult { FinishReason = reason,
                ToolCalls = new[] { new ToolCallResult { Id = "call1", Name = "search_knowledge" } } })
            .ReturnsAsync(new ChatCompletionResult { FinishReason = ChatFinishReason.Stop, AssistantMessage = "14 days" });
        completions.SetupSequence(x => x.CompleteStreamingAsync(It.IsAny<ICollection<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Stream(OpenAIChatModelFactory.StreamingChatCompletionUpdate(
                toolCallUpdates: new[] { OpenAIChatModelFactory.StreamingChatToolCallUpdate(
                    index: 0, toolCallId: "call1", functionName: "search_knowledge",
                    functionArgumentsUpdate: BinaryData.FromString("{}")) }, finishReason: reason)))
            .Returns(Stream(OpenAIChatModelFactory.StreamingChatCompletionUpdate(
                contentUpdate: new ChatMessageContent("14 days"), finishReason: ChatFinishReason.Stop)));
        var executor = new Mock<IToolExecutor>();
        executor.Setup(x => x.ExecuteAsync("search_knowledge", It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolExecutionResult {
                RawContent = capture
                    ? """{"embeddingTimeMs":120,"vectorSearchTimeMs":20,"keywordSearchTimeMs":30,"rankingTimeMs":0,"matches":[{"DocumentId":"00000000-0000-0000-0000-000000000001","DocumentName":"Handbook","ChunkIndex":3,"Score":0.032,"SearchEngine":2}]}"""
                    : """{"embeddingTimeMs":120,"vectorSearchTimeMs":20,"keywordSearchTimeMs":30,"rankingTimeMs":0,"matches":[{"documentId":"00000000-0000-0000-0000-000000000001","documentName":"Handbook","chunkIndex":3,"score":0.032,"searchEngine":2}]}""",
                Content = "Source: Handbook\nEmployees get 19 vacation days."
            });
        var registry = new Mock<IMcpToolRegistry>();
        registry.Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var router = new Mock<IToolRouter>();
        router.Setup(x => x.SelectTools(It.IsAny<IReadOnlyList<ChatMessage>>())).Returns(new[] { ChatTool.CreateFunctionTool("search_knowledge") });
        var loop = new AgentLoop(completions.Object, registry.Object, processor.Object, executor.Object,
            Options.Create(new OpenAiOptions { MaxToolRounds = 3, EnablePromptViewer = capture, Model = "test-model" }), router.Object, NullLogger<AgentLoop>.Instance);
        var context = new AgentContext { ConversationId = Guid.NewGuid(), Messages = new() { new UserChatMessage("When must I submit a claim?") } };
        if (streaming)
        {
            var events = new List<ChatStreamEvent>();
            await foreach (var e in loop.RunStreamingAsync(context, default)) events.Add(e);
            executor.Verify(x => x.ExecuteAsync("search_knowledge", It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Contains(events, e => e.Type == "content" && e.Content == "14 days");
            var snapshots = events.Where(e => e.Type == "prompt").Select(e => e.Prompt!).ToArray();
            Assert.Equal(capture ? 2 : 0, snapshots.Length);
            if (capture)
            {
                Assert.Equal(new[] { 1, 2 }, snapshots.Select(x => x.Round));
                Assert.DoesNotContain("tool_call_id", snapshots[0].MessagesJson);
                Assert.Contains("tool_call_id", snapshots[1].MessagesJson);
                Assert.Contains("Employees get 19 vacation days.", snapshots[1].MessagesJson);
                Assert.True(events.FindIndex(x => x.Type == "prompt") < events.FindIndex(x => x.Type == "tool-started"));
            }
            Assert.Contains("search_knowledge", Assert.Single(events.Where(e => e.Type == "completed")).UsedTools!);
            var match = Assert.Single(events.Single(e => e.Type == "completed").Debug!.Matches);
            Assert.Equal("Handbook", match.DocumentName);
            Assert.Equal(3, match.ChunkIndex);
            Assert.Equal(0.032, match.Score);
            Assert.Equal("Hybrid", match.SearchEngine);
            var debug = events.Single(e => e.Type == "completed").Debug!;
            Assert.Equal(120L, debug.EmbeddingTimeMs);
            Assert.Equal(20L, debug.VectorSearchTimeMs);
            Assert.Equal(30L, debug.KeywordSearchTimeMs);
            Assert.Equal(0L, debug.RankingTimeMs);
        }
        else
        {
            var result = await loop.RunAsync(context, default);
            Assert.Equal("14 days", result.AssistantMessage);
            Assert.Equal(capture ? 2 : 0, result.Prompts.Count);
            processor.Verify(x => x.ProcessAsync(It.IsAny<ChatCompletionResult>(), It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
    private static async IAsyncEnumerable<StreamingChatCompletionUpdate> Stream(StreamingChatCompletionUpdate item)
    {
        await Task.Yield();
        yield return item;
    }
}
