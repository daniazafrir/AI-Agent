using Agent.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Agent.Api.HealthChecks;

public sealed class PostgresHealthCheck(
    AgentDbContext dbContext,
    ILogger<PostgresHealthCheck> logger)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect =
                await dbContext.Database.CanConnectAsync(
                    cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy(
                    "PostgreSQL is available.")
                : HealthCheckResult.Unhealthy(
                    "PostgreSQL is unavailable.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "PostgreSQL health check failed.");

            return HealthCheckResult.Unhealthy(
                "PostgreSQL health check failed.",
                exception);
        }
    }
}