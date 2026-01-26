using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace ExtraTime.UnitTests.Common;

internal sealed class TestAsyncQueryProvider<T> : IAsyncQueryProvider
{
    private readonly IQueryProvider _innerQueryProvider;

    public TestAsyncQueryProvider(IQueryProvider innerQueryProvider)
    {
        _innerQueryProvider = innerQueryProvider;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<T>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(Expression expression)
    {
        return _innerQueryProvider.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _innerQueryProvider.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments().FirstOrDefault() ?? typeof(TResult);
        var result = _innerQueryProvider.Execute(expression);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, new[] { result })!;
    }
}

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _innerEnumerator;

    public TestAsyncEnumerator(IEnumerator<T> innerEnumerator)
    {
        _innerEnumerator = innerEnumerator;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(_innerEnumerator.MoveNext());
    }

    public T Current => _innerEnumerator.Current;

    public ValueTask DisposeAsync()
    {
        _innerEnumerator.Dispose();
        return ValueTask.CompletedTask;
    }
}
