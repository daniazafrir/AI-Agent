using Agent.Api.Tools;
using Agent.Api.Chat;
using Agent.Api.Chat.Models;
using Agent.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Agent.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController(
    IChatOrchestrator chatOrchestrator,
    ILogger<ChatController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message is required." });
        }

        var response = await chatOrchestrator.ChatAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("stream")]
    public async Task Stream(
    [FromBody] ChatRequest request,
    CancellationToken cancellationToken)
    {
        Response.StatusCode =
            StatusCodes.Status200OK;

        Response.ContentType =
            "text/event-stream";

        Response.Headers.CacheControl =
            "no-cache";

        Response.Headers.Connection =
            "keep-alive";

        try
        {
            await foreach (
                var streamEvent in
                    chatOrchestrator.ChatStreamingAsync(
                        request,
                        cancellationToken))
            {
                await WriteEventAsync(
                    streamEvent,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Chat stream was canceled by the client.");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Chat stream failed after the response started.");

            if (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                await WriteEventAsync(
                    new ChatStreamEvent
                    {
                        Type = "error",
                        Content = exception is KnowledgeSearchUnavailableException
                            ? KnowledgeSearchUnavailableException.UserMessage
                            : "The chat stream failed unexpectedly."
                    },
                    HttpContext.RequestAborted);
            }
        }
    }

    private async Task WriteEventAsync(
        ChatStreamEvent streamEvent,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(streamEvent);

        await Response.WriteAsync(
            $"data: {json}\n\n",
            cancellationToken);

        await Response.Body.FlushAsync(cancellationToken);
    }
}

