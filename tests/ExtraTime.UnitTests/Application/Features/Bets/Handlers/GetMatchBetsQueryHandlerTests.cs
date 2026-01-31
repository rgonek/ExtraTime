using ExtraTime.Application.Features.Bets.Queries.GetMatchBets;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Bets.Handlers;

public sealed class GetMatchBetsQueryHandlerTests : HandlerTestBase
{
    private readonly GetMatchBetsQueryHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public GetMatchBetsQueryHandlerTests()
    {
        _handler = new GetMatchBetsQueryHandler(Context, CurrentUserService);
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
        var matchId = Guid.NewGuid();

        SetCurrentUser(userId);

        var mockLeagueMembers = CreateMockDbSet(new List<Domain.Entities.LeagueMember>().AsQueryable());
        Context.LeagueMembers.Returns(mockLeagueMembers);

        var query = new GetMatchBetsQuery(leagueId, matchId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_LeagueNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var matchId = Guid.NewGuid();

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

        var mockLeagues = CreateMockDbSet(new List<Domain.Entities.League>().AsQueryable());
        Context.Leagues.Returns(mockLeagues);

        var query = new GetMatchBetsQuery(leagueId, matchId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_MatchNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        var league = new LeagueBuilder().WithId(leagueId).Build();

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

        var leagues = new List<Domain.Entities.League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var mockMatches = CreateMockDbSet(new List<Domain.Entities.Match>().AsQueryable());
        Context.Matches.Returns(mockMatches);

        var query = new GetMatchBetsQuery(leagueId, matchId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

}
