namespace Agent.Api.Features.Conversation;

public sealed record ConversationSummary(
    Guid Id,
    string Title,
    DateTimeOffset LastUpdated);
