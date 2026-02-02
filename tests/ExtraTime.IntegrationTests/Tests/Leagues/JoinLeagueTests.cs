using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Leagues.Commands.CreateLeague;
using ExtraTime.Application.Features.Leagues.Commands.JoinLeague;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Tests.Leagues;

public sealed class JoinLeagueTests : IntegrationTestBase
{
    private async Task<Guid> CreateLeagueAsync(Guid ownerId, string inviteCode)
    {
        var generator = Substitute.For<IInviteCodeGenerator>();
        generator.GenerateUniqueAsync(Arg.Any<Func<string, CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns(inviteCode);

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
        // and to avoid potential state issues with InMemory provider
        Context.ChangeTracker.Clear();
        
        return result.Value.Id;
    }

    [Test]
    public async Task JoinLeague_ValidInviteCode_AddsMemberToLeague()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        var leagueId = await CreateLeagueAsync(ownerId, "CODE1234");

        var newUserId = Guid.NewGuid();
        var newUser = new UserBuilder().WithId(newUserId).Build();
        Context.Users.Add(newUser);
        await Context.SaveChangesAsync();

        SetCurrentUser(newUserId);

        var handler = new JoinLeagueCommandHandler(Context, CurrentUserService);
        var command = new JoinLeagueCommand(leagueId, "CODE1234");

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var membership = await Context.LeagueMembers
            .FirstOrDefaultAsync(m => m.LeagueId == leagueId && m.UserId == newUserId);

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
        await Context.SaveChangesAsync();

        var leagueId = await CreateLeagueAsync(ownerId, "CODE1234");

        var newUserId = Guid.NewGuid();
        var newUser = new UserBuilder().WithId(newUserId).Build();
        Context.Users.Add(newUser);
        await Context.SaveChangesAsync();

        SetCurrentUser(newUserId);

        var handler = new JoinLeagueCommandHandler(Context, CurrentUserService);
        var command = new JoinLeagueCommand(leagueId, "WRONG");

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
        await Context.SaveChangesAsync();

        var leagueId = await CreateLeagueAsync(ownerId, "CODE1234");

        // Manually expire the code
        var league = await Context.Leagues.FirstAsync(l => l.Id == leagueId);
        league.RegenerateInviteCode("CODE1234", DateTime.UtcNow.AddDays(-1));
        await Context.SaveChangesAsync();

        var newUserId = Guid.NewGuid();
        var newUser = new UserBuilder().WithId(newUserId).Build();
        Context.Users.Add(newUser);
        await Context.SaveChangesAsync();

        SetCurrentUser(newUserId);

        var handler = new JoinLeagueCommandHandler(Context, CurrentUserService);
        var command = new JoinLeagueCommand(leagueId, "CODE1234");

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
        await Context.SaveChangesAsync();

        var leagueId = await CreateLeagueAsync(ownerId, "CODE1234");

        var newUserId = Guid.NewGuid();
        var newUser = new UserBuilder().WithId(newUserId).Build();
        Context.Users.Add(newUser);
        await Context.SaveChangesAsync();

        SetCurrentUser(newUserId);

        var handler = new JoinLeagueCommandHandler(Context, CurrentUserService);
        var command = new JoinLeagueCommand(leagueId, "code1234");

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
        await Context.SaveChangesAsync();

        var leagueId = await CreateLeagueAsync(ownerId, "CODE1234");

        var existingMemberId = Guid.NewGuid();
        var existingMember = new UserBuilder().WithId(existingMemberId).Build();
        Context.Users.Add(existingMember);
        await Context.SaveChangesAsync();

        // Add as member first
        var league = await Context.Leagues
            .Include(l => l.Members)
            .FirstAsync(l => l.Id == leagueId);
        league.AddMember(existingMemberId, MemberRole.Member);
        await Context.SaveChangesAsync();

        SetCurrentUser(existingMemberId);

        var handler = new JoinLeagueCommandHandler(Context, CurrentUserService);
        var command = new JoinLeagueCommand(leagueId, "CODE1234");

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
