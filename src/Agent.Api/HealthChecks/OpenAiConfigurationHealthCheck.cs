using Agent.Api.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Agent.Api.HealthChecks;

public sealed class OpenAiConfigurationHealthCheck(
    IOptions<OpenAiOptions> openAiOptions)
    : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var options = openAiOptions.Value;

        var apiKey =
            !string.IsNullOrWhiteSpace(options.ApiKey)
                ? options.ApiKey
                : Environment.GetEnvironmentVariable(
                    "OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "OpenAI API key is missing."));
        }

        if (string.IsNullOrWhiteSpace(options.Model))
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "OpenAI model is missing."));
        }

        return Task.FromResult(
            HealthCheckResult.Healthy(
                $"OpenAI configuration is valid. " +
                $"Model: {options.Model}."));
    }
}