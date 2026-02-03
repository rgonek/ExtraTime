using ExtraTime.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Common;

public abstract class HandlerTestBase
{
    protected readonly IApplicationDbContext Context;
    protected readonly ICurrentUserService CurrentUserService;
    protected readonly CancellationToken CancellationToken = CancellationToken.None;

    protected HandlerTestBase()
    {
        Context = Substitute.For<IApplicationDbContext>();
        CurrentUserService = Substitute.For<ICurrentUserService>();
    }

    protected void SetCurrentUser(Guid userId, string email = "test@example.com")
    {
        CurrentUserService.UserId.Returns(userId);
        CurrentUserService.IsAuthenticated.Returns(true);
    }

    protected static DbSet<T> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = Substitute.For<DbSet<T>, IQueryable<T>, IAsyncEnumerable<T>>();

        // Setup IQueryable properties with async support
        ((IQueryable<T>)mockSet).Provider.Returns(new TestAsyncQueryProvider<T>(data.Provider));
        ((IQueryable<T>)mockSet).Expression.Returns(data.Expression);
        ((IQueryable<T>)mockSet).ElementType.Returns(data.ElementType);
        ((IQueryable<T>)mockSet).GetEnumerator().Returns(_ => data.GetEnumerator());

        // Setup IAsyncEnumerable
        ((IAsyncEnumerable<T>)mockSet).GetAsyncEnumerator(Arg.Any<CancellationToken>())
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

        return mockSet;
    }
}
