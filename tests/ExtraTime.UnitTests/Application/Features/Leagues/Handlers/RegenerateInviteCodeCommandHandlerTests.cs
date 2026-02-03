using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Leagues.Commands.RegenerateInviteCode;
using ExtraTime.Domain.Common;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TUnit.Core;

namespace ExtraTime.UnitTests.Application.Features.Leagues.Handlers;

[NotInParallel]
public sealed class RegenerateInviteCodeCommandHandlerTests : HandlerTestBase
{
    private readonly RegenerateInviteCodeCommandHandler _handler;
    private readonly IInviteCodeGenerator _inviteCodeGenerator;
    private readonly DateTime _now = new(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

    public RegenerateInviteCodeCommandHandlerTests()
    {
        _inviteCodeGenerator = Substitute.For<IInviteCodeGenerator>();
        _handler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, _inviteCodeGenerator);
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
    public async Task Handle_LeagueNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetCurrentUser(userId);

        var mockLeagues = CreateMockDbSet(new List<Domain.Entities.League>().AsQueryable());
        Context.Leagues.Returns(mockLeagues);

        var command = new RegenerateInviteCodeCommand(Guid.NewGuid(), null);

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

        var command = new RegenerateInviteCodeCommand(leagueId, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
