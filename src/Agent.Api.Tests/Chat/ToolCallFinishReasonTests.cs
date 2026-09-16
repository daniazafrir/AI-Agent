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
    [InlineData(ChatFinishReason.Stop, false)]
    [InlineData(ChatFinishReason.ToolCalls, false)]
    [InlineData(ChatFinishReason.Stop, true)]
    [InlineData(ChatFinishReason.ToolCalls, true)]
    public async Task ReturnedCalls_AreExecutedBeforeAnswer(ChatFinishReason reason, bool streaming)
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
            .ReturnsAsync(new ToolExecutionResult { RawContent = "{\"matches\":[]}", Content = "{\"matches\":[]}" });
        var registry = new Mock<IMcpToolRegistry>();
        registry.Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var router = new Mock<IToolRouter>();
        router.Setup(x => x.SelectTools(It.IsAny<IReadOnlyList<ChatMessage>>())).Returns(new[] { ChatTool.CreateFunctionTool("search_knowledge") });
        var loop = new AgentLoop(completions.Object, registry.Object, processor.Object, executor.Object,
            Options.Create(new OpenAiOptions { MaxToolRounds = 3 }), router.Object, NullLogger<AgentLoop>.Instance);
        var context = new AgentContext { ConversationId = Guid.NewGuid(), Messages = new() { new UserChatMessage("When must I submit a claim?") } };
        if (streaming)
        {
            var events = new List<ChatStreamEvent>();
            await foreach (var e in loop.RunStreamingAsync(context, default)) events.Add(e);
            executor.Verify(x => x.ExecuteAsync("search_knowledge", It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Contains(events, e => e.Type == "content" && e.Content == "14 days");
            Assert.Contains("search_knowledge", Assert.Single(events.Where(e => e.Type == "completed")).UsedTools!);
        }
        else
        {
            Assert.Equal("14 days", (await loop.RunAsync(context, default)).AssistantMessage);
            processor.Verify(x => x.ProcessAsync(It.IsAny<ChatCompletionResult>(), It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
    private static async IAsyncEnumerable<StreamingChatCompletionUpdate> Stream(StreamingChatCompletionUpdate item)
    {
        await Task.Yield();
        yield return item;
    }
}
