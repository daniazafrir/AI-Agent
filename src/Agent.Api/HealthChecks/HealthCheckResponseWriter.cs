using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Agent.Api.HealthChecks;

public static class HealthCheckResponseWriter
{
    public static Task WriteAsync(
        HttpContext httpContext,
        HealthReport report)
    {
        httpContext.Response.ContentType =
            "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            durationMilliseconds =
                report.TotalDuration.TotalMilliseconds,

            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMilliseconds =
                    entry.Value.Duration.TotalMilliseconds,
                error = entry.Value.Exception?.Message
            })
        };

        return httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
    }
}