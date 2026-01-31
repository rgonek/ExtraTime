using ExtraTime.Application.Features.Bets.Queries.GetLeagueStandings;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Bets.Handlers;

public sealed class GetLeagueStandingsQueryHandlerTests : HandlerTestBase
{
    private readonly GetLeagueStandingsQueryHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public GetLeagueStandingsQueryHandlerTests()
    {
        _handler = new GetLeagueStandingsQueryHandler(Context, CurrentUserService);
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
    public async Task Handle_MemberWithStandings_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();

        var currentUser = new UserBuilder()
            .WithId(userId)
            .WithUsername("currentuser")
            .WithEmail("current@example.com")
            .Build();

        var otherUser = new UserBuilder()
            .WithId(otherUserId)
            .WithUsername("otheruser")
            .WithEmail("other@example.com")
            .Build();

        var leagueMember1 = new LeagueMemberBuilder()
            .WithLeagueId(leagueId)
            .WithUserId(userId)
            .WithRole(MemberRole.Member)
            .WithJoinedAt(_now.AddDays(-7))
            .Build();

        var leagueMember2 = new LeagueMemberBuilder()
            .WithLeagueId(leagueId)
            .WithUserId(otherUserId)
            .WithRole(MemberRole.Member)
            .WithJoinedAt(_now.AddDays(-7))
            .Build();

        var standing1 = Domain.Entities.LeagueStanding.Create(leagueId, userId);
        standing1.TotalPoints = 15;
        standing1.BetsPlaced = 5;
        standing1.ExactMatches = 2;
        standing1.CorrectResults = 3;
        standing1.CurrentStreak = 2;
        standing1.BestStreak = 3;

        var standing2 = Domain.Entities.LeagueStanding.Create(leagueId, otherUserId);
        standing2.TotalPoints = 10;
        standing2.BetsPlaced = 5;
        standing2.ExactMatches = 1;
        standing2.CorrectResults = 2;
        standing2.CurrentStreak = 1;
        standing2.BestStreak = 2;

        SetCurrentUser(userId);

        var leagueMembers = new List<Domain.Entities.LeagueMember> { leagueMember1, leagueMember2 }.AsQueryable();
        var mockLeagueMembers = CreateMockDbSet(leagueMembers);
        Context.LeagueMembers.Returns(mockLeagueMembers);

        var standings = new List<Domain.Entities.LeagueStanding> { standing1, standing2 }.AsQueryable();
        var mockStandings = CreateMockDbSet(standings);
        Context.LeagueStandings.Returns(mockStandings);

        var users = new List<Domain.Entities.User> { currentUser, otherUser }.AsQueryable();
        var mockUsers = CreateMockDbSet(users);
        Context.Users.Returns(mockUsers);

        var query = new GetLeagueStandingsQuery(leagueId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(2);
        // First place should be the user with 15 points
        await Assert.That(result.Value![0].TotalPoints).IsEqualTo(15);
        await Assert.That(result.Value![0].Rank).IsEqualTo(1);
        // Second place should be the user with 10 points
        await Assert.That(result.Value![1].TotalPoints).IsEqualTo(10);
        await Assert.That(result.Value![1].Rank).IsEqualTo(2);
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

        var query = new GetLeagueStandingsQuery(leagueId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_NoStandings_ReturnsEmptyList()
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

        var mockStandings = CreateMockDbSet(new List<Domain.Entities.LeagueStanding>().AsQueryable());
        Context.LeagueStandings.Returns(mockStandings);

        var mockUsers = CreateMockDbSet(new List<Domain.Entities.User>().AsQueryable());
        Context.Users.Returns(mockUsers);

        var query = new GetLeagueStandingsQuery(leagueId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }
}
