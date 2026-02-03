using System.Data;
using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Leagues.Commands.JoinLeague;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TUnit.Core;

namespace ExtraTime.UnitTests.Application.Features.Leagues.Handlers;

[NotInParallel]
public sealed class JoinLeagueCommandHandlerTests : HandlerTestBase
{
    private readonly JoinLeagueCommandHandler _handler;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public JoinLeagueCommandHandlerTests()
    {
        _handler = new JoinLeagueCommandHandler(Context, CurrentUserService);
    }

    [Before(Test)]
    public void Setup()
    {
        Clock.Current = new FakeClock(_now);

        // Mock ExecuteInTransactionAsync to execute the operation directly
        Context.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Result>>>(), Arg.Any<IsolationLevel>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<Result>>>();
                return await operation(CancellationToken);
            });
    }

    [After(Test)]
    public void Cleanup()
    {
        Clock.Current = null!;
    }

    [Test]
    public async Task Handle_ValidInviteCode_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var inviteCode = "VALID123";
        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithInviteCode(inviteCode)
            .Build();

        SetCurrentUser(userId);

        var leagues = new List<Domain.Entities.League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var command = new JoinLeagueCommand(leagueId, inviteCode);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
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

        var command = new JoinLeagueCommand(Guid.NewGuid(), "VALID123");

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_InvalidInviteCode_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithInviteCode("VALID123")
            .Build();

        SetCurrentUser(userId);

        var leagues = new List<Domain.Entities.League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var command = new JoinLeagueCommand(leagueId, "INVALID1");

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_ExpiredInviteCode_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var inviteCode = "VALID123";
        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithInviteCode(inviteCode)
            .Build();

        // Set expired invite code
        league.RegenerateInviteCode(inviteCode, _now.AddDays(-1));

        SetCurrentUser(userId);

        var leagues = new List<Domain.Entities.League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var command = new JoinLeagueCommand(leagueId, inviteCode);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task Handle_AlreadyMember_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var leagueId = Guid.NewGuid();
        var inviteCode = "VALID123";
        var league = new LeagueBuilder()
            .WithId(leagueId)
            .WithInviteCode(inviteCode)
            .Build();

        // Add user as existing member
        league.AddMember(userId, MemberRole.Member);

        SetCurrentUser(userId);

        var leagues = new List<Domain.Entities.League> { league }.AsQueryable();
        var mockLeagues = CreateMockDbSet(leagues);
        Context.Leagues.Returns(mockLeagues);

        var command = new JoinLeagueCommand(leagueId, inviteCode);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
