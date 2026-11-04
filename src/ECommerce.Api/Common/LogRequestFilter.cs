using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ECommerce.Api.Common;

/// <summary>
/// Logs every request hitting an endpoint: start, completion (status code + duration)
/// or failure. Applied once at the versioned API group in <c>Program.cs</c>.
/// </summary>
public sealed class LogRequestFilter(ILogger<LogRequestFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var start = Stopwatch.GetTimestamp();

        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Endpoint display name is "Namespace.Class::Method" — extract the bare method name.
        var displayName = http.GetEndpoint()?.DisplayName;
        var actionName = displayName?.Split("::", StringSplitOptions.RemoveEmptyEntries)
            is { Length: > 0 } parts
            ? parts[^1]
            : displayName;

        try
        {
            var result = await next(context);

            var elapsed = Stopwatch.GetElapsedTime(start);

            int? statusCode = http.Response.HasStarted
                ? http.Response.StatusCode
                : result switch
                {
                    IStatusCodeHttpResult { StatusCode: { } code } => code,
                    _ => null,
                };

            logger.LogInformation(
                "HTTP {Method} {Path} {Action} by user {UserId} responded {StatusCode} in {Elapsed:F1} ms",
                http.Request.Method,
                http.Request.Path,
                actionName,
                userId,
                statusCode,
                elapsed.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);

            logger.LogError(
                ex,
                "HTTP {Method} {Path} {Action} by user {UserId} failed after {Elapsed:F1} ms",
                http.Request.Method,
                http.Request.Path,
                actionName,
                userId,
                elapsed.TotalMilliseconds);

            throw;
        }
    }
}
