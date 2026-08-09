namespace Agent.Api.Infrastructure.Middleware;

public static class CorrelationIdExtensions
{
    public static IApplicationBuilder
        UseCorrelationId(
            this IApplicationBuilder app)
    {
        return app.UseMiddleware<
            CorrelationIdMiddleware>();
    }
}