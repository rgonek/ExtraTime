using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Leagues.Commands.CreateLeague;
using ExtraTime.Domain.Entities;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Leagues.Handlers;

public sealed class CreateLeagueCommandHandlerTests : HandlerTestBase
{
    private readonly IInviteCodeGenerator _inviteCodeGenerator;
    private readonly CreateLeagueCommandHandler _handler;

    public CreateLeagueCommandHandlerTests()
    {
        _inviteCodeGenerator = Substitute.For<IInviteCodeGenerator>();
        _handler = new CreateLeagueCommandHandler(Context, CurrentUserService, _inviteCodeGenerator);
    }

    [Test]
    public async Task Handle_ValidCommand_CreatesLeagueAndOwnerMembership()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).WithUsername("owner").Build();
        SetCurrentUser(userId);

        var command = new CreateLeagueCommand(
            "New League", "Desc", false, 20, 3, 1, 5, null, null);

        var mockUsers = CreateMockDbSet(new List<User> { user }.AsQueryable());
        Context.Users.Returns(mockUsers);
        
        var mockLeagues = CreateMockDbSet(new List<League>().AsQueryable());
        Context.Leagues.Returns(mockLeagues);
        
        var mockMembers = CreateMockDbSet(new List<LeagueMember>().AsQueryable());
        Context.LeagueMembers.Returns(mockMembers);

        _inviteCodeGenerator.GenerateUniqueAsync(Arg.Any<Func<string, CancellationToken, Task<bool>>>(), CancellationToken)
            .Returns("INVITE123");

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Name).IsEqualTo("New League");
        await Assert.That(result.Value.InviteCode).IsEqualTo("INVITE123");
        
        Context.Leagues.Received(1).Add(Arg.Is<League>(l => l.Name == "New League" && l.OwnerId == userId));
        Context.LeagueMembers.Received(1).Add(Arg.Is<LeagueMember>(m => m.UserId == userId && m.Role == ExtraTime.Domain.Enums.MemberRole.Owner));
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }
}
