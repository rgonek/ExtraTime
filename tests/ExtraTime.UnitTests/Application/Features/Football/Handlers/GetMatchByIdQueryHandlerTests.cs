using ExtraTime.Application.Features.Football.Queries.GetMatchById;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Football.Handlers;

public sealed class GetMatchByIdQueryHandlerTests : HandlerTestBase
{
    private readonly GetMatchByIdQueryHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public GetMatchByIdQueryHandlerTests()
    {
        _handler = new GetMatchByIdQueryHandler(Context);
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
    public async Task Handle_MatchNotFound_ReturnsFailure()
    {
        // Arrange
        var mockMatches = CreateMockDbSet(new List<Domain.Entities.Match>().AsQueryable());
        Context.Matches.Returns(mockMatches);

        var query = new GetMatchByIdQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
