using Agent.Api.Infrastructure.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Agent.Api.Infrastructure.Exceptions;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId =
    httpContext.Items[
        CorrelationIdMiddleware.HeaderName]
        ?.ToString()
    ?? string.Empty;

        logger.LogError(
            exception,
            "Unhandled exception. CorrelationId: {CorrelationId}",
            correlationId);

        var statusCode = exception switch
        {
            HttpRequestException =>
                StatusCodes.Status503ServiceUnavailable,

            OperationCanceledException =>
                StatusCodes.Status408RequestTimeout,

            ArgumentException =>
                StatusCodes.Status400BadRequest,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        var title = statusCode switch
        {
            StatusCodes.Status503ServiceUnavailable =>
                "External service unavailable",

            StatusCodes.Status408RequestTimeout =>
                "Request timeout",

            StatusCodes.Status400BadRequest =>
                "Invalid request",

            _ =>
                "Internal server error"
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode ==
                     StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["correlationId"] =
            correlationId;

        httpContext.Response.StatusCode =
            statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }
}