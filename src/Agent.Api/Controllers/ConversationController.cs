using Agent.Api.Conversations;
using Agent.Api.Features.Conversation;
using Microsoft.AspNetCore.Mvc;

namespace Agent.Api.Controllers;

[ApiController]
[Route("api/conversations")]
public sealed class ConversationsController(
    IConversationStore conversationStore)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationSummaryResponse>>>
        GetAll(
            CancellationToken cancellationToken)
    {
        var conversations =
            await conversationStore.GetConversationsAsync(
                cancellationToken);

        var response = conversations
            .Select(conversation =>
                new ConversationSummaryResponse
                {
                    Id = conversation.Id,
                    Title = conversation.Title,
                    LastUpdated = conversation.LastUpdated
                })
            .ToList();

        return Ok(response);
    }

    [HttpGet("{conversationId:guid}")]
    public async Task<ActionResult<ConversationDetailsResponse>>
        GetById(
            Guid conversationId,
            CancellationToken cancellationToken)
    {
        var messages =
            await conversationStore.GetMessagesAsync(
                conversationId,
                cancellationToken);

        var response =
            new ConversationDetailsResponse
            {
                ConversationId = conversationId,

                Messages = messages
                    .Select(message =>
                        new ConversationMessageResponse
                        {
                            Role = message.Role,
                            Content = message.Content,
                            Trace = Agent.Api.Chat.Models.ConversationTrace.Parse(message.TraceJson)
                        })
                    .ToList()
            };

        return Ok(response);
    }
}

public sealed class ConversationSummaryResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } =
        string.Empty;

    public DateTimeOffset LastUpdated { get; init; }
}

public sealed class ConversationDetailsResponse
{
    public Guid ConversationId { get; init; }

    public IReadOnlyList<ConversationMessageResponse>
        Messages
    { get; init; } =
            [];
}

public sealed class ConversationMessageResponse
{
    public Agent.Api.Chat.Models.ConversationTrace? Trace { get; init; }
    public IReadOnlyList<Agent.Api.Features.Conversation.KnowledgeSource> Sources => Trace?.Sources ?? [];
    public bool Incomplete => Trace is not null && Trace.Status != "completed";
    public string Role { get; init; } =
        string.Empty;

    public string Content { get; init; } =
        string.Empty;

    public IReadOnlyList<string> UsedTools => Trace?.UsedTools ?? [];
}
