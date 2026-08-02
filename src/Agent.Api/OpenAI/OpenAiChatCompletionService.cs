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

            AssistantMessage = GetAssistantMessage(completion),

            ToolCalls = completion.ToolCalls
                .Select(toolCall => new ToolCallResult
                {
                    Id = toolCall.Id,
                    Name = toolCall.FunctionName,
                    Arguments = toolCall.FunctionArguments
                })
                .ToArray()
        };
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