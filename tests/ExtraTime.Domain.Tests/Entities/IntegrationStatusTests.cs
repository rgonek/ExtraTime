using ExtraTime.Domain.Entities;

namespace ExtraTime.Domain.Tests.Entities;

public sealed class IntegrationStatusTests
{
    [Test]
    public async Task RecordSuccess_ShouldSetHealthyStatusAndResetFailures()
    {
        // Arrange
        var status = new IntegrationStatus
        {
            IntegrationName = "Understat",
            ConsecutiveFailures = 3,
            TotalFailures24h = 2,
            LastErrorMessage = "Timeout",
            LastErrorDetails = "details"
        };
        var duration = TimeSpan.FromSeconds(15);

        // Act
        status.RecordSuccess(duration);

        // Assert
        await Assert.That(status.Health).IsEqualTo(IntegrationHealth.Healthy);
        await Assert.That(status.ConsecutiveFailures).IsEqualTo(0);
        await Assert.That(status.SuccessfulSyncs24h).IsEqualTo(1);
        await Assert.That(status.LastSuccessfulSync).IsNotNull();
        await Assert.That(status.LastAttemptedSync).IsNotNull();
        await Assert.That(status.DataFreshAsOf).IsNotNull();
        await Assert.That(status.LastErrorMessage).IsNull();
        await Assert.That(status.LastErrorDetails).IsNull();
        await Assert.That(status.AverageSyncDuration).IsEqualTo(duration);
    }

    [Test]
    public async Task RecordFailure_FiveConsecutiveFailures_ShouldSetFailedHealth()
    {
        // Arrange
        var status = new IntegrationStatus
        {
            IntegrationName = "Understat",
            ConsecutiveFailures = 4
        };

        // Act
        status.RecordFailure("Request failed", "stack trace");

        // Assert
        await Assert.That(status.Health).IsEqualTo(IntegrationHealth.Failed);
        await Assert.That(status.ConsecutiveFailures).IsEqualTo(5);
        await Assert.That(status.TotalFailures24h).IsEqualTo(1);
        await Assert.That(status.LastFailedSync).IsNotNull();
        await Assert.That(status.LastAttemptedSync).IsNotNull();
        await Assert.That(status.LastErrorMessage).IsEqualTo("Request failed");
        await Assert.That(status.LastErrorDetails).IsEqualTo("stack trace");
    }

    [Test]
    public async Task IsDataStale_WhenFreshnessExceedsThreshold_ShouldBeTrue()
    {
        // Arrange
        var status = new IntegrationStatus
        {
            IntegrationName = "FootballDataUk",
            StaleThreshold = TimeSpan.FromHours(1),
            DataFreshAsOf = DateTime.UtcNow.AddHours(-2)
        };

        // Act & Assert
        await Assert.That(status.IsDataStale).IsTrue();
    }

    [Test]
    public async Task SuccessRate24h_ShouldCalculatePercentage()
    {
        // Arrange
        var status = new IntegrationStatus
        {
            IntegrationName = "API-Football",
            SuccessfulSyncs24h = 3,
            TotalFailures24h = 1
        };

        // Act & Assert
        await Assert.That(status.SuccessRate24h).IsEqualTo(75d);
    }
}
