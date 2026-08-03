using Agent.Api.OpenAI;
using FluentAssertions;
using Moq;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace Agent.Api.Tests.OpenAI;

#pragma warning disable OPENAI001

public sealed class OpenAiChatCompletionServiceTests
{
    private readonly Mock<ChatClient> _chatClient = new();

    private readonly OpenAiChatCompletionService _sut;

    public OpenAiChatCompletionServiceTests()
    {
        _sut = new OpenAiChatCompletionService(
            _chatClient.Object);
    }

    [Fact]
    public async Task CompleteAsync_Should_Map_Stop_Completion()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("Hello")
        };

        var options = new ChatCompletionOptions();

        var sdkCompletion =
            OpenAIChatModelFactory.ChatCompletion(
                id: "completion-1",
                finishReason: ChatFinishReason.Stop,
                content:
                [
                    ChatMessageContentPart.CreateTextPart(
                        "Hello! How can I help?")
                ],
                model: "test-model",
                toolCalls: []);

        SetupCompletion(sdkCompletion);

        // Act
        var result = await _sut.CompleteAsync(
            messages,
            options,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        result.FinishReason.Should()
            .Be(ChatFinishReason.Stop);

        result.AssistantMessage.Should()
            .Be("Hello! How can I help?");

        result.ToolCalls.Should().BeEmpty();

        _chatClient.Verify(x =>
            x.CompleteChatAsync(
                messages,
                options,
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task CompleteAsync_Should_Join_Multiple_Text_Content_Parts()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("Tell me something")
        };

        var options = new ChatCompletionOptions();

        var sdkCompletion =
            OpenAIChatModelFactory.ChatCompletion(
                id: "completion-2",
                finishReason: ChatFinishReason.Stop,
                content:
                [
                    ChatMessageContentPart.CreateTextPart(
                        "First line"),

                    ChatMessageContentPart.CreateTextPart(
                        "Second line")
                ],
                model: "test-model",
                toolCalls: []);

        SetupCompletion(sdkCompletion);

        // Act
        var result = await _sut.CompleteAsync(
            messages,
            options,
            CancellationToken.None);

        // Assert
        result.AssistantMessage.Should().Be(
            string.Join(
                Environment.NewLine,
                "First line",
                "Second line"));

        result.FinishReason.Should()
            .Be(ChatFinishReason.Stop);
    }

    [Fact]
    public async Task CompleteAsync_Should_Return_Empty_Message_When_Content_Is_Empty()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("Hello")
        };

        var options = new ChatCompletionOptions();

        var sdkCompletion =
            OpenAIChatModelFactory.ChatCompletion(
                id: "completion-3",
                finishReason: ChatFinishReason.Stop,
                content: [],
                model: "test-model",
                toolCalls: []);

        SetupCompletion(sdkCompletion);

        // Act
        var result = await _sut.CompleteAsync(
            messages,
            options,
            CancellationToken.None);

        // Assert
        result.AssistantMessage.Should().BeEmpty();
        result.ToolCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteAsync_Should_Map_Function_Tool_Call()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new UserChatMessage(
                "What is the weather in Tel Aviv?")
        };

        var options = new ChatCompletionOptions();

        var functionArguments = BinaryData.FromString(
            """
            {
              "city": "Tel Aviv"
            }
            """);

        var sdkToolCall =
            ChatToolCall.CreateFunctionToolCall(
                id: "tool-call-1",
                functionName: "get_weather",
                functionArguments: functionArguments);

        var sdkCompletion =
            OpenAIChatModelFactory.ChatCompletion(
                id: "completion-4",
                finishReason: ChatFinishReason.ToolCalls,
                content: [],
                model: "test-model",
                toolCalls: [sdkToolCall]);

        SetupCompletion(sdkCompletion);

        // Act
        var result = await _sut.CompleteAsync(
            messages,
            options,
            CancellationToken.None);

        // Assert
        result.FinishReason.Should()
            .Be(ChatFinishReason.ToolCalls);

        result.AssistantMessage.Should().BeEmpty();

        result.ToolCalls.Should().ContainSingle();

        var mappedToolCall = result.ToolCalls.Single();

        mappedToolCall.Id.Should()
            .Be("tool-call-1");

        mappedToolCall.Name.Should()
            .Be("get_weather");

        mappedToolCall.Arguments
            .ToString()
            .Should()
            .Contain("Tel Aviv");
    }

    [Fact]
    public async Task CompleteAsync_Should_Map_Multiple_Tool_Calls()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new UserChatMessage(
                "Calculate and check the current time")
        };

        var options = new ChatCompletionOptions();

        var calculateCall =
            ChatToolCall.CreateFunctionToolCall(
                id: "tool-call-1",
                functionName: "calculate",
                functionArguments: BinaryData.FromString(
                    """
                    {
                      "expression": "2+2"
                    }
                    """));

        var timeCall =
            ChatToolCall.CreateFunctionToolCall(
                id: "tool-call-2",
                functionName: "get_current_time",
                functionArguments: BinaryData.FromString(
                    """
                    {
                      "timeZone": "Asia/Jerusalem"
                    }
                    """));

        var sdkCompletion =
            OpenAIChatModelFactory.ChatCompletion(
                id: "completion-5",
                finishReason: ChatFinishReason.ToolCalls,
                content: [],
                model: "test-model",
                toolCalls:
                [
                    calculateCall,
                    timeCall
                ]);

        SetupCompletion(sdkCompletion);

        // Act
        var result = await _sut.CompleteAsync(
            messages,
            options,
            CancellationToken.None);

        // Assert
        result.ToolCalls.Should().HaveCount(2);

        result.ToolCalls
            .Select(toolCall => toolCall.Name)
            .Should()
            .ContainInOrder(
                "calculate",
                "get_current_time");
    }

    [Fact]
    public async Task CompleteAsync_Should_Pass_CancellationToken_To_ChatClient()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new UserChatMessage("Hello")
        };

        var options = new ChatCompletionOptions();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var sdkCompletion =
            OpenAIChatModelFactory.ChatCompletion(
                id: "completion-6",
                finishReason: ChatFinishReason.Stop,
                content:
                [
                    ChatMessageContentPart.CreateTextPart("Hi")
                ],
                model: "test-model",
                toolCalls: []);

        var clientResult = ClientResult.FromValue(
            sdkCompletion,
            Mock.Of<PipelineResponse>());

        _chatClient
            .Setup(x => x.CompleteChatAsync(
                messages,
                options,
                cancellationToken))
            .ReturnsAsync(clientResult);

        // Act
        await _sut.CompleteAsync(
            messages,
            options,
            cancellationToken);

        // Assert
        _chatClient.Verify(x =>
            x.CompleteChatAsync(
                messages,
                options,
                cancellationToken),
            Times.Once);
    }

    private void SetupCompletion(
        ChatCompletion completion)
    {
        var clientResult = ClientResult.FromValue(
            completion,
            Mock.Of<PipelineResponse>());

        _chatClient
            .Setup(x => x.CompleteChatAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(clientResult);
    }
}

#pragma warning restore OPENAI001