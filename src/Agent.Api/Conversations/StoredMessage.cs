namespace Agent.Api.Conversations;

public sealed record StoredMessage(
    string Role,
    string Content,
    DateTime CreatedAtUtc);