using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.Events;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class LeagueTests
{
    [Test]
    public async Task Create_WithValidData_ShouldInitializeLeagueCorrectly()
    {
        // Arrange
        var name = "Test League";
        var ownerId = Guid.NewGuid();
        var inviteCode = "ABCDEF";

        // Act
        var league = League.Create(name, ownerId, inviteCode);

        // Assert
        await Assert.That(league.Name).IsEqualTo(name);
        await Assert.That(league.OwnerId).IsEqualTo(ownerId);
        await Assert.That(league.InviteCode).IsEqualTo(inviteCode);
        await Assert.That(league.Members).Count().IsEqualTo(1);

        var ownerMember = league.Members.First();
        await Assert.That(ownerMember.UserId).IsEqualTo(ownerId);
        await Assert.That(ownerMember.Role).IsEqualTo(MemberRole.Owner);
    }

    [Test]
    public async Task Create_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.That(() => League.Create("", Guid.NewGuid(), "CODE"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task AddMember_WhenLeagueNotFull_ShouldAddMember()
    {
        // Arrange
        var league = League.Create("Test", Guid.NewGuid(), "CODE", maxMembers: 2);
        var newUserId = Guid.NewGuid();

        // Act
        league.AddMember(newUserId, MemberRole.Member);

        // Assert
        await Assert.That(league.Members).Count().IsEqualTo(2);
        await Assert.That(league.Members.Any(m => m.UserId == newUserId)).IsTrue();
        await Assert.That(league.DomainEvents.Any(e => e is LeagueMemberAdded)).IsTrue();
    }

    [Test]
    public async Task AddMember_WhenLeagueFull_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var league = League.Create("Test", Guid.NewGuid(), "CODE", maxMembers: 2);
        league.AddMember(Guid.NewGuid(), MemberRole.Member);
        var thirdUserId = Guid.NewGuid();

        // Act & Assert
        await Assert.That(() => league.AddMember(thirdUserId, MemberRole.Member))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RemoveMember_WhenUserIsOwner_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var league = League.Create("Test", ownerId, "CODE");

        // Act & Assert
        await Assert.That(() => league.RemoveMember(ownerId))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RemoveMember_WhenUserIsRegularMember_ShouldRemoveMember()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var league = League.Create("Test", ownerId, "CODE");
        var userId = Guid.NewGuid();
        league.AddMember(userId, MemberRole.Member);
        league.ClearDomainEvents();

        // Act
        league.RemoveMember(userId);

        // Assert
        await Assert.That(league.Members).Count().IsEqualTo(1);
        await Assert.That(league.DomainEvents.Any(e => e is LeagueMemberRemoved)).IsTrue();
    }

    [Test]
    public async Task RegenerateInviteCode_ShouldUpdateCodeAndRaiseEvent()
    {
        // Arrange
        var league = League.Create("Test", Guid.NewGuid(), "OLDCODE");
        var newCode = "NEWCODE";
        league.ClearDomainEvents();

        // Act
        league.RegenerateInviteCode(newCode);

        // Assert
        await Assert.That(league.InviteCode).IsEqualTo(newCode);
        await Assert.That(league.DomainEvents.Any(e => e is LeagueInviteCodeRegenerated)).IsTrue();
    }

    [Test]
    public async Task UpdateSettings_WithValidData_ShouldUpdateProperties()
    {
        // Arrange
        var league = League.Create("Test", Guid.NewGuid(), "CODE");

        // Act
        league.UpdateSettings("New Name", "New Description", true, 100, 5, 2, 10);

        // Assert
        await Assert.That(league.Name).IsEqualTo("New Name");
        await Assert.That(league.Description).IsEqualTo("New Description");
        await Assert.That(league.IsPublic).IsTrue();
        await Assert.That(league.MaxMembers).IsEqualTo(100);
        await Assert.That(league.ScoreExactMatch).IsEqualTo(5);
        await Assert.That(league.ScoreCorrectResult).IsEqualTo(2);
        await Assert.That(league.BettingDeadlineMinutes).IsEqualTo(10);
    }
}
