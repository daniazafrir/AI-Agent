using Agent.Api.Features.Conversation;
using Microsoft.AspNetCore.Mvc;

namespace Agent.Api.Controllers;

[ApiController]
[Route("api/conversations")]
public sealed class ConversationController(IConversationStore conversationStore)
    : ControllerBase
{
    [HttpGet("{conversationId:guid}")]
    public async Task<IActionResult> Get(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var messages = await conversationStore.GetMessagesAsync(
            conversationId,
            cancellationToken);

        return Ok(messages.Select(x => new
        {
            x.Id,
            x.ConversationId,
            x.Role,
            x.Content,
            x.CreatedAtUtc
        }));
    }

    [HttpDelete("{conversationId:guid}")]
    public async Task<IActionResult> Delete(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        await conversationStore.DeleteConversationAsync(
            conversationId,
            cancellationToken);

        return NoContent();
    }
}
