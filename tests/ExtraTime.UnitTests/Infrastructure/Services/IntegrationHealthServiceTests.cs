using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Services;
using ExtraTime.UnitTests.Attributes;
using ExtraTime.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

[TestCategory("Significant")]
public sealed class IntegrationHealthServiceTests : HandlerTestBase
{
    private readonly ILogger<IntegrationHealthService> _logger;
    private readonly IntegrationHealthService _service;

    public IntegrationHealthServiceTests()
    {
        _logger = Substitute.For<ILogger<IntegrationHealthService>>();
        _service = new IntegrationHealthService(Context, _logger);
    }

    [Test]
    public async Task GetStatusAsync_WhenMissing_ShouldCreateStatus()
    {
        // Arrange
        var statuses = new List<IntegrationStatus>();
        var mockSet = CreateMockDbSet(statuses.AsQueryable());
        mockSet.When(s => s.Add(Arg.Any<IntegrationStatus>()))
            .Do(ci => statuses.Add(ci.Arg<IntegrationStatus>()));

        Context.IntegrationStatuses.Returns(mockSet);
        Context.SaveChangesAsync(CancellationToken).Returns(1);

        // Act
        var status = await _service.GetStatusAsync(IntegrationType.ClubElo, CancellationToken);

        // Assert
        await Assert.That(status.IntegrationName).IsEqualTo("ClubElo");
        await Assert.That(status.Health).IsEqualTo(IntegrationHealth.Unknown);
        await Assert.That(status.StaleThreshold).IsEqualTo(TimeSpan.FromHours(48));
        mockSet.Received(1).Add(Arg.Any<IntegrationStatus>());
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task RecordFailureAsync_ShouldIncreaseFailureCounters()
    {
        // Arrange
        var status = new IntegrationStatus
        {
            IntegrationName = IntegrationType.Understat.ToString(),
            Health = IntegrationHealth.Unknown
        };
        var mockSet = CreateMockDbSet(new List<IntegrationStatus> { status }.AsQueryable());
        Context.IntegrationStatuses.Returns(mockSet);
        Context.SaveChangesAsync(CancellationToken).Returns(1);

        // Act
        await _service.RecordFailureAsync(
            IntegrationType.Understat,
            "Network timeout",
            "stack",
            CancellationToken);

        // Assert
        await Assert.That(status.ConsecutiveFailures).IsEqualTo(1);
        await Assert.That(status.TotalFailures24h).IsEqualTo(1);
        await Assert.That(status.Health).IsEqualTo(IntegrationHealth.Degraded);
        await Assert.That(status.LastErrorMessage).IsEqualTo("Network timeout");
        await Assert.That(status.LastErrorDetails).IsEqualTo("stack");
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task HasFreshDataAsync_WhenStale_ShouldReturnFalse()
    {
        // Arrange
        var status = new IntegrationStatus
        {
            IntegrationName = IntegrationType.Understat.ToString(),
            Health = IntegrationHealth.Healthy,
            DataFreshAsOf = DateTime.UtcNow.AddDays(-3),
            StaleThreshold = TimeSpan.FromHours(24)
        };
        var mockSet = CreateMockDbSet(new List<IntegrationStatus> { status }.AsQueryable());
        Context.IntegrationStatuses.Returns(mockSet);

        // Act
        var hasFreshData = await _service.HasFreshDataAsync(IntegrationType.Understat, CancellationToken);

        // Assert
        await Assert.That(hasFreshData).IsFalse();
    }

    [Test]
    public async Task GetDataAvailabilityAsync_ShouldReturnExpectedAvailability()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var statuses = new List<IntegrationStatus>
        {
            new()
            {
                IntegrationName = IntegrationType.FootballDataOrg.ToString(),
                Health = IntegrationHealth.Healthy,
                DataFreshAsOf = now
            },
            new()
            {
                IntegrationName = IntegrationType.Understat.ToString(),
                Health = IntegrationHealth.Healthy,
                DataFreshAsOf = now
            },
            new()
            {
                IntegrationName = IntegrationType.FootballDataUk.ToString(),
                Health = IntegrationHealth.Failed,
                DataFreshAsOf = now
            },
            new()
            {
                IntegrationName = IntegrationType.ApiFootball.ToString(),
                Health = IntegrationHealth.Degraded,
                DataFreshAsOf = now
            },
            new()
            {
                IntegrationName = IntegrationType.ClubElo.ToString(),
                Health = IntegrationHealth.Healthy,
                DataFreshAsOf = now
            },
            new()
            {
                IntegrationName = IntegrationType.LineupProvider.ToString(),
                Health = IntegrationHealth.Healthy,
                DataFreshAsOf = now
            }
        };
        var mockSet = CreateMockDbSet(statuses.AsQueryable());
        Context.IntegrationStatuses.Returns(mockSet);

        // Act
        var availability = await _service.GetDataAvailabilityAsync(CancellationToken);

        // Assert
        await Assert.That(availability.XgDataAvailable).IsTrue();
        await Assert.That(availability.OddsDataAvailable).IsFalse();
        await Assert.That(availability.InjuryDataAvailable).IsTrue();
        await Assert.That(availability.LineupDataAvailable).IsTrue();
        await Assert.That(availability.EloDataAvailable).IsTrue();
        await Assert.That(availability.StandingsDataAvailable).IsTrue();
        await Assert.That(availability.AvailableSourceCount).IsEqualTo(6);
        await Assert.That(availability.HasAnyExternalData).IsTrue();
    }

    [Test]
    public async Task DisableAndEnableIntegrationAsync_ShouldToggleManualDisableState()
    {
        // Arrange
        var status = new IntegrationStatus
        {
            IntegrationName = IntegrationType.ApiFootball.ToString(),
            Health = IntegrationHealth.Healthy
        };
        var mockSet = CreateMockDbSet(new List<IntegrationStatus> { status }.AsQueryable());
        Context.IntegrationStatuses.Returns(mockSet);
        Context.SaveChangesAsync(CancellationToken).Returns(1);

        // Act
        await _service.DisableIntegrationAsync(
            IntegrationType.ApiFootball,
            "Rate limits",
            "admin",
            CancellationToken);
        await _service.EnableIntegrationAsync(IntegrationType.ApiFootball, CancellationToken);

        // Assert
        await Assert.That(status.IsManuallyDisabled).IsFalse();
        await Assert.That(status.DisabledReason).IsNull();
        await Assert.That(status.DisabledBy).IsNull();
        await Assert.That(status.DisabledAt).IsNull();
        await Assert.That(status.Health).IsEqualTo(IntegrationHealth.Unknown);
        await Context.Received(2).SaveChangesAsync(CancellationToken);
    }
}
