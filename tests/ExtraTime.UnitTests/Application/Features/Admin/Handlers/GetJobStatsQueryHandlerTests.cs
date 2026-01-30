using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Admin.DTOs;
using ExtraTime.Application.Features.Admin.Queries.GetJobStats;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Admin.Handlers;

public sealed class GetJobStatsQueryHandlerTests : HandlerTestBase
{
    private readonly GetJobStatsQueryHandler _handler;

    public GetJobStatsQueryHandlerTests()
    {
        _handler = new GetJobStatsQueryHandler(Context);
    }

    [Test]
    public async Task Handle_NoJobs_ReturnsZeroStats()
    {
        // Arrange
        var mockJobs = CreateMockDbSet(new List<BackgroundJob>().AsQueryable());
        Context.BackgroundJobs.Returns(mockJobs);

        var query = new GetJobStatsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.TotalJobs).IsEqualTo(0);
        await Assert.That(result.Value.PendingJobs).IsEqualTo(0);
        await Assert.That(result.Value.ProcessingJobs).IsEqualTo(0);
        await Assert.That(result.Value.CompletedJobs).IsEqualTo(0);
        await Assert.That(result.Value.FailedJobs).IsEqualTo(0);
        await Assert.That(result.Value.CancelledJobs).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_MixedJobs_ReturnsCorrectStats()
    {
        // Arrange
        var jobs = new List<BackgroundJob>
        {
            new BackgroundJobBuilder().WithStatus(JobStatus.Pending).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Pending).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Processing).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Completed).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Completed).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Completed).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Failed).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Cancelled).Build()
        };

        var mockJobs = CreateMockDbSet(jobs.AsQueryable());
        Context.BackgroundJobs.Returns(mockJobs);

        var query = new GetJobStatsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.TotalJobs).IsEqualTo(8);
        await Assert.That(result.Value.PendingJobs).IsEqualTo(2);
        await Assert.That(result.Value.ProcessingJobs).IsEqualTo(1);
        await Assert.That(result.Value.CompletedJobs).IsEqualTo(3);
        await Assert.That(result.Value.FailedJobs).IsEqualTo(1);
        await Assert.That(result.Value.CancelledJobs).IsEqualTo(1);
    }

    [Test]
    public async Task Handle_AllPendingJobs_ReturnsCorrectStats()
    {
        // Arrange
        var jobs = Enumerable.Range(1, 10)
            .Select(_ => new BackgroundJobBuilder().WithStatus(JobStatus.Pending).Build())
            .ToList();

        var mockJobs = CreateMockDbSet(jobs.AsQueryable());
        Context.BackgroundJobs.Returns(mockJobs);

        var query = new GetJobStatsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.TotalJobs).IsEqualTo(10);
        await Assert.That(result.Value.PendingJobs).IsEqualTo(10);
        await Assert.That(result.Value.ProcessingJobs).IsEqualTo(0);
        await Assert.That(result.Value.CompletedJobs).IsEqualTo(0);
        await Assert.That(result.Value.FailedJobs).IsEqualTo(0);
        await Assert.That(result.Value.CancelledJobs).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_AllCompletedJobs_ReturnsCorrectStats()
    {
        // Arrange
        var jobs = Enumerable.Range(1, 5)
            .Select(_ => new BackgroundJobBuilder().WithStatus(JobStatus.Completed).Build())
            .ToList();

        var mockJobs = CreateMockDbSet(jobs.AsQueryable());
        Context.BackgroundJobs.Returns(mockJobs);

        var query = new GetJobStatsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.TotalJobs).IsEqualTo(5);
        await Assert.That(result.Value.CompletedJobs).IsEqualTo(5);
        await Assert.That(result.Value.PendingJobs).IsEqualTo(0);
        await Assert.That(result.Value.ProcessingJobs).IsEqualTo(0);
        await Assert.That(result.Value.FailedJobs).IsEqualTo(0);
        await Assert.That(result.Value.CancelledJobs).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_AllFailedJobs_ReturnsCorrectStats()
    {
        // Arrange
        var jobs = Enumerable.Range(1, 3)
            .Select(_ => new BackgroundJobBuilder().WithStatus(JobStatus.Failed).Build())
            .ToList();

        var mockJobs = CreateMockDbSet(jobs.AsQueryable());
        Context.BackgroundJobs.Returns(mockJobs);

        var query = new GetJobStatsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.TotalJobs).IsEqualTo(3);
        await Assert.That(result.Value.FailedJobs).IsEqualTo(3);
        await Assert.That(result.Value.PendingJobs).IsEqualTo(0);
        await Assert.That(result.Value.ProcessingJobs).IsEqualTo(0);
        await Assert.That(result.Value.CompletedJobs).IsEqualTo(0);
        await Assert.That(result.Value.CancelledJobs).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_SingleJobOfEachStatus_ReturnsCorrectStats()
    {
        // Arrange
        var jobs = new List<BackgroundJob>
        {
            new BackgroundJobBuilder().WithStatus(JobStatus.Pending).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Processing).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Completed).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Failed).Build(),
            new BackgroundJobBuilder().WithStatus(JobStatus.Cancelled).Build()
        };

        var mockJobs = CreateMockDbSet(jobs.AsQueryable());
        Context.BackgroundJobs.Returns(mockJobs);

        var query = new GetJobStatsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.TotalJobs).IsEqualTo(5);
        await Assert.That(result.Value.PendingJobs).IsEqualTo(1);
        await Assert.That(result.Value.ProcessingJobs).IsEqualTo(1);
        await Assert.That(result.Value.CompletedJobs).IsEqualTo(1);
        await Assert.That(result.Value.FailedJobs).IsEqualTo(1);
        await Assert.That(result.Value.CancelledJobs).IsEqualTo(1);
    }
}
