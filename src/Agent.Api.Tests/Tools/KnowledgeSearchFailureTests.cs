using Agent.Api.Mcp;
using Agent.Api.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Agent.Api.Tests.Tools;

public sealed class KnowledgeSearchFailureTests
{
    private readonly Mock<IMcpToolClient> _client = new();
    private readonly Mock<IMcpToolRegistry> _registry = new();
    private ToolExecutor Create()
    {
        _registry.Setup(x => x.Contains("search_knowledge")).Returns(true);
        _registry.Setup(x => x.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new(_client.Object, _registry.Object, NullLogger<ToolExecutor>.Instance);
    }

    [Theory]
    [InlineData("{\"matches\":[]}")]
    [InlineData("{\"matches\":[{\"content\":\"19 days\"}]}")]
    public async Task SuccessfulSearch_IncludingEmpty_IsNotServiceFailure(string payload)
    {
        var sut = Create();
        _client.Setup(x => x.CallToolAsync("search_knowledge",
            It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);
        var result = await sut.ExecuteAsync("search_knowledge", BinaryData.FromString("{}"));
        Assert.Equal(payload, result.RawContent);
    }

    [Theory]
    [InlineData("{\"success\":false,\"matches\":[],\"error\":\"private backend details\"}")]
    [InlineData("{\"isError\":true,\"matches\":[]}")]
    [InlineData("unavailable")]
    [InlineData("{}")]
    public async Task FailedOrInvalidSearch_ThrowsSafeServiceError(string payload)
    {
        var sut = Create();
        _client.Setup(x => x.CallToolAsync("search_knowledge",
            It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);
        var exception = await Assert.ThrowsAsync<KnowledgeSearchUnavailableException>(
            () => sut.ExecuteAsync("search_knowledge", BinaryData.FromString("{}")));
        Assert.Equal(KnowledgeSearchUnavailableException.UserMessage, exception.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConnectionFailureOrTimeout_IsServiceFailure(bool timeout)
    {
        var sut = Create();
        _client.Setup(x => x.CallToolAsync("search_knowledge",
            It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(timeout ? new OperationCanceledException() : new HttpRequestException("private host"));
        await Assert.ThrowsAsync<KnowledgeSearchUnavailableException>(
            () => sut.ExecuteAsync("search_knowledge", BinaryData.FromString("{}")));
    }

    [Fact]
    public async Task CallerCancellation_IsPreserved()
    {
        var sut = Create();
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.ExecuteAsync("search_knowledge", BinaryData.FromString("{}"), cancel.Token));
    }
}
