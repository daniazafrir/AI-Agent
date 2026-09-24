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

public sealed class KnowledgeSearchRetryTests
{
    [Theory]
    [InlineData("ימי חופשה", "{\"success\":true,\"matches\":[]}", true)]
    [InlineData("vacation days", "{\"success\":true,\"matches\":[]}", false)]
    [InlineData("ימי חופשה", "{\"success\":false,\"matches\":[]}", false)]
    [InlineData("ימי חופשה", "{\"success\":true,\"matches\":[{}]}", false)]
    [InlineData("ימי חופשה", "invalid", false)]
    public void OnlyEmptySuccessfulHebrewSearchRequestsRetry(string query, string raw, bool expected)
    {
        var context = Context();
        var arguments = BinaryData.FromObjectAsJson(new { query });
        KnowledgeSearchRetry.Observe(context, arguments, raw);
        Assert.Equal(expected, context.EnglishKnowledgeRetryPending);
        context.EnglishKnowledgeRetryPending = false;
        context.EnglishKnowledgeRetryRequested = true;
        KnowledgeSearchRetry.Observe(context, arguments, raw);
        Assert.False(context.EnglishKnowledgeRetryPending);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingRetryForcesToolChoiceInBothLoops(bool streaming)
    {
        var context = Context();
        context.UsedTools.Add("search_knowledge");
        context.EnglishKnowledgeRetryPending = true;
        var completions = new Mock<IChatCompletionService>();
        ChatCompletionOptions? captured = null;
        completions.Setup(x => x.CompleteAsync(It.IsAny<ICollection<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
            .Callback<ICollection<ChatMessage>, ChatCompletionOptions, CancellationToken>((_, options, _) => captured = options)
            .ReturnsAsync(new ChatCompletionResult { FinishReason = ChatFinishReason.Stop, AssistantMessage = "test" });
        completions.Setup(x => x.CompleteStreamingAsync(It.IsAny<ICollection<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
            .Callback<ICollection<ChatMessage>, ChatCompletionOptions, CancellationToken>((_, options, _) => captured = options)
            .Returns(Stream());
        var registry = new Mock<IMcpToolRegistry>();
        registry.Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var router = new Mock<IToolRouter>();
        router.Setup(x => x.SelectTools(It.IsAny<IReadOnlyList<ChatMessage>>()))
            .Returns(new[] { ChatTool.CreateFunctionTool("search_knowledge") });
        var loop = new AgentLoop(completions.Object, registry.Object, Mock.Of<IToolProcessor>(), Mock.Of<IToolExecutor>(),
            Options.Create(new OpenAiOptions { MaxToolRounds = 3 }), router.Object, NullLogger<AgentLoop>.Instance);
        if (streaming) { await foreach (var _ in loop.RunStreamingAsync(context)) { } }
        else await loop.RunAsync(context, default);
        var snapshot = PromptSnapshot.Capture(1, "test", context.Messages, captured!);
        using var optionsJson = System.Text.Json.JsonDocument.Parse(snapshot.OptionsJson);
        var choice = optionsJson.RootElement.GetProperty("tool_choice");
        Assert.Equal("function", choice.GetProperty("type").GetString());
        Assert.Equal("search_knowledge", choice.GetProperty("function").GetProperty("name").GetString());
        Assert.Contains("equivalent concise English query", snapshot.MessagesJson);
        Assert.True(context.EnglishKnowledgeRetryRequested);
        Assert.False(context.EnglishKnowledgeRetryPending);
    }

    private static AgentContext Context() => new() { ConversationId = Guid.NewGuid(), Messages = [new UserChatMessage("כמה ימי חופשה?")] };
    private static async IAsyncEnumerable<StreamingChatCompletionUpdate> Stream()
    {
        await Task.Yield();
        yield return OpenAIChatModelFactory.StreamingChatCompletionUpdate(finishReason: ChatFinishReason.Stop);
    }
}
