using Agent.Api.Chat;
using Agent.Api.Chat.Models;
using Agent.Api.Configuration;
using Agent.Api.Contracts;
using Agent.Api.Features.Conversation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI.Chat;

namespace Agent.Api.Tests.Chat;

public class ChatOrchestratorTests
{
    private readonly Mock<IConversationService> _conversationService = new();

    private readonly Mock<IAgentRuntime> _agentRuntime = new();

    private readonly Mock<ILogger<ChatOrchestrator>> _logger = new();

    private readonly ChatOrchestrator _sut;

    public ChatOrchestratorTests()
    {
        var options = Options.Create(new AgentOptions
        {
            SystemPrompt = "System Prompt"
        });

        _sut = new ChatOrchestrator(
            _conversationService.Object,
            _agentRuntime.Object,
            _logger.Object);
    }

    [Fact]
    public async Task ChatAsync_Should_Return_Response_When_Request_Is_Valid()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        var request = new Contracts.ChatRequest
        {
            ConversationId = conversationId,
            Message = "Hello"
        };

        var messages = new List<ChatMessage>
    {
        new UserChatMessage("Hello")
    };

        _conversationService
            .Setup(x => x.BuildMessagesAsync(
                conversationId,
                request.Message,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        _agentRuntime
            .Setup(x => x.RunAsync(
                messages,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentRunResult
            {
                AssistantMessage = "Hi!",
                UsedTools = []
            });

        _conversationService
            .Setup(x => x.SaveConversationAsync(
                conversationId,
                request.Message,
                "Hi!",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response =
            await _sut.ChatAsync(request);

        // Assert
        response.ConversationId
            .Should()
            .Be(conversationId);

        response.Answer
            .Should()
            .Be("Hi!");

        response.UsedTools
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task ChatAsync_Should_Throw_When_Request_Is_Null()
    {
        // Act
        Func<Task> act = () => _sut.ChatAsync(null!);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ChatAsync_Should_Throw_When_Message_Is_Empty()
    {
        // Arrange
        var request = new Contracts.ChatRequest
        {
            Message = string.Empty
        };

        // Act
        Func<Task> act = () => _sut.ChatAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Message is required.*");
    }

    [Fact]
    public async Task ChatAsync_Should_Throw_When_Message_Is_Whitespace()
    {
        // Arrange
        var request = new Contracts.ChatRequest
        {
            Message = "      "
        };

        // Act
        Func<Task> act = () => _sut.ChatAsync(request);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Message is required.*");
    }

    [Fact]
    public async Task ChatAsync_Should_Create_New_ConversationId_When_Not_Provided()
    {
        // Arrange
        var request = new Contracts.ChatRequest
        {
            Message = "Hello"
        };

        Guid capturedConversationId = Guid.Empty;

        var messages = new List<ChatMessage>();

        _conversationService
            .Setup(x => x.BuildMessagesAsync(
                It.IsAny<Guid>(),
                request.Message,
                null,
                It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string?, CancellationToken>(
                (id, _, _, _) =>
                {
                    capturedConversationId = id;
                })
            .ReturnsAsync(messages);

        _agentRuntime
            .Setup(x => x.RunAsync(
                messages,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentRunResult
            {
                AssistantMessage = "Hi!",
                UsedTools = []
            });

        _conversationService
            .Setup(x => x.SaveConversationAsync(
                It.IsAny<Guid>(),
                request.Message,
                "Hi!",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response =
            await _sut.ChatAsync(request);

        // Assert
        capturedConversationId
            .Should()
            .NotBe(Guid.Empty);

        response.ConversationId
            .Should()
            .Be(capturedConversationId);

        response.Answer
            .Should()
            .Be("Hi!");

        response.UsedTools
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task ChatAsync_Should_Reuse_Existing_ConversationId()
    {
        // Arrange
        var existingConversationId =
            Guid.NewGuid();

        var request = new Contracts.ChatRequest
        {
            ConversationId =
                existingConversationId,

            Message =
                "Hello"
        };

        var messages =
            new List<ChatMessage>
            {
            new UserChatMessage("Hello")
            };

        _conversationService
            .Setup(x => x.BuildMessagesAsync(
                existingConversationId,
                request.Message,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        _agentRuntime
            .Setup(x => x.RunAsync(
                messages,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new AgentRunResult
                {
                    AssistantMessage = "Hi!",
                    UsedTools = []
                });

        _conversationService
            .Setup(x => x.SaveConversationAsync(
                existingConversationId,
                request.Message,
                "Hi!",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response =
            await _sut.ChatAsync(request);

        // Assert
        response.ConversationId
            .Should()
            .Be(existingConversationId);

        _conversationService.Verify(
            x => x.BuildMessagesAsync(
                existingConversationId,
                request.Message,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _conversationService.Verify(
            x => x.SaveConversationAsync(
                existingConversationId,
                request.Message,
                "Hi!",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}