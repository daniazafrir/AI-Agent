using Agent.Api.Conversations;
using System.Collections.Concurrent;

namespace Agent.Api.Features.Conversation;

public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<Guid, List<StoredMessage>> _conversations = new();

    public Task AddMessageAsync(
        Guid conversationId,
        string role,
        string content,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

    public Task<IReadOnlyList<Entitites.ConversationMessage>> GetMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_conversations.TryGetValue(
                conversationId,
                out var messages))
        {
            return Task.FromResult<IReadOnlyList<Entitites.ConversationMessage>>([]);
        }

        lock (messages)
        {
            IReadOnlyList<Entitites.ConversationMessage> result = messages
                .OrderBy(message => message.CreatedAtUtc)
                .Select(message => new Entitites.ConversationMessage
                {
                    ConversationId = conversationId,
                    Role = message.Role,
                    Content = message.Content,
                    CreatedAtUtc = message.CreatedAtUtc
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
}
