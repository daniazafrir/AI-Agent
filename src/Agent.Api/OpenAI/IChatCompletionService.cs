using OpenAI.Chat;

namespace Agent.Api.OpenAI;

public interface IChatCompletionService
{
    Task<ChatCompletionResult> CompleteAsync(
        ICollection<ChatMessage> messages,
        ChatCompletionOptions options,
        CancellationToken cancellationToken);
}