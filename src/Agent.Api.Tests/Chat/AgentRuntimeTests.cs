using Agent.Api.Chat;
using Agent.Api.Configuration;
using Agent.Api.Mcp;
using Agent.Api.OpenAI;
using Agent.Api.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace Agent.Api.Tests.Chat;

public class AgentRuntimeTests
{
    private readonly Mock<IChatCompletionService> _chatCompletionService = new();

    private readonly Mock<IMcpToolRegistry> _toolRegistry = new();

    private readonly Mock<IToolExecutor> _toolExecutor = new();

    private readonly Mock<ILogger<AgentRuntime>> _logger = new();

    private readonly AgentRuntime _sut;

    public AgentRuntimeTests()
    {
        var options = Options.Create(new OpenAiOptions
        {
            MaxToolRounds = 3
        });

        _toolRegistry
            .Setup(x => x.InitializeAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _toolRegistry
            .SetupGet(x => x.Definitions)
            .Returns([]);

        _sut = new AgentRuntime(
            _chatCompletionService.Object,
            _toolRegistry.Object,
            _toolExecutor.Object,
            options,
            _logger.Object);
    }

   [Fact]
public async Task RunAsync_Should_Return_Assistant_Message_When_OpenAI_Returns_Stop()
{
    // Arrange
    var messages = new List<ChatMessage>
    {
        new UserChatMessage("Hello")
    };

    var completion = new ChatCompletionResult
    {
        FinishReason = ChatFinishReason.Stop,
        AssistantMessage = "Hello! How can I help?",
        ToolCalls = Array.Empty<ToolCallResult>()
    };

    _chatCompletionService
        .Setup(x => x.CompleteAsync(
            It.IsAny<ICollection<ChatMessage>>(),
            It.IsAny<ChatCompletionOptions>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(completion);

    // Act
    AgentRunResult result = await _sut.RunAsync(
        messages,
        CancellationToken.None);

    // Assert
    result.AssistantMessage.Should()
        .Be("Hello! How can I help?");

    result.UsedTools.Should().BeEmpty();

    _toolExecutor.Verify(
        x => x.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<BinaryData>(),
            It.IsAny<CancellationToken>()),
        Times.Never);

    _chatCompletionService.Verify(
        x => x.CompleteAsync(
            It.IsAny<ICollection<ChatMessage>>(),
            It.IsAny<ChatCompletionOptions>(),
            It.IsAny<CancellationToken>()),
        Times.Once);
}
    [Fact]
    public async Task RunAsync_Should_Execute_Tool_When_OpenAI_Returns_ToolCalls()
    {
        // Arrange
        var messages = new List<ChatMessage>
    {
        new UserChatMessage("What is the weather?")
    };

        var functionArguments = BinaryData.FromString(
            """
        {
          "city": "Tel Aviv"
        }
        """);

#pragma warning disable OPENAI001

        var toolCall = ChatToolCall.CreateFunctionToolCall(
            id: "tool-call-1",
            functionName: "get_weather",
            functionArguments: BinaryData.FromString(
                """
        {
          "city": "Tel Aviv"
        }
        """));

        var toolCallCompletion =
            OpenAIChatModelFactory.ChatCompletion(
                id: "completion-1",
                finishReason: ChatFinishReason.ToolCalls,
                content: [],
                model: "test-model",
                toolCalls: [toolCall]);

#pragma warning restore OPENAI001

        _chatCompletionService
    .SetupSequence(x => x.CompleteAsync(
        It.IsAny<ICollection<ChatMessage>>(),
        It.IsAny<ChatCompletionOptions>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(new ChatCompletionResult
    {
        FinishReason = ChatFinishReason.ToolCalls,
        ToolCalls =
        [
            new ToolCallResult
            {
                Id = "1",
                Name = "get_weather",
                Arguments = BinaryData.FromString("""{"city":"Tel Aviv"}""")
            }
        ]
    })
    .ReturnsAsync(new ChatCompletionResult
    {
        FinishReason = ChatFinishReason.Stop,
        AssistantMessage = "Sunny"
    });

        _toolExecutor
            .Setup(x => x.ExecuteAsync(
                "get_weather",
                It.Is<BinaryData>(arguments =>
                    arguments.ToString().Contains("Tel Aviv")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                """
            {
              "temperature": 27,
              "condition": "Sunny"
            }
            """);

        // Act
        var result = await _sut.RunAsync(messages);

        // Assert
        result.AssistantMessage.Should().Be("Sunny");

        result.UsedTools.Should()
            .BeEquivalentTo(["get_weather"]);

        _chatCompletionService.Verify(
            x => x.CompleteAsync(
                It.IsAny<ICollection<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _toolExecutor.Verify(
            x => x.ExecuteAsync(
                "get_weather",
                It.Is<BinaryData>(arguments =>
                    arguments.ToString().Contains("Tel Aviv")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        messages.Should().ContainSingle(
            message => message is ToolChatMessage);

        messages.Should().NotContain(
            message => message is AssistantChatMessage);
    }

    [Fact]
    public async Task RunAsync_Should_Return_Default_Message_When_Assistant_Message_Is_Empty()
    {
        // Arrange
        var messages = new List<ChatMessage>
    {
        new UserChatMessage("Hello")
    };

        _chatCompletionService
            .Setup(x => x.CompleteAsync(
                It.IsAny<ICollection<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatCompletionResult
            {
                FinishReason = ChatFinishReason.Stop,
                AssistantMessage = string.Empty,
                ToolCalls = []
            });

        // Act
        var result = await _sut.RunAsync(messages);

        // Assert
        result.AssistantMessage.Should().Be(
            "The agent completed the request but returned no textual response.");

        result.UsedTools.Should().BeEmpty();

        _toolExecutor.Verify(
            x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<BinaryData>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_Should_Throw_When_Messages_Are_Null()
    {
        // Act
        Func<Task> act = () => _sut.RunAsync(null!);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RunAsync_Should_Throw_When_FinishReason_Is_ToolCalls_But_List_Is_Empty()
    {
        // Arrange
        var messages = new List<ChatMessage>
    {
        new UserChatMessage("Use a tool")
    };

        _chatCompletionService
            .Setup(x => x.CompleteAsync(
                It.IsAny<ICollection<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatCompletionResult
            {
                FinishReason = ChatFinishReason.ToolCalls,
                ToolCalls = []
            });

        // Act
        Func<Task> act = () => _sut.RunAsync(messages);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "OpenAI returned ToolCalls finish reason without any tool calls.");

        _toolExecutor.Verify(
            x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<BinaryData>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_Should_Throw_When_Tool_Call_Name_Is_Empty()
    {
        // Arrange
        var messages = new List<ChatMessage>
    {
        new UserChatMessage("Use a tool")
    };

        _chatCompletionService
            .Setup(x => x.CompleteAsync(
                It.IsAny<ICollection<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatCompletionResult
            {
                FinishReason = ChatFinishReason.ToolCalls,
                ToolCalls =
                [
                    new ToolCallResult
                {
                    Id = "tool-call-1",
                    Name = string.Empty,
                    Arguments = BinaryData.FromString("{}")
                }
                ]
            });

        // Act
        Func<Task> act = () => _sut.RunAsync(messages);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Tool call name cannot be empty.");

        _toolExecutor.Verify(
            x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<BinaryData>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_Should_Throw_When_Tool_Call_Id_Is_Empty()
    {
        // Arrange
        var messages = new List<ChatMessage>
    {
        new UserChatMessage("Use a tool")
    };

        _chatCompletionService
            .Setup(x => x.CompleteAsync(
                It.IsAny<ICollection<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatCompletionResult
            {
                FinishReason = ChatFinishReason.ToolCalls,
                ToolCalls =
                [
                    new ToolCallResult
                {
                    Id = string.Empty,
                    Name = "calculate",
                    Arguments = BinaryData.FromString(
                        """{"expression":"2+2"}""")
                }
                ]
            });

        // Act
        Func<Task> act = () => _sut.RunAsync(messages);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Tool call ID cannot be empty.");

        _toolExecutor.Verify(
            x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<BinaryData>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_Should_Throw_When_FinishReason_Is_Not_Supported()
    {
        // Arrange
        var messages = new List<ChatMessage>
    {
        new UserChatMessage("Hello")
    };

        _chatCompletionService
            .Setup(x => x.CompleteAsync(
                It.IsAny<ICollection<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatCompletionResult
            {
                FinishReason = ChatFinishReason.Length,
                ToolCalls = []
            });

        // Act
        Func<Task> act = () => _sut.RunAsync(messages);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Unsupported finish reason:*");
    }

    [Fact]
    public async Task RunAsync_Should_Throw_When_Maximum_Number_Of_Rounds_Is_Exceeded()
    {
        // Arrange
        var messages = new List<ChatMessage>
    {
        new UserChatMessage("Keep using tools")
    };

        var toolCallCompletion = new ChatCompletionResult
        {
            FinishReason = ChatFinishReason.ToolCalls,
            ToolCalls =
            [
                new ToolCallResult
            {
                Id = "tool-call-1",
                Name = "calculate",
                Arguments = BinaryData.FromString(
                    """{"expression":"2+2"}""")
            }
            ]
        };

        _chatCompletionService
            .Setup(x => x.CompleteAsync(
                It.IsAny<ICollection<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolCallCompletion);

        _toolExecutor
            .Setup(x => x.ExecuteAsync(
                "calculate",
                It.IsAny<BinaryData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"result":"4"}""");

        // Act
        Func<Task> act = () => _sut.RunAsync(messages);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                "Agent exceeded the maximum number of 3 rounds.");

        _chatCompletionService.Verify(
            x => x.CompleteAsync(
                It.IsAny<ICollection<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        _toolExecutor.Verify(
            x => x.ExecuteAsync(
                "calculate",
                It.IsAny<BinaryData>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task RunAsync_Should_Return_All_Used_Tools()
    {
        // Arrange
        var messages = new List<ChatMessage>
    {
        new UserChatMessage("Calculate and check the time")
    };

        var firstCompletion = new ChatCompletionResult
        {
            FinishReason = ChatFinishReason.ToolCalls,
            ToolCalls =
            [
                new ToolCallResult
            {
                Id = "tool-call-1",
                Name = "calculate",
                Arguments = BinaryData.FromString(
                    """{"expression":"2+2"}""")
            }
            ]
        };

        var secondCompletion = new ChatCompletionResult
        {
            FinishReason = ChatFinishReason.ToolCalls,
            ToolCalls =
            [
                new ToolCallResult
            {
                Id = "tool-call-2",
                Name = "get_current_time",
                Arguments = BinaryData.FromString(
                    """{"timeZone":"Asia/Jerusalem"}""")
            }
            ]
        };

        var finalCompletion = new ChatCompletionResult
        {
            FinishReason = ChatFinishReason.Stop,
            AssistantMessage = "The result is 4 and the current time was checked.",
            ToolCalls = []
        };

        _chatCompletionService
            .SetupSequence(x => x.CompleteAsync(
                It.IsAny<ICollection<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstCompletion)
            .ReturnsAsync(secondCompletion)
            .ReturnsAsync(finalCompletion);

        _toolExecutor
            .Setup(x => x.ExecuteAsync(
                "calculate",
                It.IsAny<BinaryData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"result":"4"}""");

        _toolExecutor
            .Setup(x => x.ExecuteAsync(
                "get_current_time",
                It.IsAny<BinaryData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"localTime":"2026-08-02T11:00:00+03:00"}""");

        // Act
        var result = await _sut.RunAsync(messages);

        // Assert
        result.AssistantMessage.Should()
            .Be("The result is 4 and the current time was checked.");

        result.UsedTools.Should().BeEquivalentTo(
            ["calculate", "get_current_time"]);

        _chatCompletionService.Verify(
            x => x.CompleteAsync(
                It.IsAny<ICollection<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task RunAsync_Should_Return_Distinct_Used_Tools()
    {
        // Arrange
        var messages = new List<ChatMessage>
    {
        new UserChatMessage("Calculate twice")
    };

        var toolCompletion = new ChatCompletionResult
        {
            FinishReason = ChatFinishReason.ToolCalls,
            ToolCalls =
            [
                new ToolCallResult
            {
                Id = "tool-call-1",
                Name = "calculate",
                Arguments = BinaryData.FromString(
                    """{"expression":"2+2"}""")
            }
            ]
        };

        var finalCompletion = new ChatCompletionResult
        {
            FinishReason = ChatFinishReason.Stop,
            AssistantMessage = "Done",
            ToolCalls = []
        };

        _chatCompletionService
            .SetupSequence(x => x.CompleteAsync(
                It.IsAny<ICollection<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolCompletion)
            .ReturnsAsync(new ChatCompletionResult
            {
                FinishReason = ChatFinishReason.ToolCalls,
                ToolCalls =
                [
                    new ToolCallResult
                {
                    Id = "tool-call-2",
                    Name = "calculate",
                    Arguments = BinaryData.FromString(
                        """{"expression":"3+3"}""")
                }
                ]
            })
            .ReturnsAsync(finalCompletion);

        _toolExecutor
            .Setup(x => x.ExecuteAsync(
                "calculate",
                It.IsAny<BinaryData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"result":"value"}""");

        // Act
        var result = await _sut.RunAsync(messages);

        // Assert
        result.UsedTools.Should().ContainSingle();
        result.UsedTools.Should().Contain("calculate");

        _toolExecutor.Verify(
            x => x.ExecuteAsync(
                "calculate",
                It.IsAny<BinaryData>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}