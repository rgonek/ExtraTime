using ExtraTime.Application.Features.Admin.DTOs;
using ExtraTime.Application.Features.Admin.Queries.GetJobStats;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;

namespace ExtraTime.IntegrationTests.Application.Features.Admin;

public sealed class GetJobStatsQueryIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetJobStats_NoJobs_ReturnsZeroStats()
    {
        // Arrange
        var handler = new GetJobStatsQueryHandler(Context);
        var query = new GetJobStatsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();

        var stats = result.Value!;
        await Assert.That(stats.TotalJobs).IsEqualTo(0);
        await Assert.That(stats.PendingJobs).IsEqualTo(0);
        await Assert.That(stats.ProcessingJobs).IsEqualTo(0);
        await Assert.That(stats.CompletedJobs).IsEqualTo(0);
        await Assert.That(stats.FailedJobs).IsEqualTo(0);
        await Assert.That(stats.CancelledJobs).IsEqualTo(0);
    }

    [Test]
    public async Task GetJobStats_WithJobs_ReturnsCorrectStats()
    {
        // Arrange
        var pendingJob = new BackgroundJobBuilder()
            .WithStatus(JobStatus.Pending)
            .Build();

        var processingJob = new BackgroundJobBuilder()
            .WithStatus(JobStatus.Processing)
            .Build();

        var completedJob = new BackgroundJobBuilder()
            .WithStatus(JobStatus.Completed)
            .Build();

        var failedJob = new BackgroundJobBuilder()
            .WithStatus(JobStatus.Failed)
            .Build();

        var cancelledJob = new BackgroundJobBuilder()
            .WithStatus(JobStatus.Cancelled)
            .Build();

        Context.BackgroundJobs.AddRange(pendingJob, processingJob, completedJob, failedJob, cancelledJob);
        await Context.SaveChangesAsync();

        var handler = new GetJobStatsQueryHandler(Context);
        var query = new GetJobStatsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();

        var stats = result.Value!;
        await Assert.That(stats.TotalJobs).IsEqualTo(5);
        await Assert.That(stats.PendingJobs).IsEqualTo(1);
        await Assert.That(stats.ProcessingJobs).IsEqualTo(1);
        await Assert.That(stats.CompletedJobs).IsEqualTo(1);
        await Assert.That(stats.FailedJobs).IsEqualTo(1);
        await Assert.That(stats.CancelledJobs).IsEqualTo(1);
    }

    [Test]
    public async Task GetJobStats_MixedStatuses_ReturnsCorrectCounts()
    {
        // Arrange
        var pendingJobs = new[]
        {
            new BackgroundJobBuilder().WithStatus(JobStatus.Pending).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Pending).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Pending).Build()
        };

        var completedJobs = new[]
        {
            new BackgroundJobBuilder().WithStatus(JobStatus.Completed).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Completed).Build()
        };

        var failedJob = new BackgroundJobBuilder()
            .WithStatus(JobStatus.Failed)
            .Build();

        Context.BackgroundJobs.AddRange(pendingJobs);
        Context.BackgroundJobs.AddRange(completedJobs);
        Context.BackgroundJobs.Add(failedJob);
        await Context.SaveChangesAsync();

        var handler = new GetJobStatsQueryHandler(Context);
        var query = new GetJobStatsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();

        var stats = result.Value!;
        await Assert.That(stats.TotalJobs).IsEqualTo(6);
        await Assert.That(stats.PendingJobs).IsEqualTo(3);
        await Assert.That(stats.ProcessingJobs).IsEqualTo(0);
        await Assert.That(stats.CompletedJobs).IsEqualTo(2);
        await Assert.That(stats.FailedJobs).IsEqualTo(1);
        await Assert.That(stats.CancelledJobs).IsEqualTo(0);
    }
}
