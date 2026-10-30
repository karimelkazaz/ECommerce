using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;

namespace ECommerce.IntegrationTests;

/// <summary>
/// Makes plain in-memory queryables compatible with EF Core's async operators
/// (FirstOrDefaultAsync / CountAsync / ToListAsync) so application handlers can be
/// exercised against list-backed fakes without a database.
/// </summary>
public static class TestAsyncQueryable
{
    public static IQueryable<T> AsTestAsyncQueryable<T>(this IQueryable<T> source)
        => new TestAsyncEnumerable<T>(source);

    private sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;

        public ValueTask<bool> MoveNextAsync()
            => ValueTask.FromResult(inner.MoveNext());

        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestAsyncEnumerable<T> : IQueryable<T>, IOrderedQueryable<T>, IAsyncEnumerable<T>
    {
        private readonly IQueryable<T> _inner;

        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            => _inner = enumerable.AsQueryable();

        public TestAsyncEnumerable(Expression expression)
            => _inner = new EnumerableQuery<T>(expression);

        public Type ElementType => typeof(T);
        public Expression Expression => _inner.Expression;
        public IQueryProvider Provider => new TestAsyncQueryProvider<T>(_inner.Provider);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(_inner.GetEnumerator());

        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();
    }

    private sealed class TestAsyncQueryProvider<T>(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new TestAsyncEnumerable<TElement>(expression);

        public IQueryable CreateQuery(Expression expression)
            => new TestAsyncEnumerable<object>(expression);

        public object? Execute(Expression expression)
            => inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression)
            => inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            if (typeof(TResult).IsGenericType
                && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
            {
                var elementType = typeof(TResult).GetGenericArguments()[0];

                var executed = typeof(IQueryProvider)
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Single(m => m.IsGenericMethod && m.Name == nameof(IQueryProvider.Execute))
                    .MakeGenericMethod(elementType)
                    .Invoke(inner, new object?[] { expression });

                return (TResult)typeof(Task)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Single(m => m.Name == nameof(Task.FromResult) && m.IsGenericMethod)
                    .MakeGenericMethod(elementType)
                    .Invoke(null, new object?[] { executed })!;
            }

            // e.g. IAsyncEnumerable<T> (materialized by AsAsyncEnumerable/ToListAsync)
            var materialized = (IEnumerable<T>)typeof(IQueryProvider)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Single(m => m.IsGenericMethod && m.Name == nameof(IQueryProvider.Execute))
                .MakeGenericMethod(typeof(T))
                .Invoke(inner, new object?[] { expression })!;

            return (TResult)(new TestAsyncEnumerable<T>(materialized) as object);
        }
    }
}
