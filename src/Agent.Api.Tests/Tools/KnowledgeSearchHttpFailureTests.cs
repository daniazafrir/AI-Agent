using Agent.Api.Chat;
using Agent.Api.Chat.Models;
using Agent.Api.Contracts;
using Agent.Api.Controllers;
using Agent.Api.Infrastructure.Exceptions;
using Agent.Api.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Agent.Api.Tests.Tools;

public sealed class KnowledgeSearchHttpFailureTests
{
    [Fact]
    public async Task JsonFailure_Returns503WithoutPrivateDetails()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        await handler.TryHandleAsync(context,
            new KnowledgeSearchUnavailableException(new Exception("private-host")), default);
        Assert.Equal(503, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("knowledge search", body);
        Assert.DoesNotContain("private-host", body);
    }

    [Fact]
    public async Task StreamFailure_EmitsErrorAndEndsWithoutSuccessfulCompletion()
    {
        var orchestrator = new Mock<IChatOrchestrator>();
        orchestrator.Setup(x => x.ChatStreamingAsync(
            It.IsAny<ChatRequest>(), It.IsAny<CancellationToken>())).Returns(Fail());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var controller = new ChatController(orchestrator.Object, NullLogger<ChatController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
        await controller.Stream(new ChatRequest { Message = "Vacation days?" }, default);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("\"Type\":\"error\"", body);
        Assert.Contains(KnowledgeSearchUnavailableException.UserMessage, body);
        Assert.DoesNotContain("\"Type\":\"completed\"", body);
        Assert.DoesNotContain("private-host", body);
    }

    private static async IAsyncEnumerable<ChatStreamEvent> Fail()
    {
        yield return new ChatStreamEvent { Type = "conversation", ConversationId = Guid.NewGuid() };
        await Task.Yield();
        throw new KnowledgeSearchUnavailableException(new Exception("private-host"));
    }
}
