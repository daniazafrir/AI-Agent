using Agent.Api.Mcp;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Agent.Api.HealthChecks;

public sealed class McpHealthCheck(
    IMcpToolClient mcpToolClient,
    ILogger<McpHealthCheck> logger)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tools = await mcpToolClient.ListToolsAsync(
                cancellationToken);

            return HealthCheckResult.Healthy(
                $"MCP server is available. " +
                $"{tools.Count} tool(s) discovered.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "MCP health check failed.");

            return HealthCheckResult.Unhealthy(
                "MCP server is unavailable.",
                exception);
        }
    }
}