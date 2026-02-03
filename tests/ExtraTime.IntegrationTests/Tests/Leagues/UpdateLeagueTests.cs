using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Leagues;
using ExtraTime.Application.Features.Leagues.Commands.UpdateLeague;
using ExtraTime.Application.Features.Leagues.DTOs;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Services;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Tests.Leagues;

public sealed class UpdateLeagueTests : IntegrationTestBase
{
    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task UpdateLeague_ValidData_UpdatesLeague()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithName("Original Name")
            .WithDescription("Original Description")
            .WithMaxMembers(50)
            .WithScoringRules(3, 1)
            .WithBettingDeadlineMinutes(10)
            .Build();
        Context.Leagues.Add(league);

        await Context.SaveChangesAsync();
        SetCurrentUser(userId);

        var handler = new UpdateLeagueCommandHandler(Context, CurrentUserService);

        var command = new UpdateLeagueCommand(
            LeagueId: league.Id,
            Name: "Updated League Name",
            Description: "Updated Description",
            IsPublic: true,
            MaxMembers: 100,
            ScoreExactMatch: 5,
            ScoreCorrectResult: 2,
            BettingDeadlineMinutes: 15,
            AllowedCompetitionIds: null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value.Name).IsEqualTo("Updated League Name");
        await Assert.That(result.Value.Description).IsEqualTo("Updated Description");
        await Assert.That(result.Value.IsPublic).IsTrue();
        await Assert.That(result.Value.MaxMembers).IsEqualTo(100);
        await Assert.That(result.Value.ScoreExactMatch).IsEqualTo(5);
        await Assert.That(result.Value.ScoreCorrectResult).IsEqualTo(2);
        await Assert.That(result.Value.BettingDeadlineMinutes).IsEqualTo(15);

        // Verify persistence
        var updatedLeague = await Context.Leagues.FindAsync(league.Id);
        await Assert.That(updatedLeague).IsNotNull();
        await Assert.That(updatedLeague!.Name).IsEqualTo("Updated League Name");
        await Assert.That(updatedLeague.Description).IsEqualTo("Updated Description");
        await Assert.That(updatedLeague.IsPublic).IsTrue();
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task UpdateLeague_NotOwner_ReturnsForbidden()
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
            .WithName("League Name")
            .Build();
        Context.Leagues.Add(league);

        await Context.SaveChangesAsync();
        SetCurrentUser(otherUserId); // Set as non-owner

        var handler = new UpdateLeagueCommandHandler(Context, CurrentUserService);

        var command = new UpdateLeagueCommand(
            LeagueId: league.Id,
            Name: "Attempted Update",
            Description: null,
            IsPublic: false,
            MaxMembers: 50,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(LeagueErrors.NotTheOwner);

        // Verify league was not modified
        var unchangedLeague = await Context.Leagues.FindAsync(league.Id);
        await Assert.That(unchangedLeague!.Name).IsEqualTo("League Name");
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task UpdateLeague_InvalidName_ReturnsValidationError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(userId)
            .WithName("Valid Name")
            .Build();
        Context.Leagues.Add(league);

        await Context.SaveChangesAsync();
        SetCurrentUser(userId);

        var handler = new UpdateLeagueCommandHandler(Context, CurrentUserService);

        // Test with empty name
        var command = new UpdateLeagueCommand(
            LeagueId: league.Id,
            Name: "", // Invalid - empty name
            Description: null,
            IsPublic: false,
            MaxMembers: 50,
            ScoreExactMatch: 3,
            ScoreCorrectResult: 1,
            BettingDeadlineMinutes: 5,
            AllowedCompetitionIds: null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();

        // Verify league was not modified
        var unchangedLeague = await Context.Leagues.FindAsync(league.Id);
        await Assert.That(unchangedLeague!.Name).IsEqualTo("Valid Name");
    }
}
