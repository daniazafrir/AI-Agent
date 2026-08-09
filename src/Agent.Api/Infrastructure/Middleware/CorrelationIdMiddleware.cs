using Serilog.Context;

namespace Agent.Api.Infrastructure.Middleware;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId =
            context.Request.Headers.TryGetValue(
                HeaderName,
                out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue)
                ? headerValue.ToString()
                : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty(
                   "CorrelationId",
                   correlationId))
        {
            await next(context);
        }
    }
}