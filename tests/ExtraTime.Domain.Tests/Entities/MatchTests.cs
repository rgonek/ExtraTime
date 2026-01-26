using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.Events;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class MatchTests
{
    [Test]
    public async Task Create_ShouldInitializeCorrectly()
    {
        // Act
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, MatchStatus.Scheduled);

        // Assert
        await Assert.That(match.ExternalId).IsEqualTo(123);
        await Assert.That(match.Status).IsEqualTo(MatchStatus.Scheduled);
    }

    [Test]
    public async Task UpdateStatus_ShouldRaiseEvent()
    {
        // Arrange
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, MatchStatus.Scheduled);
        
        // Act
        match.UpdateStatus(MatchStatus.InPlay);

        // Assert
        await Assert.That(match.Status).IsEqualTo(MatchStatus.InPlay);
        await Assert.That(match.DomainEvents.Any(e => e is MatchStatusChanged)).IsTrue();
    }

    [Test]
    public async Task IsOpenForBetting_WhenBeforeDeadline_ShouldBeTrue()
    {
        // Arrange
        var matchDate = DateTime.UtcNow.AddHours(1);
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), matchDate, MatchStatus.Scheduled);

        // Act
        var isOpen = match.IsOpenForBetting(15, DateTime.UtcNow);

        // Assert
        await Assert.That(isOpen).IsTrue();
    }

    [Test]
    public async Task IsOpenForBetting_WhenAfterDeadline_ShouldBeFalse()
    {
        // Arrange
        var matchDate = DateTime.UtcNow.AddMinutes(10);
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), matchDate, MatchStatus.Scheduled);

        // Act
        var isOpen = match.IsOpenForBetting(15, DateTime.UtcNow);

        // Assert
        await Assert.That(isOpen).IsFalse();
    }
}
