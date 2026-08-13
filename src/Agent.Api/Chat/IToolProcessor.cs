namespace Agent.Api.Chat;

public interface IToolProcessor
{
    Task ProcessAsync(
        ChatCompletionResult completion,
        AgentContext context,
        CancellationToken cancellationToken);
}