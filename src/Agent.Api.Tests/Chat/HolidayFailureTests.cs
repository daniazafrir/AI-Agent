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

public class HolidayFailureTests
{
    [Theory]
    [InlineData("{\"success\":false}")]
    [InlineData("{\"isError\":true}")]
    [InlineData("unavailable")]
    [InlineData("{}")]
    public async Task StreamingFailure_StopsBeforeModelCanInventDate(string payload)
    {
        var completion = new Mock<IChatCompletionService>();
        completion.Setup(x => x.CompleteStreamingAsync(It.IsAny<ICollection<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Call());
        var executor = new Mock<IToolExecutor>();
        executor.Setup(x => x.ExecuteAsync("get_jewish_holidays", It.IsAny<BinaryData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolExecutionResult { RawContent = payload, Content = payload });
        var registry = new Mock<IMcpToolRegistry>();
        registry.Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var router = new Mock<IToolRouter>();
        router.Setup(x => x.SelectTools(It.IsAny<IReadOnlyList<ChatMessage>>())).Returns(new[] { ChatTool.CreateFunctionTool("get_jewish_holidays") });
        var loop = new AgentLoop(completion.Object, registry.Object, Mock.Of<IToolProcessor>(), executor.Object,
            Options.Create(new OpenAiOptions { MaxToolRounds = 1 }), router.Object, NullLogger<AgentLoop>.Instance);
        var context = Context();
        var events = new List<ChatStreamEvent>();
        await foreach (var e in loop.RunStreamingAsync(context)) events.Add(e);
        Assert.Contains("לא ניתן לאמת", Assert.Single(events, e => e.Type == "content").Content);
        Assert.Single(events, e => e.Type == "completed");
        Assert.Single(events, e => e.Type == "tool-completed");
        completion.Verify(x => x.CompleteStreamingAsync(It.IsAny<ICollection<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NonStreamingFailure_ReturnsWithoutAnotherCompletion()
    {
        var completion = new Mock<IChatCompletionService>();
        completion.Setup(x => x.CompleteAsync(It.IsAny<ICollection<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatCompletionResult { FinishReason = ChatFinishReason.ToolCalls,
                ToolCalls = new[] { new ToolCallResult { Id = "holiday1", Name = "get_jewish_holidays" } } });
        var processor = new Mock<IToolProcessor>();
        processor.Setup(x => x.ProcessAsync(It.IsAny<ChatCompletionResult>(), It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
            .Callback<ChatCompletionResult, AgentContext, CancellationToken>((_, context, _) =>
                HolidayFailure.Observe(context, "get_jewish_holidays", "{\"success\":false}"))
            .Returns(Task.CompletedTask);
        var registry = new Mock<IMcpToolRegistry>();
        registry.Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var router = new Mock<IToolRouter>();
        router.Setup(x => x.SelectTools(It.IsAny<IReadOnlyList<ChatMessage>>())).Returns(Array.Empty<ChatTool>());
        var loop = new AgentLoop(completion.Object, registry.Object, processor.Object, Mock.Of<IToolExecutor>(),
            Options.Create(new OpenAiOptions { MaxToolRounds = 1 }), router.Object, NullLogger<AgentLoop>.Instance);
        var result = await loop.RunAsync(Context(), default);
        Assert.Contains("לא ניתן לאמת", result.AssistantMessage);
        Assert.DoesNotContain("2027", result.AssistantMessage);
        completion.Verify(x => x.CompleteAsync(It.IsAny<ICollection<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void SuccessfulHoliday_AndUnrelatedFailure_DoNotBlock()
    {
        var context = Context();
        HolidayFailure.Observe(context, "get_weather", "{\"success\":false}");
        HolidayFailure.Observe(context, "get_jewish_holidays", "{\"success\":true}");
        Assert.Null(context.HolidayFailureMessage);
    }

    private static AgentContext Context() => new()
    {
        ConversationId = Guid.NewGuid(), Messages = new()
        {
            new AssistantChatMessage("Previously: 22 October 2027"),
            new UserChatMessage("מתי ערב שמחת תורה?")
        }
    };

    private static async IAsyncEnumerable<StreamingChatCompletionUpdate> Call()
    {
        await Task.Yield();
        yield return OpenAIChatModelFactory.StreamingChatCompletionUpdate(
            toolCallUpdates: new[] { OpenAIChatModelFactory.StreamingChatToolCallUpdate(index: 0,
                toolCallId: "holiday1", functionName: "get_jewish_holidays", functionArgumentsUpdate: BinaryData.FromString("{}")) },
            finishReason: ChatFinishReason.ToolCalls);
    }
}
