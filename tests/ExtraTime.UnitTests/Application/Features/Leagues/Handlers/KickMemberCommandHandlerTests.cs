using ExtraTime.Application.Features.Leagues.Commands.KickMember;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Leagues.Handlers;

public sealed class KickMemberCommandHandlerTests : HandlerTestBase
{
    private readonly KickMemberCommandHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public KickMemberCommandHandlerTests()
    {
        _handler = new KickMemberCommandHandler(Context, CurrentUserService);
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
    public async Task Handle_OwnerKickingMember_ReturnsSuccess()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithOwnerId(ownerId)
            .Build();

        var member = new LeagueMemberBuilder()
            .WithLeagueId(leagueId)
            .WithUserId(memberId)
            .WithRole(MemberRole.Member)
            .Build();

        SetCurrentUser(ownerId);

        var leagues = new List<Domain.Entities.League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var members = new List<Domain.Entities.LeagueMember> { member }.AsQueryable();
        var mockMembers = CreateMockDbSet(members);
        Context.LeagueMembers.Returns(mockMembers);

        var command = new KickMemberCommand(leagueId, memberId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        Context.LeagueMembers.Received(1).Remove(Arg.Any<Domain.Entities.LeagueMember>());
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task Handle_LeagueNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetCurrentUser(userId);

        var mockLeagues = CreateMockDbSet(new List<Domain.Entities.League>().AsQueryable());
        Context.Leagues.Returns(mockLeagues);

        var command = new KickMemberCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_NotTheOwner_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithOwnerId(ownerId)
            .Build();

        SetCurrentUser(otherUserId);

        var leagues = new List<Domain.Entities.League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var command = new KickMemberCommand(leagueId, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_MemberNotFound_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithOwnerId(ownerId)
            .Build();

        SetCurrentUser(ownerId);

        var leagues = new List<Domain.Entities.League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var mockMembers = CreateMockDbSet(new List<Domain.Entities.LeagueMember>().AsQueryable());
        Context.LeagueMembers.Returns(mockMembers);

        var command = new KickMemberCommand(leagueId, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_CannotKickOwner_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithOwnerId(ownerId)
            .Build();

        var ownerMember = new LeagueMemberBuilder()
            .WithLeagueId(leagueId)
            .WithUserId(ownerId)
            .WithRole(MemberRole.Owner)
            .Build();

        SetCurrentUser(ownerId);

        var leagues = new List<Domain.Entities.League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var members = new List<Domain.Entities.LeagueMember> { ownerMember }.AsQueryable();
        var mockMembers = CreateMockDbSet(members);
        Context.LeagueMembers.Returns(mockMembers);

        var command = new KickMemberCommand(leagueId, ownerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
