using Agent.Api.Conversations;
using Agent.Api.Features.Conversation;
using FluentAssertions;
using Moq;
using OpenAI.Chat;
using StoredConversationMessage =
    Agent.Api.Entitites.ConversationMessage;

namespace Agent.Api.Tests.Conversation;

public sealed class ConversationServiceTests
{
    private readonly Mock<IConversationStore> _conversationStore = new();

    private readonly ConversationService _sut;

    public ConversationServiceTests()
    {
        _sut = new ConversationService(
            _conversationStore.Object);
    }

    [Fact]
    public async Task BuildMessagesAsync_Should_Add_System_Message_When_System_Prompt_Is_Provided()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        const string currentUserMessage = "Hello";
        const string systemPrompt = "You are a helpful assistant.";

        SetupEmptyHistory(conversationId);

        // Act
        var result = await _sut.BuildMessagesAsync(
            conversationId,
            currentUserMessage,
            systemPrompt);

        // Assert
        result.Should().HaveCount(2);

        result[0].Should().BeOfType<SystemChatMessage>();
        result[1].Should().BeOfType<UserChatMessage>();

        GetMessageText(result[0]).Should().Be(systemPrompt);
        GetMessageText(result[1]).Should().Be(currentUserMessage);
    }

    [Fact]
    public async Task BuildMessagesAsync_Should_Not_Add_System_Message_When_System_Prompt_Is_Empty()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        const string currentUserMessage = "Hello";

        SetupEmptyHistory(conversationId);

        // Act
        var result = await _sut.BuildMessagesAsync(
            conversationId,
            currentUserMessage,
            string.Empty);

        // Assert
        result.Should().ContainSingle();
        result[0].Should().BeOfType<UserChatMessage>();

        GetMessageText(result[0]).Should().Be(currentUserMessage);
    }

    [Fact]
    public async Task BuildMessagesAsync_Should_Not_Add_System_Message_When_System_Prompt_Is_Null()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        const string currentUserMessage = "Hello";

        SetupEmptyHistory(conversationId);

        // Act
        var result = await _sut.BuildMessagesAsync(
            conversationId,
            currentUserMessage,
            null);

        // Assert
        result.Should().ContainSingle();
        result[0].Should().BeOfType<UserChatMessage>();

        GetMessageText(result[0]).Should().Be(currentUserMessage);
    }

    [Fact]
    public async Task BuildMessagesAsync_Should_Add_Stored_User_Message()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        IReadOnlyList<StoredConversationMessage> history =
        [
            CreateStoredMessage(
                "user",
                "Previous user message")
        ];

        SetupHistory(conversationId, history);

        // Act
        var result = await _sut.BuildMessagesAsync(
            conversationId,
            "Current message",
            null);

        // Assert
        result.Should().HaveCount(2);

        result[0].Should().BeOfType<UserChatMessage>();
        GetMessageText(result[0]).Should()
            .Be("Previous user message");

        result[1].Should().BeOfType<UserChatMessage>();
        GetMessageText(result[1]).Should()
            .Be("Current message");
    }

    [Fact]
    public async Task BuildMessagesAsync_Should_Add_Stored_Assistant_Message()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        IReadOnlyList<StoredConversationMessage> history =
        [
            CreateStoredMessage(
                "assistant",
                "Previous assistant message")
        ];

        SetupHistory(conversationId, history);

        // Act
        var result = await _sut.BuildMessagesAsync(
            conversationId,
            "Current message",
            null);

        // Assert
        result.Should().HaveCount(2);

        result[0].Should().BeOfType<AssistantChatMessage>();
        GetMessageText(result[0]).Should()
            .Be("Previous assistant message");

        result[1].Should().BeOfType<UserChatMessage>();
        GetMessageText(result[1]).Should()
            .Be("Current message");
    }

    [Fact]
    public async Task BuildMessagesAsync_Should_Preserve_History_Order()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        IReadOnlyList<StoredConversationMessage> history =
        [
            CreateStoredMessage(
                "user",
                "First question"),

            CreateStoredMessage(
                "assistant",
                "First answer"),

            CreateStoredMessage(
                "user",
                "Second question"),

            CreateStoredMessage(
                "assistant",
                "Second answer")
        ];

        SetupHistory(conversationId, history);

        // Act
        var result = await _sut.BuildMessagesAsync(
            conversationId,
            "Current question",
            "System prompt");

        // Assert
        result.Should().HaveCount(6);

        result[0].Should().BeOfType<SystemChatMessage>();
        result[1].Should().BeOfType<UserChatMessage>();
        result[2].Should().BeOfType<AssistantChatMessage>();
        result[3].Should().BeOfType<UserChatMessage>();
        result[4].Should().BeOfType<AssistantChatMessage>();
        result[5].Should().BeOfType<UserChatMessage>();

        GetMessageText(result[0]).Should().Be("System prompt");
        GetMessageText(result[1]).Should().Be("First question");
        GetMessageText(result[2]).Should().Be("First answer");
        GetMessageText(result[3]).Should().Be("Second question");
        GetMessageText(result[4]).Should().Be("Second answer");
        GetMessageText(result[5]).Should().Be("Current question");
    }

    [Fact]
    public async Task BuildMessagesAsync_Should_Trim_Current_User_Message()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        SetupEmptyHistory(conversationId);

        // Act
        var result = await _sut.BuildMessagesAsync(
            conversationId,
            "   Hello world   ",
            null);

        // Assert
        result.Should().ContainSingle();

        GetMessageText(result[0]).Should()
            .Be("Hello world");
    }

    [Fact]
    public async Task BuildMessagesAsync_Should_Ignore_Unsupported_Message_Roles()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        IReadOnlyList<StoredConversationMessage> history =
        [
            CreateStoredMessage(
                "system",
                "Stored system message"),

            CreateStoredMessage(
                "unknown",
                "Unknown message"),

            CreateStoredMessage(
                "user",
                "Valid user message")
        ];

        SetupHistory(conversationId, history);

        // Act
        var result = await _sut.BuildMessagesAsync(
            conversationId,
            "Current message",
            null);

        // Assert
        result.Should().HaveCount(2);

        result[0].Should().BeOfType<UserChatMessage>();
        GetMessageText(result[0]).Should()
            .Be("Valid user message");

        result[1].Should().BeOfType<UserChatMessage>();
        GetMessageText(result[1]).Should()
            .Be("Current message");
    }

    [Fact]
    public async Task BuildMessagesAsync_Should_Request_History_For_Correct_Conversation()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();

        _conversationStore
            .Setup(x => x.GetMessagesAsync(
                conversationId,
                cancellationToken))
            .Returns(
                EmptyMessagesAsync());

        // Act
        await _sut.BuildMessagesAsync(
            conversationId,
            "Hello",
            null,
            cancellationToken);

        // Assert
        _conversationStore.Verify(x =>
            x.GetMessagesAsync(
                conversationId,
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task SaveConversationAsync_Should_Save_User_And_Assistant_Messages()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        const string userMessage = "Hello";
        const string assistantMessage = "Hi!";

        _conversationStore
            .Setup(x => x.AddMessageAsync(
                conversationId,
                "user",
                userMessage,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _conversationStore
            .Setup(x => x.AddMessageAsync(
                conversationId,
                "assistant",
                assistantMessage,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SaveConversationAsync(
            conversationId,
            userMessage,
            assistantMessage);

        // Assert
        _conversationStore.Verify(x =>
            x.AddMessageAsync(
                conversationId,
                "user",
                userMessage,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _conversationStore.Verify(x =>
            x.AddMessageAsync(
                conversationId,
                "assistant",
                assistantMessage,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveConversationAsync_Should_Preserve_Message_Order()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var calls = new List<string>();

        _conversationStore
            .Setup(x => x.AddMessageAsync(
                conversationId,
                "user",
                "Question",
                It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("user"))
            .Returns(Task.CompletedTask);

        _conversationStore
            .Setup(x => x.AddMessageAsync(
                conversationId,
                "assistant",
                "Answer",
                It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("assistant"))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SaveConversationAsync(
            conversationId,
            "Question",
            "Answer");

        // Assert
        calls.Should().ContainInOrder(
            "user",
            "assistant");
    }

    [Fact]
    public async Task SaveConversationAsync_Should_Use_Provided_CancellationToken()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();

        _conversationStore
            .Setup(x => x.AddMessageAsync(
                conversationId,
                "user",
                "Question",
                cancellationToken))
            .Returns(Task.CompletedTask);

        _conversationStore
            .Setup(x => x.AddMessageAsync(
                conversationId,
                "assistant",
                "Answer",
                cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SaveConversationAsync(
            conversationId,
            "Question",
            "Answer",
            cancellationToken);

        // Assert
        _conversationStore.Verify(x =>
            x.AddMessageAsync(
                conversationId,
                "user",
                "Question",
                cancellationToken),
            Times.Once);

        _conversationStore.Verify(x =>
            x.AddMessageAsync(
                conversationId,
                "assistant",
                "Answer",
                cancellationToken),
            Times.Once);
    }

    private void SetupEmptyHistory(
        Guid conversationId)
    {
        _conversationStore
            .Setup(x => x.GetMessagesAsync(
                conversationId,
                It.IsAny<CancellationToken>()))
            .Returns(
                EmptyMessagesAsync());
    }

    private void SetupHistory(
        Guid conversationId,
        IReadOnlyList<StoredConversationMessage> history)
    {
        _conversationStore
            .Setup(x => x.GetMessagesAsync(
                conversationId,
                It.IsAny<CancellationToken>()))
            .Returns(
                Task.FromResult(history));
    }

    private static Task<IReadOnlyList<StoredConversationMessage>>
        EmptyMessagesAsync()
    {
        return Task.FromResult<IReadOnlyList<StoredConversationMessage>>(
            Array.Empty<StoredConversationMessage>());
    }

    private static string GetMessageText(
        ChatMessage message)
    {
        return string.Join(
            Environment.NewLine,
            message.Content
                .Select(part => part.Text)
                .Where(text =>
                    !string.IsNullOrWhiteSpace(text)));
    }

    private static StoredConversationMessage CreateStoredMessage(
    string role,
    string content)
    {
        return new StoredConversationMessage
        {
            Role = role,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow
            
        };
    }
}