using OpenAI.Chat;

namespace Agent.Api.Chat;

public interface IOpenAIChatService
{
    Task<ChatCompletionResult> CompleteAsync(
        List<ChatMessage> messages,
        CancellationToken cancellationToken = default);
}