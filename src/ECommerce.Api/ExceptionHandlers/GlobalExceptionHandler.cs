using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.ExceptionHandlers;

// Milan Jovanovic approach: focused IExceptionHandler + IProblemDetailsService.
// Catch-all handler — must be registered LAST so specific handlers run first.
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception occurred");

        httpContext.Response.StatusCode = exception switch
        {
            BadHttpRequestException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Type = exception.GetType().Name,
            Title = httpContext.Response.StatusCode switch
            {
                StatusCodes.Status400BadRequest => "Bad request",
                StatusCodes.Status401Unauthorized => "Unauthorized",
                _ => "An error occurred"
            },
            Detail = environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred. Please try again later.",
            Status = httpContext.Response.StatusCode,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
