using System.ClientModel.Primitives;
using OpenAI.Chat;

namespace Agent.Api.Chat.Models;

public sealed record PromptSnapshot(
    int Round, string Model, DateTimeOffset CapturedAtUtc,
    string MessagesJson, string OptionsJson)
{
    // Serialize with the SDK so roles, tool-call IDs, arguments and schemas
    // remain identical to the inputs passed to ChatClient. No transport headers.
    public static PromptSnapshot Capture(int round, string model,
        IEnumerable<ChatMessage> messages, ChatCompletionOptions options) =>
        new(round, model, DateTimeOffset.UtcNow,
            "[" + string.Join(",", messages.Select(message =>
                ModelReaderWriter.Write(message).ToString())) + "]",
            ModelReaderWriter.Write(options).ToString());
}
