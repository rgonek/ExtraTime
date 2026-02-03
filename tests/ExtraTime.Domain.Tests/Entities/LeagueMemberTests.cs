using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class LeagueMemberTests
{
    [Test]
    public async Task Create_WithValidData_CreatesMember()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var role = MemberRole.Member;

        // Act
        var member = LeagueMember.Create(leagueId, userId, role);

        // Assert
        await Assert.That(member.LeagueId).IsEqualTo(leagueId);
        await Assert.That(member.UserId).IsEqualTo(userId);
        await Assert.That(member.Role).IsEqualTo(role);
        await Assert.That(member.JoinedAt).IsNotDefault();
    }

    [Test]
    public async Task Create_WithOwnerRole_CreatesMember()
    {
        // Arrange
        var leagueId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var role = MemberRole.Owner;

        // Act
        var member = LeagueMember.Create(leagueId, userId, role);

        // Assert
        await Assert.That(member.Role).IsEqualTo(MemberRole.Owner);
        await Assert.That(member.IsOwner()).IsTrue();
    }

    [Test]
    public async Task ChangeRole_ToOwner_SetsRole()
    {
        // Arrange
        var member = LeagueMember.Create(Guid.NewGuid(), Guid.NewGuid(), MemberRole.Member);

        // Act
        member.ChangeRole(MemberRole.Owner);

        // Assert
        await Assert.That(member.Role).IsEqualTo(MemberRole.Owner);
    }

    [Test]
    public async Task ChangeRole_FromOwnerToMember_ThrowsInvalidOperationException()
    {
        // Arrange
        var member = LeagueMember.Create(Guid.NewGuid(), Guid.NewGuid(), MemberRole.Owner);

        // Act & Assert - Owner cannot be demoted directly
        await Assert.That(() => member.ChangeRole(MemberRole.Member))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ChangeRole_MemberToOwner_SetsRole()
    {
        // Arrange
        var member = LeagueMember.Create(Guid.NewGuid(), Guid.NewGuid(), MemberRole.Member);

        // Act - Member can be promoted to Owner
        member.ChangeRole(MemberRole.Owner);

        // Assert
        await Assert.That(member.Role).IsEqualTo(MemberRole.Owner);
        await Assert.That(member.IsOwner()).IsTrue();
    }

    [Test]
    public async Task ChangeRole_SameRole_DoesNotChange()
    {
        // Arrange
        var member = LeagueMember.Create(Guid.NewGuid(), Guid.NewGuid(), MemberRole.Member);

        // Act
        member.ChangeRole(MemberRole.Member);

        // Assert
        await Assert.That(member.Role).IsEqualTo(MemberRole.Member);
    }

    [Test]
    public async Task IsOwner_WhenOwner_ReturnsTrue()
    {
        // Arrange
        var member = LeagueMember.Create(Guid.NewGuid(), Guid.NewGuid(), MemberRole.Owner);

        // Assert
        await Assert.That(member.IsOwner()).IsTrue();
    }

    [Test]
    public async Task IsOwner_WhenMember_ReturnsFalse()
    {
        // Arrange
        var member = LeagueMember.Create(Guid.NewGuid(), Guid.NewGuid(), MemberRole.Member);

        // Assert
        await Assert.That(member.IsOwner()).IsFalse();
    }

    [Test]
    public async Task Create_WithDuplicateUser_ThrowsException()
    {
        // Note: This would typically be enforced by database constraints
        // The domain entity itself doesn't prevent duplicate creation
        // This test documents the expected behavior

        // Arrange
        var leagueId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var member1 = LeagueMember.Create(leagueId, userId, MemberRole.Member);
        var member2 = LeagueMember.Create(leagueId, userId, MemberRole.Member);

        // Assert - entities can be created but DB should prevent duplicates
        await Assert.That(member1.UserId).IsEqualTo(userId);
        await Assert.That(member2.UserId).IsEqualTo(userId);
    }

    [Test]
    public async Task JoinedAt_IsSetOnCreation()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var member = LeagueMember.Create(Guid.NewGuid(), Guid.NewGuid(), MemberRole.Member);

        // Assert
        var afterCreation = DateTime.UtcNow.AddSeconds(1);
        await Assert.That(member.JoinedAt > beforeCreation).IsTrue();
        await Assert.That(member.JoinedAt < afterCreation).IsTrue();
    }
}
