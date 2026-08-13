namespace Agent.Api.Chat;

public interface IAgentLoop
{
    Task<AgentRunResult> RunAsync(
        AgentContext context,
        CancellationToken cancellationToken);
}