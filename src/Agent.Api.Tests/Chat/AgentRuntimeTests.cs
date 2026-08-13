using Agent.Api.Chat;
using FluentAssertions;
using Moq;
using OpenAI.Chat;

namespace Agent.Api.Tests.Chat;

public sealed class AgentRuntimeTests
{
    private readonly Mock<IAgentLoop> _loop = new();

    private readonly AgentRuntime _sut;

    public AgentRuntimeTests()
    {
        _sut = new AgentRuntime(
            _loop.Object);
    }

    [Fact]
    public async Task RunAsync_Should_Delegate_To_AgentLoop()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("Hello")
        };

        var expected = new AgentRunResult
        {
            AssistantMessage = "Hello!",
            UsedTools = []
        };

        _loop
            .Setup(x => x.RunAsync(
                It.IsAny<AgentContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result =
            await _sut.RunAsync(
                messages,
                CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);

        _loop.Verify(
            x => x.RunAsync(
                It.Is<AgentContext>(
                    context =>
                        context.Messages.Count == 1 &&
                        context.Messages[0] ==
                            messages[0]),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_Should_Create_Context_With_Messages()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("First"),
            new AssistantChatMessage("Second")
        };

        AgentContext? capturedContext = null;

        _loop
            .Setup(x => x.RunAsync(
                It.IsAny<AgentContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<
                AgentContext,
                CancellationToken>(
                (context, _) =>
                {
                    capturedContext = context;
                })
            .ReturnsAsync(
                new AgentRunResult
                {
                    AssistantMessage = "Done",
                    UsedTools = []
                });

        // Act
        await _sut.RunAsync(messages, CancellationToken.None);

        // Assert
        capturedContext.Should().NotBeNull();

        capturedContext!
            .Messages
            .Should()
            .HaveCount(2);

        capturedContext.Messages[0]
            .Should()
            .BeSameAs(messages[0]);

        capturedContext.Messages[1]
            .Should()
            .BeSameAs(messages[1]);

        capturedContext.UsedTools
            .Should()
            .BeEmpty();

        capturedContext.Round
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task RunAsync_Should_Use_Provided_CancellationToken()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("Hello")
        };

        var cancellationToken =
            new CancellationToken();

        _loop
            .Setup(x => x.RunAsync(
                It.IsAny<AgentContext>(),
                cancellationToken))
            .ReturnsAsync(
                new AgentRunResult
                {
                    AssistantMessage = "Done",
                    UsedTools = []
                });

        // Act
        await _sut.RunAsync(
            messages,
            cancellationToken);

        // Assert
        _loop.Verify(
            x => x.RunAsync(
                It.IsAny<AgentContext>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task RunStreamingAsync_Should_Return_Content_And_Completed_Events()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("Hello")
        };

        _loop
            .Setup(x => x.RunAsync(
                It.IsAny<AgentContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new AgentRunResult
                {
                    AssistantMessage = "Hello!",
                    UsedTools =
                    [
                        "calculate"
                    ]
                });

        // Act
        var events =
            new List<ChatStreamEvent>();

        await foreach (
            var streamEvent in
                _sut.RunStreamingAsync(
                    messages,
                    CancellationToken.None))
        {
            events.Add(streamEvent);
        }

        // Assert
        events.Should().HaveCount(2);

        events[0].Type.Should()
            .Be("content");

        events[0].Content.Should()
            .Be("Hello!");

        events[1].Type.Should()
            .Be("completed");

        events[1].UsedTools.Should()
            .BeEquivalentTo(
                ["calculate"]);
    }
}