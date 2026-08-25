using ECommerce.Domain.Common;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        var error = new ValidationError(failures
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToArray());

        return CreateFailure(error);
    }

    private static TResponse CreateFailure(Error error)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];

            var failure = typeof(Result)
                .GetMethods()
                .Single(m =>
                    m.Name == nameof(Result.Failure) &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 1)
                .MakeGenericMethod(valueType)
                .Invoke(null, new object?[] { error });

            return (TResponse)failure!;
        }

        throw new InvalidOperationException(
            $"Validation failures can only be represented as Result or Result<T>, not '{responseType.Name}'.");
    }
}
