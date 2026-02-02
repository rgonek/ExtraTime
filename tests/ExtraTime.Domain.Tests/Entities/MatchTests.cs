using ExtraTime.Domain.Common;
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
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Scheduled);

        // Assert
        await Assert.That(match.ExternalId).IsEqualTo(123);
        await Assert.That(match.Status).IsEqualTo(MatchStatus.Scheduled);
    }

    [Test]
    public async Task UpdateStatus_ShouldRaiseEvent()
    {
        // Arrange
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Scheduled);
        
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
        var matchDate = Clock.UtcNow.AddHours(1);
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), matchDate, MatchStatus.Scheduled);

        // Act
        var isOpen = match.IsOpenForBetting(15, Clock.UtcNow);

        // Assert
        await Assert.That(isOpen).IsTrue();
    }

    [Test]
    public async Task IsOpenForBetting_WhenAfterDeadline_ShouldBeFalse()
    {
        // Arrange
        var matchDate = Clock.UtcNow.AddMinutes(10);
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), matchDate, MatchStatus.Scheduled);

        // Act
        var isOpen = match.IsOpenForBetting(15, Clock.UtcNow);

        // Assert
        await Assert.That(isOpen).IsFalse();
    }

    [Test]
    public async Task UpdateScore_UpdatesHomeAndAwayScore()
    {
        // Arrange
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Scheduled);
        match.ClearDomainEvents();

        // Act
        match.UpdateScore(2, 1);

        // Assert
        await Assert.That(match.HomeScore).IsEqualTo(2);
        await Assert.That(match.AwayScore).IsEqualTo(1);
        await Assert.That(match.DomainEvents).Count().IsEqualTo(1);
        await Assert.That(match.DomainEvents.First()).IsTypeOf<MatchScoreUpdated>();
    }

    [Test]
    public async Task UpdateScore_WithHalfTimeScores_UpdatesAllScores()
    {
        // Arrange
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Scheduled);

        // Act
        match.UpdateScore(2, 1, 1, 0);

        // Assert
        await Assert.That(match.HomeScore).IsEqualTo(2);
        await Assert.That(match.AwayScore).IsEqualTo(1);
        await Assert.That(match.HomeHalfTimeScore).IsEqualTo(1);
        await Assert.That(match.AwayHalfTimeScore).IsEqualTo(0);
    }

    [Test]
    public async Task UpdateScore_WithNegativeScores_ThrowsArgumentException()
    {
        // Arrange
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Scheduled);

        // Act & Assert
        await Assert.That(() => match.UpdateScore(-1, 0))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task UpdateMetadata_UpdatesVenueAndStatus()
    {
        // Arrange
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Scheduled);
        
        // Act
        match.UpdateMetadata(5, "Quarter Final", "Group A", "Old Trafford");

        // Assert
        await Assert.That(match.Matchday).IsEqualTo(5);
        await Assert.That(match.Stage).IsEqualTo("Quarter Final");
        await Assert.That(match.Group).IsEqualTo("Group A");
        await Assert.That(match.Venue).IsEqualTo("Old Trafford");
    }

    [Test]
    public async Task UpdateMetadata_WithNullValues_AllowsNulls()
    {
        // Arrange
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Scheduled);
        match.UpdateMetadata(5, "Stage", "Group", "Venue");
        
        // Act
        match.UpdateMetadata(null, null, null, null);

        // Assert
        await Assert.That(match.Matchday).IsNull();
        await Assert.That(match.Stage).IsNull();
        await Assert.That(match.Group).IsNull();
        await Assert.That(match.Venue).IsNull();
    }

    [Test]
    public async Task SyncDetails_UpdatesExternalData()
    {
        // Arrange
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Scheduled);
        var newDate = Clock.UtcNow.AddDays(1);
        
        // Act
        match.SyncDetails(newDate, MatchStatus.InPlay, 1, 0);

        // Assert
        await Assert.That(match.MatchDateUtc).IsEqualTo(newDate);
        await Assert.That(match.Status).IsEqualTo(MatchStatus.InPlay);
        await Assert.That(match.HomeScore).IsEqualTo(1);
        await Assert.That(match.AwayScore).IsEqualTo(0);
    }

    [Test]
    public async Task IsOpenForBetting_MatchStarted_ReturnsFalse()
    {
        // Arrange
        var matchDate = Clock.UtcNow.AddHours(-1); // Already started
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), matchDate, MatchStatus.InPlay);

        // Act
        var isOpen = match.IsOpenForBetting(15, Clock.UtcNow);

        // Assert
        await Assert.That(isOpen).IsFalse();
    }

    [Test]
    public async Task IsOpenForBetting_MatchFinished_ReturnsFalse()
    {
        // Arrange
        var matchDate = Clock.UtcNow.AddHours(-2); // Already finished
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), matchDate, MatchStatus.Finished);

        // Act
        var isOpen = match.IsOpenForBetting(15, Clock.UtcNow);

        // Assert
        await Assert.That(isOpen).IsFalse();
    }

    [Test]
    public async Task UpdateStatus_FromScheduledToLive_SetsCorrectly()
    {
        // Arrange
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Scheduled);
        match.ClearDomainEvents();

        // Act
        match.UpdateStatus(MatchStatus.InPlay);

        // Assert
        await Assert.That(match.Status).IsEqualTo(MatchStatus.InPlay);
        await Assert.That(match.DomainEvents).Count().IsEqualTo(1);
        await Assert.That(match.DomainEvents.First()).IsTypeOf<MatchStatusChanged>();
    }

    [Test]
    public async Task UpdateStatus_FromLiveToFinished_SetsCorrectly()
    {
        // Arrange
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.InPlay);
        match.ClearDomainEvents();

        // Act
        match.UpdateStatus(MatchStatus.Finished);

        // Assert
        await Assert.That(match.Status).IsEqualTo(MatchStatus.Finished);
        await Assert.That(match.DomainEvents.First()).IsTypeOf<MatchStatusChanged>();
    }

    [Test]
    public async Task UpdateStatus_SameStatus_DoesNotRaiseEvent()
    {
        // Arrange
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Scheduled);
        match.ClearDomainEvents();

        // Act
        match.UpdateStatus(MatchStatus.Scheduled);

        // Assert
        await Assert.That(match.DomainEvents).IsEmpty();
    }

    [Test]
    public async Task UpdateStatus_FromFinalToActive_AllowsTransition()
    {
        // Arrange - Match is finished but status is being corrected
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Clock.UtcNow, MatchStatus.Finished);
        match.ClearDomainEvents();

        // Act - API sometimes corrects status
        match.UpdateStatus(MatchStatus.InPlay);

        // Assert - should allow this correction
        await Assert.That(match.Status).IsEqualTo(MatchStatus.InPlay);
    }

    [Test]
    public async Task IsOpenForBetting_MatchPostponed_ReturnsFalse()
    {
        // Arrange
        var matchDate = Clock.UtcNow.AddDays(1);
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), matchDate, MatchStatus.Postponed);

        // Act
        var isOpen = match.IsOpenForBetting(15, Clock.UtcNow);

        // Assert
        await Assert.That(isOpen).IsFalse();
    }

    [Test]
    public async Task IsOpenForBetting_MatchCancelled_ReturnsFalse()
    {
        // Arrange
        var matchDate = Clock.UtcNow.AddDays(1);
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), matchDate, MatchStatus.Cancelled);

        // Act
        var isOpen = match.IsOpenForBetting(15, Clock.UtcNow);

        // Assert
        await Assert.That(isOpen).IsFalse();
    }

    [Test]
    public async Task IsOpenForBetting_TimedStatus_ReturnsTrue()
    {
        // Arrange - Timed means match time is confirmed
        var matchDate = Clock.UtcNow.AddHours(1);
        var match = Match.Create(123, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), matchDate, MatchStatus.Timed);

        // Act
        var isOpen = match.IsOpenForBetting(15, Clock.UtcNow);

        // Assert
        await Assert.That(isOpen).IsTrue();
    }
}
