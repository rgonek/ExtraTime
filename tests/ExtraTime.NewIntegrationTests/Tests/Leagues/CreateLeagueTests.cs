using ExtraTime.Application.Features.Leagues.Commands.CreateLeague;
using ExtraTime.Infrastructure.Services;
using ExtraTime.NewIntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.NewIntegrationTests.Tests.Leagues;

public sealed class CreateLeagueTests : NewIntegrationTestBase
{
    [Test]
    public async Task CreateLeague_ValidData_PersistsToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var inviteCodeGenerator = new InviteCodeGenerator();
        var handler = new CreateLeagueCommandHandler(Context, CurrentUserService, inviteCodeGenerator);

        var command = new CreateLeagueCommand(
            "Integration League", 
            "Test description", 
            false, 
            50, 
            3, 
            1, 
            5, 
            null, 
            null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        
        var league = await Context.Leagues
            .FirstOrDefaultAsync(l => l.Name == "Integration League");
        
        await Assert.That(league).IsNotNull();
        await Assert.That(league!.OwnerId).IsEqualTo(userId);
        
        var membership = await Context.LeagueMembers
            .FirstOrDefaultAsync(m => m.LeagueId == league.Id && m.UserId == userId);
        
        await Assert.That(membership).IsNotNull();
        await Assert.That(membership!.Role).IsEqualTo(ExtraTime.Domain.Enums.MemberRole.Owner);
    }
}
