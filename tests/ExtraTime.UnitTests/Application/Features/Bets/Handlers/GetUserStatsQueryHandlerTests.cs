using ExtraTime.Application.Features.Bets.Queries.GetUserStats;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Bets.Handlers;

public sealed class GetUserStatsQueryHandlerTests : HandlerTestBase
{
    private readonly GetUserStatsQueryHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public GetUserStatsQueryHandlerTests()
    {
        _handler = new GetUserStatsQueryHandler(Context, CurrentUserService);
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
        var targetUserId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();

        SetCurrentUser(userId);

        var mockLeagueMembers = CreateMockDbSet(new List<Domain.Entities.LeagueMember>().AsQueryable());
        Context.LeagueMembers.Returns(mockLeagueMembers);

        var query = new GetUserStatsQuery(leagueId, targetUserId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_TargetNotAMember_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();

        var leagueMember = new Domain.Entities.LeagueMember
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            UserId = userId,
            Role = MemberRole.Member,
            JoinedAt = _now.AddDays(-7)
        };

        SetCurrentUser(userId);

        var leagueMembers = new List<Domain.Entities.LeagueMember> { leagueMember }.AsQueryable();
        var mockLeagueMembers = CreateMockDbSet(leagueMembers);
        Context.LeagueMembers.Returns(mockLeagueMembers);

        var query = new GetUserStatsQuery(leagueId, targetUserId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();

        var leagueMember = new Domain.Entities.LeagueMember
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            UserId = userId,
            Role = MemberRole.Member,
            JoinedAt = _now.AddDays(-7)
        };

        var targetMember = new Domain.Entities.LeagueMember
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            UserId = targetUserId,
            Role = MemberRole.Member,
            JoinedAt = _now.AddDays(-7)
        };

        SetCurrentUser(userId);

        var leagueMembers = new List<Domain.Entities.LeagueMember> { leagueMember, targetMember }.AsQueryable();
        var mockLeagueMembers = CreateMockDbSet(leagueMembers);
        Context.LeagueMembers.Returns(mockLeagueMembers);

        var mockUsers = CreateMockDbSet(new List<Domain.Entities.User>().AsQueryable());
        Context.Users.Returns(mockUsers);

        var query = new GetUserStatsQuery(leagueId, targetUserId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_UserWithNoStanding_ReturnsDefaultStats()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();

        var targetUser = new UserBuilder()
            .WithId(targetUserId)
            .WithUsername("targetuser")
            .Build();

        var leagueMember = new Domain.Entities.LeagueMember
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            UserId = userId,
            Role = MemberRole.Member,
            JoinedAt = _now.AddDays(-7)
        };

        var targetMember = new Domain.Entities.LeagueMember
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            UserId = targetUserId,
            Role = MemberRole.Member,
            JoinedAt = _now.AddDays(-7)
        };

        SetCurrentUser(userId);

        var leagueMembers = new List<Domain.Entities.LeagueMember> { leagueMember, targetMember }.AsQueryable();
        var mockLeagueMembers = CreateMockDbSet(leagueMembers);
        Context.LeagueMembers.Returns(mockLeagueMembers);

        var users = new List<Domain.Entities.User> { targetUser }.AsQueryable();
        var mockUsers = CreateMockDbSet(users);
        Context.Users.Returns(mockUsers);

        var mockStandings = CreateMockDbSet(new List<Domain.Entities.LeagueStanding>().AsQueryable());
        Context.LeagueStandings.Returns(mockStandings);

        var query = new GetUserStatsQuery(leagueId, targetUserId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.UserId).IsEqualTo(targetUserId);
        await Assert.That(result.Value!.Username).IsEqualTo("targetuser");
        await Assert.That(result.Value!.TotalPoints).IsEqualTo(0);
        await Assert.That(result.Value!.BetsPlaced).IsEqualTo(0);
        await Assert.That(result.Value!.Rank).IsEqualTo(1);
    }
}
