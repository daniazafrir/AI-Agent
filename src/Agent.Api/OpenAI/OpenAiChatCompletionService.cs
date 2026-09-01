using OpenAI.Chat;

namespace Agent.Api.OpenAI;

public sealed class OpenAiChatCompletionService(
    ChatClient chatClient)
    : IChatCompletionService
{
    public async Task<ChatCompletionResult> CompleteAsync(
    ICollection<ChatMessage> messages,
    ChatCompletionOptions options,
    CancellationToken cancellationToken)
    {
        ChatCompletion completion =
            await chatClient.CompleteChatAsync(
                messages,
                options,
                cancellationToken);

        return new ChatCompletionResult
        {
            FinishReason = completion.FinishReason,

            AssistantMessage =
                string.Join(
                    Environment.NewLine,
                    completion.Content
                        .Select(x => x.Text)
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x))),

            ToolCalls = completion.ToolCalls
    .Select(toolCall => new ToolCallResult
    {
        Id = toolCall.Id,
        Name = toolCall.FunctionName,
        Arguments = toolCall.FunctionArguments
    })
    
                .ToList(),

            RawCompletion = completion
        };
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
    ICollection<ChatMessage> messages,
    ChatCompletionOptions options,
    [System.Runtime.CompilerServices.EnumeratorCancellation]
    CancellationToken cancellationToken)
    {
        await foreach (var update in chatClient.CompleteChatStreamingAsync(
                           messages,
                           options,
                           cancellationToken))
        {
            yield return update;
        }
    }
    private static string GetAssistantMessage(
        ChatCompletion completion)
    {
        return string.Join(
            Environment.NewLine,
            completion.Content
                .Select(contentPart => contentPart.Text)
                .Where(text =>
                    !string.IsNullOrWhiteSpace(text)));
    }
}