using ECommerce.Domain.Common;

namespace ECommerce.Api.Common;

public static class ResultExtensions
{
    public static IResult ToProblemDetails(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert a successful result to a problem response.");

        return result.Error.ToProblemDetails();
    }

    public static IResult ToProblemDetails(this Error error)
    {
        var (statusCode, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation failed"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Server failure"),
        };

        if (error is ValidationError validationError)
        {
            return Results.ValidationProblem(
                validationError.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()),
                statusCode: statusCode,
                title: title,
                extensions: new Dictionary<string, object?>
                {
                    ["errorCode"] = error.Code,
                });
        }

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = error.Code,
            });
    }
}
