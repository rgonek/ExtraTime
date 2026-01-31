using ExtraTime.Application.Features.Leagues.Commands.JoinLeague;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Leagues;

[TestCategory(TestCategories.Significant)]
public sealed class JoinLeagueCommandIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task JoinLeague_ValidInviteCode_AddsMemberToLeague()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode("INVITE123")
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var newUserId = Guid.NewGuid();
        var newUser = new UserBuilder().WithId(newUserId).Build();
        Context.Users.Add(newUser);
        await Context.SaveChangesAsync();

        SetCurrentUser(newUserId);

        var handler = new JoinLeagueCommandHandler(Context, CurrentUserService);
        var command = new JoinLeagueCommand(league.Id, "INVITE123");

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var membership = await Context.LeagueMembers
            .FirstOrDefaultAsync(m => m.LeagueId == league.Id && m.UserId == newUserId);

        await Assert.That(membership).IsNotNull();
        await Assert.That(membership!.Role).IsEqualTo(MemberRole.Member);
    }

    [Test]
    public async Task JoinLeague_InvalidInviteCode_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode("INVITE123")
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var newUserId = Guid.NewGuid();
        var newUser = new UserBuilder().WithId(newUserId).Build();
        Context.Users.Add(newUser);
        await Context.SaveChangesAsync();

        SetCurrentUser(newUserId);

        var handler = new JoinLeagueCommandHandler(Context, CurrentUserService);
        var command = new JoinLeagueCommand(league.Id, "WRONGCODE");

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task JoinLeague_ExpiredInviteCode_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode("INVITE123")
            .Build();
        league.RegenerateInviteCode("INVITE123", DateTime.UtcNow.AddDays(-1)); // Expired yesterday
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var newUserId = Guid.NewGuid();
        var newUser = new UserBuilder().WithId(newUserId).Build();
        Context.Users.Add(newUser);
        await Context.SaveChangesAsync();

        SetCurrentUser(newUserId);

        var handler = new JoinLeagueCommandHandler(Context, CurrentUserService);
        var command = new JoinLeagueCommand(league.Id, "INVITE123");

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task JoinLeague_CaseInsensitiveInviteCode_ReturnsSuccess()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode("InviteCode123")
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var newUserId = Guid.NewGuid();
        var newUser = new UserBuilder().WithId(newUserId).Build();
        Context.Users.Add(newUser);
        await Context.SaveChangesAsync();

        SetCurrentUser(newUserId);

        var handler = new JoinLeagueCommandHandler(Context, CurrentUserService);
        var command = new JoinLeagueCommand(league.Id, "invitEcode123"); // Different case

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
    }

    [Test]
    public async Task JoinLeague_AlreadyMember_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode("INVITE123")
            .Build();
        Context.Leagues.Add(league);

        var existingMemberId = Guid.NewGuid();
        var existingMember = new UserBuilder().WithId(existingMemberId).Build();
        Context.Users.Add(existingMember);
        await Context.SaveChangesAsync();

        // Add as member first
        league.AddMember(existingMemberId, MemberRole.Member);
        await Context.SaveChangesAsync();

        SetCurrentUser(existingMemberId);

        var handler = new JoinLeagueCommandHandler(Context, CurrentUserService);
        var command = new JoinLeagueCommand(league.Id, "INVITE123");

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
