using Agent.Api.Chat;
using Agent.Api.Contracts;
using Agent.Api.Features.Chat;
using Microsoft.AspNetCore.Mvc;

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
}
