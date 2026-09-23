using Agent.Api.Chat;
using Agent.Api.Chat.Models;
using Agent.Api.Features.Conversation;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAI.Chat;

namespace Agent.Api.Tests.Chat;

public class ChatCancellationHistoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stop_SavesQuestionAndIncompleteAnswer_WithIndependentToken(bool receiveText)
    {
        var service = new Mock<IConversationService>();
        service.Setup(x => x.BuildMessagesAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ChatMessage>());
        var runtime = new Mock<IAgentRuntime>();
        runtime.Setup(x => x.RunStreamingAsync(It.IsAny<List<ChatMessage>>(),
            It.IsAny<CancellationToken>())).Returns(Events());
        var sut = new ChatOrchestrator(service.Object, runtime.Object,
            Mock.Of<ILogger<ChatOrchestrator>>());
        using var cancellation = new CancellationTokenSource();
        var id = Guid.NewGuid();
        var iterator = sut.ChatStreamingAsync(new() { ConversationId = id, Message = "question" },
            cancellation.Token).GetAsyncEnumerator();

        Assert.True(await iterator.MoveNextAsync());
        Assert.Equal("conversation", iterator.Current.Type);
        service.Verify(x => x.SaveUserMessageAsync(id, "question", It.IsAny<CancellationToken>()), Times.Once);
        if (receiveText) Assert.True(await iterator.MoveNextAsync());
        cancellation.Cancel();
        await iterator.DisposeAsync();

        service.Verify(x => x.SaveAssistantMessageAsync(id,
            receiveText ? "partial\n\n[התשובה לא הושלמה]" : "[התשובה לא הושלמה]",
            It.Is<CancellationToken>(token => !token.IsCancellationRequested && token != cancellation.Token)), Times.Once);
        service.Verify(x => x.SaveConversationAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompletedStream_SavesEachMessageOnce_WithoutIncompleteMarker()
    {
        var service = new Mock<IConversationService>();
        service.Setup(x => x.BuildMessagesAsync(It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ChatMessage>());
        var runtime = new Mock<IAgentRuntime>();
        runtime.Setup(x => x.RunStreamingAsync(It.IsAny<List<ChatMessage>>(),
            It.IsAny<CancellationToken>())).Returns(Events());
        var sut = new ChatOrchestrator(service.Object, runtime.Object,
            Mock.Of<ILogger<ChatOrchestrator>>());
        var id = Guid.NewGuid();
        await foreach (var item in sut.ChatStreamingAsync(new() { ConversationId = id, Message = "question" })) { }
        service.Verify(x => x.SaveUserMessageAsync(id, "question", It.IsAny<CancellationToken>()), Times.Once);
        service.Verify(x => x.SaveAssistantMessageAsync(id, "partial", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async IAsyncEnumerable<ChatStreamEvent> Events()
    {
        await Task.Yield();
        yield return new() { Type = "content", Content = "partial" };
        yield return new() { Type = "completed" };
    }
}
