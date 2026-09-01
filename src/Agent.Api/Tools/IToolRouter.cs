using OpenAI.Chat;

namespace Agent.Api.Tools;

public interface IToolRouter
{
    IReadOnlyList<ChatTool> SelectTools(
        IReadOnlyList<ChatMessage> messages);
}