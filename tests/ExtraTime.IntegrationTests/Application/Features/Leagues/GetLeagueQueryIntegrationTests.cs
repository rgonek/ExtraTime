using ExtraTime.Application.Features.Leagues;
using ExtraTime.Application.Features.Leagues.Queries.GetLeague;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Attributes;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Leagues;

[TestCategory(TestCategories.Significant)]
public sealed class GetLeagueQueryIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task Handle_LeagueDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetLeagueQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(LeagueErrors.LeagueNotFound);
    }

    [Test]
    public async Task Handle_UserNotAMember_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();

        Context.Users.AddRange(user, owner);
        await Context.SaveChangesAsync();

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();

        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();


        SetCurrentUser(userId);

        var handler = new GetLeagueQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(LeagueErrors.NotAMember);
    }

    [Test]
    public async Task Handle_ValidLeague_ReturnsLeagueDetail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).WithUsername("member1").Build();
        
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).WithUsername("leagueowner").Build();

        Context.Users.AddRange(user, owner);
        await Context.SaveChangesAsync();

        var league = new LeagueBuilder()
            .WithName("Test League")
            .WithDescription("Test Description")
            .WithOwnerId(ownerId)
            .WithPublic(true)
            .WithMaxMembers(50)
            .WithScoringRules(3, 1)
            .Build();

        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        // Reload the league to get the persisted state with owner membership
        Context.Entry(league).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        league = await Context.Leagues
            .Include(l => l.Members)
            .FirstAsync(l => l.Id == league.Id);

        // Now add the user as a member
        var userMember = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithRole(MemberRole.Member)
            .Build();

        Context.LeagueMembers.Add(userMember);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetLeagueQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();

        var leagueDetail = result.Value!;
        await Assert.That(leagueDetail.Id).IsEqualTo(league.Id);
        await Assert.That(leagueDetail.Name).IsEqualTo("Test League");
        await Assert.That(leagueDetail.Description).IsEqualTo("Test Description");
        await Assert.That(leagueDetail.OwnerId).IsEqualTo(ownerId);
        await Assert.That(leagueDetail.OwnerUsername).IsEqualTo("leagueowner");
        await Assert.That(leagueDetail.IsPublic).IsTrue();
        await Assert.That(leagueDetail.MaxMembers).IsEqualTo(50);
        await Assert.That(leagueDetail.ScoreExactMatch).IsEqualTo(3);
        await Assert.That(leagueDetail.ScoreCorrectResult).IsEqualTo(1);
    }

    [Test]
    public async Task Handle_LeagueWithMultipleMembers_ReturnsAllMembers()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).WithUsername("member1").Build();
        
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).WithUsername("owner").Build();
        
        var member2Id = Guid.NewGuid();
        var member2 = new UserBuilder().WithId(member2Id).WithUsername("member2").Build();

        Context.Users.AddRange(user, owner, member2);
        await Context.SaveChangesAsync();

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();

        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var userMembership = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithRole(MemberRole.Member)
            .WithJoinedAt(DateTime.UtcNow.AddDays(-1))
            .Build();

        var member2Membership = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(member2Id)
            .WithRole(MemberRole.Member)
            .Build();

        Context.LeagueMembers.AddRange(userMembership, member2Membership);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetLeagueQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Members).IsNotNull();
        await Assert.That(result.Value.Members.Count).IsEqualTo(3);

        // Verify owner is first (ordered by role descending)
        await Assert.That(result.Value.Members[0].UserId).IsEqualTo(ownerId);
        await Assert.That(result.Value.Members[0].Username).IsEqualTo("owner");
        await Assert.That(result.Value.Members[0].Role).IsEqualTo(MemberRole.Owner);

        // Verify other members are ordered by JoinedAt
        await Assert.That(result.Value.Members[1].UserId).IsEqualTo(userId);
        await Assert.That(result.Value.Members[2].UserId).IsEqualTo(member2Id);
    }

    [Test]
    public async Task Handle_LeagueWithAllowedCompetitions_ParsesCompetitionIds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();

        Context.Users.AddRange(user, owner);
        await Context.SaveChangesAsync();

        var competitionId1 = Guid.NewGuid();
        var competitionId2 = Guid.NewGuid();

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .WithAllowedCompetitions(competitionId1, competitionId2)
            .Build();

        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var userMember = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithRole(MemberRole.Member)
            .Build();

        Context.LeagueMembers.Add(userMember);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetLeagueQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.AllowedCompetitionIds).IsNotNull();
        await Assert.That(result.Value.AllowedCompetitionIds!.Length).IsEqualTo(2);
        await Assert.That(result.Value.AllowedCompetitionIds).Contains(competitionId1);
        await Assert.That(result.Value.AllowedCompetitionIds).Contains(competitionId2);
    }

    [Test]
    public async Task Handle_LeagueWithNoAllowedCompetitions_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).Build();

        Context.Users.AddRange(user, owner);
        await Context.SaveChangesAsync();

        // League without allowed competitions (default is null)
        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();

        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        var userMember = new LeagueMemberBuilder()
            .WithLeagueId(league.Id)
            .WithUserId(userId)
            .WithRole(MemberRole.Member)
            .Build();

        Context.LeagueMembers.Add(userMember);
        await Context.SaveChangesAsync();

        SetCurrentUser(userId);

        var handler = new GetLeagueQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.AllowedCompetitionIds).IsNull();
    }

    [Test]
    public async Task Handle_OwnerCanAccessLeague_ReturnsSuccess()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var owner = new UserBuilder().WithId(ownerId).WithUsername("owner").Build();

        Context.Users.Add(owner);
        await Context.SaveChangesAsync();

        var league = new LeagueBuilder()
            .WithOwnerId(ownerId)
            .Build();

        Context.Leagues.Add(league);
        await Context.SaveChangesAsync();

        SetCurrentUser(ownerId);

        var handler = new GetLeagueQueryHandler(Context, CurrentUserService);
        var query = new GetLeagueQuery(league.Id);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.OwnerId).IsEqualTo(ownerId);
    }
}
