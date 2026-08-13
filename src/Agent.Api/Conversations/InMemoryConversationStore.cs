using System.Collections.Concurrent;
using Agent.Api.Entitites;
using Agent.Api.Features.Conversation;

namespace Agent.Api.Conversations;

public sealed class InMemoryConversationStore
    : IConversationStore
{
    private readonly ConcurrentDictionary<
        Guid,
        List<StoredMessage>> _conversations = new();

    public Task AddMessageAsync(
        Guid conversationId,
        string role,
        string content,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Conversation id cannot be empty.",
                nameof(conversationId));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException(
                "Message role cannot be empty.",
                nameof(role));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "Message content cannot be empty.",
                nameof(content));
        }

        var message = new StoredMessage(
            role.Trim(),
            content.Trim(),
            DateTime.UtcNow);

        var messages = _conversations.GetOrAdd(
            conversationId,
            _ => []);

        lock (messages)
        {
            messages.Add(message);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ConversationSummary>>
        GetConversationsAsync(
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = new List<ConversationSummary>();

        foreach (var entry in _conversations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var conversationId = entry.Key;
            var messages = entry.Value;

            lock (messages)
            {
                if (messages.Count == 0)
                {
                    continue;
                }

                var firstUserMessage = messages
                    .Where(message =>
                        string.Equals(
                            message.Role,
                            "user",
                            StringComparison.OrdinalIgnoreCase))
                    .OrderBy(message =>
                        message.CreatedAtUtc)
                    .FirstOrDefault();

                var lastUpdated = messages
                    .Max(message =>
                        message.CreatedAtUtc);

                result.Add(
                    new ConversationSummary(
                        conversationId,
                        CreateConversationTitle(
                            firstUserMessage?.Content),
                        new DateTimeOffset(
                            DateTime.SpecifyKind(
                                lastUpdated,
                                DateTimeKind.Utc))));
            }
        }

        IReadOnlyList<ConversationSummary> ordered =
            result
                .OrderByDescending(conversation =>
                    conversation.LastUpdated)
                .ToList();

        return Task.FromResult(ordered);
    }

    public Task<IReadOnlyList<Entitites.ConversationMessage>>
        GetMessagesAsync(
            Guid conversationId,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_conversations.TryGetValue(
                conversationId,
                out var messages))
        {
            return Task.FromResult<
                IReadOnlyList<Entitites.ConversationMessage>>(
                    []);
        }

        lock (messages)
        {
            IReadOnlyList<Entitites.ConversationMessage> result =
                messages
                    .OrderBy(message =>
                        message.CreatedAtUtc)
                    .Select(message =>
                        new Entitites.ConversationMessage
                        {
                            ConversationId =
                                conversationId,

                            Role =
                                message.Role,

                            Content =
                                message.Content,

                            CreatedAtUtc =
                                message.CreatedAtUtc
                        })
                    .ToList();

            return Task.FromResult(result);
        }
    }

    public Task DeleteConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _conversations.TryRemove(
            conversationId,
            out _);

        return Task.CompletedTask;
    }

    private static string CreateConversationTitle(
        string? firstUserMessage)
    {
        if (string.IsNullOrWhiteSpace(
                firstUserMessage))
        {
            return "New conversation";
        }

        const int maxLength = 50;

        var title =
            firstUserMessage.Trim();

        return title.Length <= maxLength
            ? title
            : $"{title[..maxLength]}...";
    }

    private sealed record StoredMessage(
        string Role,
        string Content,
        DateTime CreatedAtUtc);
}