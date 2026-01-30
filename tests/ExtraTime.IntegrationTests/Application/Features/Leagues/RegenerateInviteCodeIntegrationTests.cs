using ExtraTime.Application.Features.Leagues.Commands.RegenerateInviteCode;
using ExtraTime.Domain.Entities;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Application.Features.Leagues;

public sealed class RegenerateInviteCodeCommandIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task RegenerateInviteCode_ValidRequest_GeneratesNewCode()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode("OLD123")
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var inviteCodeGenerator = Substitute.For<ExtraTime.Application.Common.Interfaces.IInviteCodeGenerator>();
        inviteCodeGenerator.GenerateUniqueAsync(Arg.Any<Func<string, CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns("NEW456");

        var handler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, inviteCodeGenerator);
        var command = new RegenerateInviteCodeCommand(league.Id, null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value.InviteCode).IsEqualTo("NEW456");
        await Assert.That(result.Value.InviteCodeExpiresAt).IsNull();

        // Verify in database
        var updatedLeague = await Context.Leagues.FirstOrDefaultAsync(l => l.Id == league.Id);
        await Assert.That(updatedLeague).IsNotNull();
        await Assert.That(updatedLeague!.InviteCode).IsEqualTo("NEW456");
    }

    [Test]
    public async Task RegenerateInviteCode_WithExpiration_SetsExpiresAt()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode("OLD123")
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var expiresAt = DateTime.UtcNow.AddDays(7);

        var inviteCodeGenerator = Substitute.For<ExtraTime.Application.Common.Interfaces.IInviteCodeGenerator>();
        inviteCodeGenerator.GenerateUniqueAsync(Arg.Any<Func<string, CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns("NEW456");

        var handler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, inviteCodeGenerator);
        var command = new RegenerateInviteCodeCommand(league.Id, expiresAt);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.InviteCodeExpiresAt).IsEqualTo(expiresAt);

        // Verify in database
        var updatedLeague = await Context.Leagues.FirstOrDefaultAsync(l => l.Id == league.Id);
        await Assert.That(updatedLeague!.InviteCodeExpiresAt).IsEqualTo(expiresAt);
    }

    [Test]
    public async Task RegenerateInviteCode_LeagueNotFound_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var inviteCodeGenerator = Substitute.For<ExtraTime.Application.Common.Interfaces.IInviteCodeGenerator>();
        var handler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, inviteCodeGenerator);
        var command = new RegenerateInviteCodeCommand(Guid.NewGuid(), null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).Contains("League not found");
    }

    [Test]
    public async Task RegenerateInviteCode_NotOwner_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder().WithId(otherUserId).Build();
        Context.Users.Add(otherUser);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode("OLD123")
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        SetCurrentUser(otherUserId); // Not the owner

        var inviteCodeGenerator = Substitute.For<ExtraTime.Application.Common.Interfaces.IInviteCodeGenerator>();
        var handler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, inviteCodeGenerator);
        var command = new RegenerateInviteCodeCommand(league.Id, null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).Contains("Only the league owner");
    }

    [Test]
    public async Task RegenerateInviteCode_GeneratesUniqueCode_ViaGenerator()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithInviteCode("OLD123")
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var inviteCodeGenerator = Substitute.For<ExtraTime.Application.Common.Interfaces.IInviteCodeGenerator>();
        inviteCodeGenerator.GenerateUniqueAsync(Arg.Any<Func<string, CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns("UNIQUE789");

        var handler = new RegenerateInviteCodeCommandHandler(Context, CurrentUserService, inviteCodeGenerator);
        var command = new RegenerateInviteCodeCommand(league.Id, null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        // Verify the generator was called with a uniqueness check function
        await inviteCodeGenerator.Received(1)
            .GenerateUniqueAsync(Arg.Any<Func<string, CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>());
    }
}
