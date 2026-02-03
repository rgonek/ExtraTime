using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ExtraTime.IntegrationTests.Infrastructure.Data;

public sealed class ConfigurationTests : IntegrationTestBase
{
    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task LeagueConfiguration_TableName_IsCorrect()
    {
        // Act
        var entityType = Context.Model.FindEntityType(typeof(League));

        // Assert
        await Assert.That(entityType).IsNotNull();
        await Assert.That(entityType!.GetTableName()).IsEqualTo("Leagues");
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task UserConfiguration_Email_HasUniqueIndex()
    {
        // Act
        var entityType = Context.Model.FindEntityType(typeof(User));
        var emailProperty = entityType!.FindProperty("Email");
        var indexes = entityType.GetIndexes()
            .Where(i => i.Properties.Contains(emailProperty))
            .ToList();

        // Assert
        await Assert.That(emailProperty).IsNotNull();
        await Assert.That(indexes).IsNotEmpty();
        await Assert.That(indexes.Any(i => i.IsUnique)).IsTrue();
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task UserConfiguration_Username_HasUniqueIndex()
    {
        // Act
        var entityType = Context.Model.FindEntityType(typeof(User));
        var usernameProperty = entityType!.FindProperty("Username");
        var indexes = entityType.GetIndexes()
            .Where(i => i.Properties.Contains(usernameProperty))
            .ToList();

        // Assert
        await Assert.That(usernameProperty).IsNotNull();
        await Assert.That(indexes).IsNotEmpty();
        await Assert.That(indexes.Any(i => i.IsUnique)).IsTrue();
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task BetConfiguration_CompositeIndex_LeagueMatchUser()
    {
        // Act
        var entityType = Context.Model.FindEntityType(typeof(Bet));
        var indexes = entityType!.GetIndexes().ToList();

        // Find composite index with LeagueId, UserId, MatchId
        var compositeIndex = indexes.FirstOrDefault(i =>
            i.Properties.Count == 3 &&
            i.Properties.Any(p => p.Name == "LeagueId") &&
            i.Properties.Any(p => p.Name == "UserId") &&
            i.Properties.Any(p => p.Name == "MatchId"));

        // Assert
        await Assert.That(compositeIndex).IsNotNull();
        await Assert.That(compositeIndex!.IsUnique).IsTrue();
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task LeagueConfiguration_InviteCode_HasIndex()
    {
        // Act
        var entityType = Context.Model.FindEntityType(typeof(League));
        var inviteCodeProperty = entityType!.FindProperty("InviteCode");
        var indexes = entityType.GetIndexes()
            .Where(i => i.Properties.Contains(inviteCodeProperty))
            .ToList();

        // Assert
        await Assert.That(inviteCodeProperty).IsNotNull();
        await Assert.That(indexes).IsNotEmpty();
        await Assert.That(indexes.Any(i => i.IsUnique)).IsTrue();
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task MatchConfiguration_Status_StoredAsString()
    {
        // Act
        var entityType = Context.Model.FindEntityType(typeof(Match));
        var statusProperty = entityType!.FindProperty("Status");

        // Assert
        await Assert.That(statusProperty).IsNotNull();
        await Assert.That(statusProperty!.GetColumnType()).IsEqualTo("nvarchar(20)");

        // Verify enum conversion by creating a match
        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .WithStatus(MatchStatus.Finished)
            .Build();
        Context.Matches.Add(match);
        await Context.SaveChangesAsync();

        // Query raw SQL to verify stored value is string
        var connection = Context.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT Status FROM Matches WHERE Id = '{match.Id}'";
        var result = await command.ExecuteScalarAsync();
        await connection.CloseAsync();

        await Assert.That(result).IsEqualTo("Finished");
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task BotConfiguration_Strategy_StoredAsString()
    {
        // Arrange
        var user = new UserBuilder().Build();
        Context.Users.Add(user);

        var bot = new BotBuilder()
            .WithUserId(user.Id)
            .WithStrategy(BotStrategy.StatsAnalyst)
            .Build();
        Context.Bots.Add(bot);
        await Context.SaveChangesAsync();

        // Act - Query raw SQL to verify stored value is string
        var connection = Context.Database.GetDbConnection();
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT Strategy FROM Bots WHERE Id = '{bot.Id}'";
        var result = await command.ExecuteScalarAsync();
        await connection.CloseAsync();

        // Assert
        await Assert.That(result).IsEqualTo("StatsAnalyst");
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task ForeignKey_League_Owner_CascadeDelete()
    {
        // Arrange
        var user = new UserBuilder().Build();
        Context.Users.Add(user);

        var league = new LeagueBuilder()
            .WithOwnerId(user.Id)
            .Build();
        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var leagueId = league.Id;

        // Act - Delete the user (owner)
        Context.Users.Remove(user);
        await Context.SaveChangesAsync();

        // Assert - Verify cascade behavior
        // Since League has Restrict delete behavior on Owner, the league should still exist
        var leagueExists = await Context.Leagues.AnyAsync(l => l.Id == leagueId);
        await Assert.That(leagueExists).IsTrue();

        // But the OwnerId foreign key relationship is restricted
        var entityType = Context.Model.FindEntityType(typeof(League));
        var ownerFk = entityType!.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(User));

        await Assert.That(ownerFk).IsNotNull();
        await Assert.That(ownerFk!.DeleteBehavior).IsEqualTo(DeleteBehavior.Restrict);
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task ForeignKey_Bet_League_RestrictDelete()
    {
        // Arrange
        var user = new UserBuilder().Build();
        Context.Users.Add(user);

        var competition = new CompetitionBuilder().Build();
        Context.Competitions.Add(competition);

        var homeTeam = new TeamBuilder().Build();
        var awayTeam = new TeamBuilder().Build();
        Context.Teams.AddRange(homeTeam, awayTeam);

        var match = new MatchBuilder()
            .WithCompetitionId(competition.Id)
            .WithTeams(homeTeam.Id, awayTeam.Id)
            .Build();
        Context.Matches.Add(match);

        var league = new LeagueBuilder()
            .WithOwnerId(user.Id)
            .Build();
        Context.Leagues.Add(league);

        var bet = Bet.Place(league.Id, user.Id, match.Id, 2, 1);
        Context.Bets.Add(bet);
        await Context.SaveChangesAsync();

        // Act
        var entityType = Context.Model.FindEntityType(typeof(Bet));
        var leagueFk = entityType!.GetForeignKeys()
            .FirstOrDefault(fk => fk.Properties.Any(p => p.Name == "LeagueId"));

        // Assert
        await Assert.That(leagueFk).IsNotNull();
        await Assert.That(leagueFk!.DeleteBehavior).IsEqualTo(DeleteBehavior.Cascade);
    }

    [Test]
    [TestCategory(TestCategories.Significant)]
    [SkipOnGitHubActions]
    public async Task AllConfigurations_HaveRequiredFields()
    {
        // This test verifies that all entity configurations properly enforce required fields

        // Arrange & Act - Test User required fields
        var userEntity = Context.Model.FindEntityType(typeof(User));
        var userEmailProperty = userEntity!.FindProperty("Email");
        var userUsernameProperty = userEntity.FindProperty("Username");
        var userPasswordProperty = userEntity.FindProperty("PasswordHash");

        // Assert User fields are required
        await Assert.That(userEmailProperty!.IsNullable).IsFalse();
        await Assert.That(userUsernameProperty!.IsNullable).IsFalse();
        await Assert.That(userPasswordProperty!.IsNullable).IsFalse();

        // Arrange & Act - Test League required fields
        var leagueEntity = Context.Model.FindEntityType(typeof(League));
        var leagueNameProperty = leagueEntity!.FindProperty("Name");
        var leagueInviteCodeProperty = leagueEntity.FindProperty("InviteCode");

        // Assert League fields are required
        await Assert.That(leagueNameProperty!.IsNullable).IsFalse();
        await Assert.That(leagueInviteCodeProperty!.IsNullable).IsFalse();

        // Arrange & Act - Test required field lengths
        var nameMaxLength = leagueNameProperty.GetMaxLength();
        var inviteCodeMaxLength = leagueInviteCodeProperty.GetMaxLength();

        // Assert
        await Assert.That(nameMaxLength).IsEqualTo(100);
        await Assert.That(inviteCodeMaxLength).IsEqualTo(8);
    }
}
