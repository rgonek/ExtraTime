using ExtraTime.Application.Features.Leagues.Commands.UpdateLeague;
using ExtraTime.Domain.Entities;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Leagues;

public sealed class UpdateLeagueCommandIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task UpdateLeague_ValidData_UpdatesLeagueSettings()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithName("Original Name")
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new UpdateLeagueCommandHandler(Context, CurrentUserService);
        var command = new UpdateLeagueCommand(
            league.Id,
            "Updated Name",
            "Updated Description",
            true,
            20,
            5,
            2,
            10,
            null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Name).IsEqualTo("Updated Name");

        var updatedLeague = await Context.Leagues.FindAsync(league.Id);
        await Assert.That(updatedLeague!.Name).IsEqualTo("Updated Name");
        await Assert.That(updatedLeague.Description).IsEqualTo("Updated Description");
        await Assert.That(updatedLeague.IsPublic).IsTrue();
        await Assert.That(updatedLeague.MaxMembers).IsEqualTo(20);
        await Assert.That(updatedLeague.ScoreExactMatch).IsEqualTo(5);
        await Assert.That(updatedLeague.ScoreCorrectResult).IsEqualTo(2);
        await Assert.That(updatedLeague.BettingDeadlineMinutes).IsEqualTo(10);
    }

    [Test]
    public async Task UpdateLeague_NotOwner_ReturnsFailure()
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
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        SetCurrentUser(otherUserId); // Not the owner

        var handler = new UpdateLeagueCommandHandler(Context, CurrentUserService);
        var command = new UpdateLeagueCommand(
            league.Id,
            "Updated Name",
            null,
            false,
            50,
            3,
            1,
            5,
            null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }

    [Test]
    public async Task UpdateLeague_WithCompetitionFilter_UpdatesAllowedCompetitions()
    {
        // Arrange
        var competition1 = new CompetitionBuilder().Build();
        var competition2 = new CompetitionBuilder().Build();
        Context.Competitions.AddRange(competition1, competition2);

        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new UpdateLeagueCommandHandler(Context, CurrentUserService);
        var command = new UpdateLeagueCommand(
            league.Id,
            league.Name,
            null,
            false,
            50,
            3,
            1,
            5,
            new[] { competition1.Id, competition2.Id });

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var updatedLeague = await Context.Leagues.FindAsync(league.Id);
        var allowedIds = updatedLeague!.AllowedCompetitionIds?.Split(',').Select(Guid.Parse).ToList();
        await Assert.That(allowedIds).IsNotNull();
        await Assert.That(allowedIds!.Count).IsEqualTo(2);
    }

    [Test]
    public async Task UpdateLeague_InvalidCompetitionId_ReturnsFailure()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();
        Context.Users.Add(owner);

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new UpdateLeagueCommandHandler(Context, CurrentUserService);
        var command = new UpdateLeagueCommand(
            league.Id,
            league.Name,
            null,
            false,
            50,
            3,
            1,
            5,
            new[] { Guid.NewGuid() }); // Non-existent competition

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
    }
}
