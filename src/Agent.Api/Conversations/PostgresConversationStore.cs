using Agent.Api.Entitites;
using Agent.Api.Features.Conversation;
using Agent.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Agent.Api.Conversations;

public sealed class PostgresConversationStore(
    AgentDbContext dbContext,
    ILogger<PostgresConversationStore> logger)
    : IConversationStore
{
    public async Task<IReadOnlyList<Entitites.ConversationMessage>> GetMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ConversationMessages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddMessageAsync(
        Guid conversationId,
        string role,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("Conversation id cannot be empty.", nameof(conversationId));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Message role is required.", nameof(role));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Message content is required.", nameof(content));
        }

        var now = DateTime.UtcNow;
        var conversation = await dbContext.Conversations
            .SingleOrDefaultAsync(x => x.Id == conversationId, cancellationToken);

        if (conversation is null)
        {
            conversation = new Conversation
            {
                Id = conversationId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            dbContext.Conversations.Add(conversation);
        }
        else
        {
            conversation.UpdatedAtUtc = now;
        }

        dbContext.ConversationMessages.Add(new Entitites.ConversationMessage
        {
            ConversationId = conversationId,
            Role = role.Trim(),
            Content = content.Trim(),
            CreatedAtUtc = now
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(
                exception,
                "Failed to save a message for conversation {ConversationId}.",
                conversationId);
            throw;
        }
    }

    public async Task DeleteConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.Conversations
            .SingleOrDefaultAsync(x => x.Id == conversationId, cancellationToken);

        if (conversation is null)
        {
            return;
        }

        dbContext.Conversations.Remove(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationSummary>>
    GetConversationsAsync(
        CancellationToken cancellationToken = default)
    {
        var conversations =
            await dbContext.Conversations
                .AsNoTracking()
                .OrderByDescending(x => x.UpdatedAtUtc)
                .Select(conversation => new
                {
                    conversation.Id,
                    conversation.UpdatedAtUtc,

                    Title = dbContext.ConversationMessages
                        .Where(message =>
                            message.ConversationId == conversation.Id &&
                            message.Role == "user")
                        .OrderBy(message => message.CreatedAtUtc)
                        .Select(message => message.Content)
                        .FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

        return conversations
            .Select(conversation =>
                new ConversationSummary(
                    conversation.Id,
                    CreateConversationTitle(
                        conversation.Title),
                    new DateTimeOffset(
                        conversation.UpdatedAtUtc,
                        TimeSpan.Zero)))
            .ToList();
    }

    private static string CreateConversationTitle(
    string? firstUserMessage)
    {
        if (string.IsNullOrWhiteSpace(firstUserMessage))
        {
            return "New conversation";
        }

        const int maxLength = 50;

        var title = firstUserMessage.Trim();

        return title.Length <= maxLength
            ? title
            : $"{title[..maxLength]}...";
    }
}
