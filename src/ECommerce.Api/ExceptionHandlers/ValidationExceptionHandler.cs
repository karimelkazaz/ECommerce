using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.ExceptionHandlers;

// Milan Jovanovic approach: specific handler for FluentValidation failures.
// Registered BEFORE GlobalExceptionHandler so it gets first chance (return false to pass through).
internal sealed class ValidationExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        logger.LogError(exception, "Validation exception occurred");

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Type = nameof(ValidationException),
                Title = "Validation failed",
                Detail = "One or more validation errors occurred",
                Status = StatusCodes.Status400BadRequest,
                Instance = httpContext.Request.Path
            }
        };

        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        context.ProblemDetails.Extensions["errors"] = errors;
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return await problemDetailsService.TryWriteAsync(context);
    }
}
