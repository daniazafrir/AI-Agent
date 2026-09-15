using System.Text.Json;
using Agent.Api.Mcp;
using Agent.Api.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Agent.Api.Tests.Tools;

public sealed class ToolExecutorTests
{
    private readonly Mock<IMcpToolClient> _mcpToolClient = new();
    private readonly Mock<IMcpToolRegistry> _toolRegistry = new();
    private readonly Mock<ILogger<ToolExecutor>> _logger = new();

    private readonly ToolExecutor _sut;

    public ToolExecutorTests()
    {
        _toolRegistry
            .Setup(x => x.InitializeAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new ToolExecutor(
            _mcpToolClient.Object,
            _toolRegistry.Object,
            _logger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Call_Mcp_Tool_When_Tool_Is_Known()
    {
        // Arrange
        const string toolName = "get_weather";

        var arguments = BinaryData.FromString(
            """
            {
              "city": "Tel Aviv",
              "days": 3,
              "includeHumidity": true
            }
            """);

        _toolRegistry
            .Setup(x => x.Contains(toolName))
            .Returns(true);

        _mcpToolClient
            .Setup(x => x.CallToolAsync(
                toolName,
                It.Is<IReadOnlyDictionary<string, object?>>(values =>
                    values.Count == 3 &&
                    Equals(values["city"], "Tel Aviv") &&
                    Equals(values["days"], 3L) &&
                    Equals(values["includeHumidity"], true)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                """
                {
                  "success": true,
                  "temperature": 27
                }
                """);

        // Act
        var result = await _sut.ExecuteAsync(
            toolName,
            arguments);

        // Assert
        result.Content.Should().Contain("\"success\": true");

        _toolRegistry.Verify(x =>
            x.InitializeAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _toolRegistry.Verify(x =>
            x.Contains(toolName),
            Times.Once);

        _mcpToolClient.Verify(x =>
            x.CallToolAsync(
                toolName,
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Error_When_Tool_Name_Is_Empty()
    {
        // Arrange
        var arguments = BinaryData.FromString("{}");

        // Act
        var result = await _sut.ExecuteAsync(
            string.Empty,
            arguments);

        // Assert
        using var document = JsonDocument.Parse(result.RawContent);

        document.RootElement
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeFalse();

        document.RootElement
            .GetProperty("error")
            .GetString()
            .Should()
            .Be("Tool name is required.");

        _toolRegistry.Verify(x =>
            x.InitializeAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        _mcpToolClient.Verify(x =>
            x.CallToolAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Error_When_Tool_Is_Unknown()
    {
        // Arrange
        const string toolName = "unknown_tool";

        _toolRegistry
            .Setup(x => x.Contains(toolName))
            .Returns(false);

        // Act
        var result = await _sut.ExecuteAsync(
            toolName,
            BinaryData.FromString("{}"));

        // Assert
        using var document = JsonDocument.Parse(result.RawContent);

        document.RootElement
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeFalse();

        document.RootElement
            .GetProperty("tool")
            .GetString()
            .Should()
            .Be(toolName);

        document.RootElement
            .GetProperty("error")
            .GetString()
            .Should()
            .Be($"Unknown MCP tool: {toolName}");

        _toolRegistry.Verify(x =>
            x.InitializeAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mcpToolClient.Verify(x =>
            x.CallToolAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Error_When_Arguments_Are_Invalid_Json()
    {
        // Arrange
        const string toolName = "calculate";

        _toolRegistry
            .Setup(x => x.Contains(toolName))
            .Returns(true);

        var invalidArguments = BinaryData.FromString(
            """{"expression":""");

        // Act
        var result = await _sut.ExecuteAsync(
            toolName,
            invalidArguments);

        // Assert
        using var document = JsonDocument.Parse(result.RawContent);

        document.RootElement
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeFalse();

        document.RootElement
            .GetProperty("error")
            .GetString()
            .Should()
            .StartWith("Invalid tool arguments:");

        _mcpToolClient.Verify(x =>
            x.CallToolAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Error_When_Arguments_Are_Not_Json_Object()
    {
        // Arrange
        const string toolName = "calculate";

        _toolRegistry
            .Setup(x => x.Contains(toolName))
            .Returns(true);

        var arrayArguments = BinaryData.FromString(
            """["2+2"]""");

        // Act
        var result = await _sut.ExecuteAsync(
            toolName,
            arrayArguments);

        // Assert
        using var document = JsonDocument.Parse(result.Content);

        document.RootElement
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeFalse();

        document.RootElement
            .GetProperty("error")
            .GetString()
            .Should()
            .Contain("Tool arguments must be a JSON object.");

        _mcpToolClient.Verify(x =>
            x.CallToolAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Convert_Nested_Json_Arguments()
    {
        // Arrange
        const string toolName = "create_event";

        var arguments = BinaryData.FromString(
            """
            {
              "title": "Architecture review",
              "duration": 45,
              "attendees": ["dana@example.com", "omer@example.com"],
              "settings": {
                "online": true,
                "room": null
              }
            }
            """);

        _toolRegistry
            .Setup(x => x.Contains(toolName))
            .Returns(true);

        _mcpToolClient
            .Setup(x => x.CallToolAsync(
                toolName,
                It.Is<IReadOnlyDictionary<string, object?>>(values =>
                    HasExpectedNestedArguments(values)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""{"success":true}""");

        // Act
        var result = await _sut.ExecuteAsync(
            toolName,
            arguments);

        // Assert
        result.Content.Should().Be("""{"success":true}""");

        _mcpToolClient.Verify(x =>
            x.CallToolAsync(
                toolName,
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Error_When_Mcp_Client_Throws()
    {
        // Arrange
        const string toolName = "get_current_time";

        _toolRegistry
            .Setup(x => x.Contains(toolName))
            .Returns(true);

        _mcpToolClient
            .Setup(x => x.CallToolAsync(
                toolName,
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "MCP server unavailable."));

        // Act
        var result = await _sut.ExecuteAsync(
            toolName,
            BinaryData.FromString(
                """{"timeZone":"Asia/Jerusalem"}"""));

        // Assert
        using var document = JsonDocument.Parse(result.Content);

        document.RootElement
            .GetProperty("success")
            .GetBoolean()
            .Should()
            .BeFalse();

        document.RootElement
            .GetProperty("tool")
            .GetString()
            .Should()
            .Be(toolName);

        document.RootElement
            .GetProperty("error")
            .GetString()
            .Should()
            .Be("MCP server unavailable.");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Propagate_Cancellation()
    {
        // Arrange
        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        // Act
        Func<Task> act = () => _sut.ExecuteAsync(
            "calculate",
            BinaryData.FromString(
                """{"expression":"2+2"}"""),
            cancellationTokenSource.Token);

        // Assert
        await act.Should()
            .ThrowAsync<OperationCanceledException>();

        _toolRegistry.Verify(x =>
            x.InitializeAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        _mcpToolClient.Verify(x =>
            x.CallToolAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Normalize_PascalCase_Knowledge_Matches()
    {
        // Arrange
        const string toolName = "search_knowledge";

        _toolRegistry
            .Setup(x => x.Contains(toolName))
            .Returns(true);

        _mcpToolClient
            .Setup(x => x.CallToolAsync(
                toolName,
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                """
                {
                  "matches": [
                    {
                      "DocumentName": "employee-handbook.pdf",
                      "Content": "Employees receive 20 vacation days."
                    }
                  ]
                }
                """);

        // Act
        var result = await _sut.ExecuteAsync(
            toolName,
            BinaryData.FromString(
                """{"query":"vacation days"}"""));

        // Assert
        result.Content.Should().Contain(
            "Source: employee-handbook.pdf");
        result.Content.Should().Contain(
            "Employees receive 20 vacation days.");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Error_When_Mcp_Tool_Times_Out()
    {
        // Arrange
        const string toolName = "search_knowledge";

        _toolRegistry
            .Setup(x => x.Contains(toolName))
            .Returns(true);

        _mcpToolClient
            .Setup(x => x.CallToolAsync(
                toolName,
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("MCP timeout."));

        var exception = await Assert.ThrowsAsync<KnowledgeSearchUnavailableException>(
            () => _sut.ExecuteAsync(toolName,
                BinaryData.FromString("""{"query":"vacation days"}""")));
        Assert.Equal(KnowledgeSearchUnavailableException.UserMessage, exception.Message);
    }
    [Fact]
    public async Task ExecuteAsync_Should_Use_Provided_CancellationToken()
    {
        // Arrange
        const string toolName = "calculate";

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        _toolRegistry
            .Setup(x => x.InitializeAsync(
                cancellationToken))
            .Returns(Task.CompletedTask);

        _toolRegistry
            .Setup(x => x.Contains(toolName))
            .Returns(true);

        _mcpToolClient
            .Setup(x => x.CallToolAsync(
                toolName,
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                cancellationToken))
            .ReturnsAsync("""{"result":4}""");

        // Act
        await _sut.ExecuteAsync(
            toolName,
            BinaryData.FromString(
                """{"expression":"2+2"}"""),
            cancellationToken);

        // Assert
        _toolRegistry.Verify(x =>
            x.InitializeAsync(cancellationToken),
            Times.Once);

        _mcpToolClient.Verify(x =>
            x.CallToolAsync(
                toolName,
                It.IsAny<IReadOnlyDictionary<string, object?>>(),
                cancellationToken),
            Times.Once);
    }

    private static bool HasExpectedNestedArguments(
        IReadOnlyDictionary<string, object?> values)
    {
        if (!Equals(
                values["title"],
                "Architecture review"))
        {
            return false;
        }

        if (!Equals(values["duration"], 45L))
        {
            return false;
        }

        if (values["attendees"] is not List<object?> attendees)
        {
            return false;
        }

        if (!attendees.SequenceEqual(
                new object?[]
                {
                    "dana@example.com",
                    "omer@example.com"
                }))
        {
            return false;
        }

        if (values["settings"] is not
            Dictionary<string, object?> settings)
        {
            return false;
        }

        return Equals(settings["online"], true)
               && settings["room"] is null;
    }
}

