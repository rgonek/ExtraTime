using ExtraTime.Application.Features.Leagues.Queries.GetUserLeagues;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;

namespace ExtraTime.IntegrationTests.Application.Features.Leagues;

[TestCategory(TestCategories.Significant)]
public sealed class GetUserLeaguesQueryIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task Handle_UserWithNoLeagues_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetUserLeaguesQueryHandler(Context, CurrentUserService);
        var query = new GetUserLeaguesQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_UserWithMultipleLeagues_ReturnsAllLeagues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).WithUsername("testuser").Build();
        
        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder().WithId(otherUserId).WithUsername("otheruser").Build();

        Context.Users.AddRange(user, otherUser);
        await Context.SaveChangesAsync();

        // Create multiple leagues where user is a member
        // Save them separately to ensure different CreatedAt timestamps
        var league1 = new LeagueBuilder()
            .WithName("League 1")
            .WithOwnerId(otherUserId)
            .Build();
        
        Context.Leagues.Add(league1);
        await Context.SaveChangesAsync();
        
        var league2 = new LeagueBuilder()
            .WithName("League 2")
            .WithOwnerId(userId)
            .Build();

        Context.Leagues.Add(league2);
        await Context.SaveChangesAsync();

        var league3 = new LeagueBuilder()
            .WithName("League 3")
            .WithOwnerId(otherUserId)
            .Build();

        Context.Leagues.Add(league3);
        await Context.SaveChangesAsync();

        // Now update the CreatedAt values to ensure proper ordering
        league1.CreatedAt = DateTime.UtcNow.AddDays(-2);
        league2.CreatedAt = DateTime.UtcNow.AddDays(-1);
        league3.CreatedAt = DateTime.UtcNow;
        await Context.SaveChangesAsync();

        // Add memberships for user to leagues they don't own
        // Note: league2 is owned by userId, so owner membership is already created
        // league1 and league3 are owned by otherUserId, so their owner memberships are already created
        var member1 = new LeagueMemberBuilder()
            .WithLeagueId(league1.Id)
            .WithUserId(userId)
            .WithRole(MemberRole.Member)
            .Build();

        var member3 = new LeagueMemberBuilder()
            .WithLeagueId(league3.Id)
            .WithUserId(userId)
            .WithRole(MemberRole.Member)
            .Build();

        Context.LeagueMembers.AddRange(member1, member3);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetUserLeaguesQueryHandler(Context, CurrentUserService);
        var query = new GetUserLeaguesQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(3);

        // Verify leagues are ordered by CreatedAt descending
        await Assert.That(result.Value[0].Name).IsEqualTo("League 3");
        await Assert.That(result.Value[1].Name).IsEqualTo("League 2");
        await Assert.That(result.Value[2].Name).IsEqualTo("League 1");
    }

    [Test]
    public async Task Handle_LeagueSummary_ContainsCorrectData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).WithUsername("currentuser").Build();
        
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).WithUsername("leagueowner").Build();

        Context.Users.AddRange(user, owner);
        await Context.SaveChangesAsync();

        var league = new LeagueBuilder()
            .WithName("Test League")
            .WithOwnerId(ownerId)
            .WithPublic(true)
            .Build();

        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        // Add two more members to the league (owner membership is already created)
        var member1 = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithRole(MemberRole.Member)
            .Build();

        var member3Id = Guid.NewGuid();
        var member3User = new UserBuilder().WithId(member3Id).Build();
        Context.Users.Add(member3User);

        var member3 = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(member3Id)
            .WithRole(MemberRole.Member)
            .Build();

        Context.LeagueMembers.AddRange(member1, member3);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetUserLeaguesQueryHandler(Context, CurrentUserService);
        var query = new GetUserLeaguesQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Count).IsEqualTo(1);

        var leagueSummary = result.Value[0];
        await Assert.That(leagueSummary.Id).IsEqualTo(league.Id);
        await Assert.That(leagueSummary.Name).IsEqualTo("Test League");
        await Assert.That(leagueSummary.OwnerUsername).IsEqualTo("leagueowner");
        await Assert.That(leagueSummary.MemberCount).IsEqualTo(3);
        await Assert.That(leagueSummary.IsPublic).IsTrue();
    }

    [Test]
    public async Task Handle_UserNotMemberOfLeague_DoesNotReturnThatLeague()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        
        var otherUserId = Guid.NewGuid();
        var otherUser = new UserBuilder().WithId(otherUserId).Build();

        Context.Users.AddRange(user, otherUser);
        await Context.SaveChangesAsync();

        var league = new LeagueBuilder()
            .WithOwnerId(otherUserId)
            .Build();

        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        // Owner membership is already created by League.Create
        // Don't add userId as member - they should not have access

        SetCurrentUser(userId);

        var handler = new GetUserLeaguesQueryHandler(Context, CurrentUserService);
        var query = new GetUserLeaguesQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Count).IsEqualTo(0);
    }
}
