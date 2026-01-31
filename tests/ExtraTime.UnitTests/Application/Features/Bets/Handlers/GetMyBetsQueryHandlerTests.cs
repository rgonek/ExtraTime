using ExtraTime.Application.Features.Bets.Queries.GetMyBets;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Bets.Handlers;

public sealed class GetMyBetsQueryHandlerTests : HandlerTestBase
{
    private readonly GetMyBetsQueryHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public GetMyBetsQueryHandlerTests()
    {
        _handler = new GetMyBetsQueryHandler(Context, CurrentUserService);
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
    public async Task Handle_NotAMember_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();

        SetCurrentUser(userId);

        var mockLeagueMembers = CreateMockDbSet(new List<Domain.Entities.LeagueMember>().AsQueryable());
        Context.LeagueMembers.Returns(mockLeagueMembers);

        var query = new GetMyBetsQuery(leagueId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_MemberWithNoBets_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();

        var leagueMember = new LeagueMemberBuilder()
            .WithLeagueId(leagueId)
            .WithUserId(userId)
            .WithRole(MemberRole.Member)
            .WithJoinedAt(_now.AddDays(-7))
            .Build();

        SetCurrentUser(userId);

        var leagueMembers = new List<Domain.Entities.LeagueMember> { leagueMember }.AsQueryable();
        var mockLeagueMembers = CreateMockDbSet(leagueMembers);
        Context.LeagueMembers.Returns(mockLeagueMembers);

        var mockBets = CreateMockDbSet(new List<Domain.Entities.Bet>().AsQueryable());
        Context.Bets.Returns(mockBets);

        var query = new GetMyBetsQuery(leagueId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }
}
