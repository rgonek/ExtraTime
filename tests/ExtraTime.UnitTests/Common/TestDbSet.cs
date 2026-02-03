using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ExtraTime.UnitTests.Common;

/// <summary>
/// A mock DbSet implementation that properly supports IQueryable operations including Include
/// </summary>
internal sealed class TestDbSet<T> : DbSet<T>, IQueryable<T>, IAsyncEnumerable<T> where T : class
{
    private readonly IQueryable<T> _queryable;

    public TestDbSet(IEnumerable<T> data)
    {
        _queryable = data.AsQueryable();
    }

    public TestDbSet(IQueryable<T> queryable)
    {
        _queryable = queryable;
    }

    public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(_queryable.GetEnumerator());
    }

    public IEnumerator<T> GetEnumerator() => _queryable.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Type ElementType => _queryable.ElementType;

    public Expression Expression => _queryable.Expression;

    public IQueryProvider Provider => new TestAsyncQueryProvider<T>(_queryable.Provider);

    public override IEntityType EntityType => null!;
}
