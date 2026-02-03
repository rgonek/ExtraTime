using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Leagues.Commands.CreateLeague;
using ExtraTime.Application.Features.Leagues.Commands.DeleteLeague;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Tests.Leagues;

public sealed class DeleteLeagueTests : IntegrationTestBase
{
    private async Task<Guid> CreateLeagueAsync(Guid ownerId)
    {
        var generator = Substitute.For<IInviteCodeGenerator>();
        generator.GenerateUniqueAsync(Arg.Any<Func<string, CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns("CODE1234");

        SetCurrentUser(ownerId);
        var handler = new CreateLeagueCommandHandler(Context, CurrentUserService, generator);
        
        var command = new CreateLeagueCommand(
            "Test League", 
            null, 
            false, 
            10, 
            3, 
            1, 
            5, 
            null, 
            null);

        var result = await handler.Handle(command, default);
        
        // Clear change tracker to ensure subsequent operations simulate a fresh request
        Context.ChangeTracker.Clear();
        
        return result.Value.Id;
    }

    [Test]
    public async Task DeleteLeague_AsOwner_DeletesLeagueAndMembers()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        var leagueId = await CreateLeagueAsync(ownerId);

        var memberId = Guid.NewGuid();
        var member = new UserBuilder().WithId(memberId).Build();
        Context.Users.Add(member);
        await Context.SaveChangesAsync();

        // Add a member
        var league = await Context.Leagues
            .Include(l => l.Members)
            .FirstAsync(l => l.Id == leagueId);
        league.AddMember(memberId, MemberRole.Member);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new DeleteLeagueCommandHandler(Context, CurrentUserService);
        var command = new DeleteLeagueCommand(leagueId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var deletedLeague = await Context.Leagues.FindAsync(leagueId);
        await Assert.That(deletedLeague).IsNull();

        var memberships = await Context.LeagueMembers
            .Where(m => m.LeagueId == leagueId)
            .ToListAsync();
        await Assert.That(memberships).IsEmpty();
    }

    [Test]
    public async Task DeleteLeague_NotOwner_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        var leagueId = await CreateLeagueAsync(ownerId);

        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder().WithId(otherUserId).Build();
        Context.Users.Add(otherUser);
        await Context.SaveChangesAsync();

        SetCurrentUser(otherUserId);

        var handler = new DeleteLeagueCommandHandler(Context, CurrentUserService);
        var command = new DeleteLeagueCommand(leagueId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();

        // Verify league still exists
        var existingLeague = await Context.Leagues.FindAsync(leagueId);
        await Assert.That(existingLeague).IsNotNull();
    }

    [Test]
    public async Task DeleteLeague_NonExistent_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new DeleteLeagueCommandHandler(Context, CurrentUserService);
        var command = new DeleteLeagueCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
