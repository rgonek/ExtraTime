using ExtraTime.Application.Features.Leagues.Commands.CreateLeague;
using ExtraTime.Domain.Entities;
using ExtraTime.Infrastructure.Services;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Leagues;

[TestCategory(TestCategories.Significant)]
public sealed class CreateLeagueCommandIntegrationTests : IntegrationTestBase
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
        ExtraTime.Application.Common.Result<ExtraTime.Application.Features.Leagues.DTOs.LeagueDto> result;
        try 
        {
            result = await handler.Handle(command, default);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }

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
