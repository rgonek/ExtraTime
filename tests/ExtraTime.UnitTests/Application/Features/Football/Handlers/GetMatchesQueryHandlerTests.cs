using ExtraTime.Application.Features.Football.Queries.GetMatches;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TUnit.Core;

namespace ExtraTime.UnitTests.Application.Features.Football.Handlers;

[NotInParallel]
public sealed class GetMatchesQueryHandlerTests : HandlerTestBase
{
    private readonly GetMatchesQueryHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public GetMatchesQueryHandlerTests()
    {
        _handler = new GetMatchesQueryHandler(Context);
    }

    [Before(Test)]
    public void Setup()
    {
        Clock.Current = new FakeClock(_now);
    }

    [After(Test)]
    public void Cleanup()
    {
        Clock.Current = null!;
    }

    [Test]
    public async Task Handle_NoMatches_ReturnsEmptyPagedResponse()
    {
        // Arrange
        var mockMatches = CreateMockDbSet(new List<Domain.Entities.Match>().AsQueryable());
        Context.Matches.Returns(mockMatches);

        var query = new GetMatchesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Items.Count).IsEqualTo(0);
        await Assert.That(result.Value!.TotalCount).IsEqualTo(0);
        await Assert.That(result.Value!.Page).IsEqualTo(1);
    }

}
