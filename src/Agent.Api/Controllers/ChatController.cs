using Agent.Api.Chat;
using Agent.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Agent.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController(IChatOrchestrator chatOrchestrator) : ControllerBase
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

        await foreach (
            var streamEvent in
                chatOrchestrator.ChatStreamingAsync(
                    request,
                    cancellationToken))
        {
            var json =
                JsonSerializer.Serialize(
                    streamEvent);

            await Response.WriteAsync(
                $"data: {json}\n\n",
                cancellationToken);

            await Response.Body.FlushAsync(
                cancellationToken);
        }
    }
}
