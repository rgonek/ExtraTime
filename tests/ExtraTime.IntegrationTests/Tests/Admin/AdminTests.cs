using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Admin;
using ExtraTime.Application.Features.Admin.Commands.CancelJob;
using ExtraTime.Application.Features.Admin.Commands.RetryJob;
using ExtraTime.Application.Features.Admin.Queries.GetJobById;
using ExtraTime.Application.Features.Admin.Queries.GetJobs;
using ExtraTime.Application.Features.Admin.Queries.GetJobStats;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Tests.Admin;

public sealed class AdminTests : IntegrationTestBase
{
    //
    // Get Job Stats Tests
    //

    [Test]
    public async Task GetJobStats_WithJobs_ReturnsCorrectStats()
    {
        // Arrange
        Context.BackgroundJobs.Add(new BackgroundJobBuilder().WithStatus(JobStatus.Pending).Build());
        Context.BackgroundJobs.Add(new BackgroundJobBuilder().WithStatus(JobStatus.Processing).Build());
        Context.BackgroundJobs.Add(new BackgroundJobBuilder().WithStatus(JobStatus.Completed).Build());
        Context.BackgroundJobs.Add(new BackgroundJobBuilder().WithStatus(JobStatus.Failed).Build());
        Context.BackgroundJobs.Add(new BackgroundJobBuilder().WithStatus(JobStatus.Cancelled).Build());
        await Context.SaveChangesAsync();

        var handler = new GetJobStatsQueryHandler(Context);
        var query = new GetJobStatsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.TotalJobs).IsEqualTo(5);
        await Assert.That(result.Value!.PendingJobs).IsEqualTo(1);
    }

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
        await Assert.That(result.Value!.TotalJobs).IsEqualTo(0);
        await Assert.That(result.Value!.PendingJobs).IsEqualTo(0);
        await Assert.That(result.Value!.ProcessingJobs).IsEqualTo(0);
        await Assert.That(result.Value!.CompletedJobs).IsEqualTo(0);
        await Assert.That(result.Value!.FailedJobs).IsEqualTo(0);
        await Assert.That(result.Value!.CancelledJobs).IsEqualTo(0);
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
        await Assert.That(result.Value!.TotalJobs).IsEqualTo(6);
        await Assert.That(result.Value!.PendingJobs).IsEqualTo(3);
        await Assert.That(result.Value!.ProcessingJobs).IsEqualTo(0);
        await Assert.That(result.Value!.CompletedJobs).IsEqualTo(2);
        await Assert.That(result.Value!.FailedJobs).IsEqualTo(1);
        await Assert.That(result.Value!.CancelledJobs).IsEqualTo(0);
    }

    //
    // Get Job By Id Tests
    //

    [Test]
    public async Task GetJobById_ExistingJob_ReturnsJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Completed)
            .WithPayload("{\"key\":\"value\"}")
            .WithResult("Success")
            .WithRetryCount(0)
            .WithMaxRetries(3)
            .WithCreatedByUserId(userId)
            .WithCorrelationId("corr-123")
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var handler = new GetJobByIdQueryHandler(Context);
        var query = new GetJobByIdQuery(jobId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Id).IsEqualTo(jobId);
        await Assert.That(result.Value!.JobType).IsEqualTo("TestJob");
        await Assert.That(result.Value!.Status).IsEqualTo(JobStatus.Completed);
        await Assert.That(result.Value!.Payload).IsEqualTo("{\"key\":\"value\"}");
        await Assert.That(result.Value!.Result).IsEqualTo("Success");
        await Assert.That(result.Value!.RetryCount).IsEqualTo(0);
        await Assert.That(result.Value!.MaxRetries).IsEqualTo(3);
        await Assert.That(result.Value!.CreatedByUserId).IsEqualTo(userId);
        await Assert.That(result.Value!.CorrelationId).IsEqualTo("corr-123");
    }

    [Test]
    public async Task GetJobById_JobNotFound_ReturnsFailure()
    {
        // Arrange
        var handler = new GetJobByIdQueryHandler(Context);
        var query = new GetJobByIdQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobNotFound);
    }

    //
    // Get Jobs Tests
    //

    [Test]
    public async Task GetJobs_NoJobs_ReturnsEmptyPage()
    {
        // Arrange
        var handler = new GetJobsQueryHandler(Context);
        var query = new GetJobsQuery();

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Items.Count).IsEqualTo(0);
        await Assert.That(result.Value!.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetJobs_WithFilters_ReturnsPagedResults()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            Context.BackgroundJobs.Add(new BackgroundJobBuilder()
                .WithStatus(i % 2 == 0 ? JobStatus.Completed : JobStatus.Failed)
                .Build());
        }
        await Context.SaveChangesAsync();

        var handler = new GetJobsQueryHandler(Context);
        var query = new GetJobsQuery(Status: JobStatus.Completed, Page: 1, PageSize: 5);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Items.Count).IsEqualTo(5);
        await Assert.That(result.Value!.TotalCount).IsEqualTo(8); // 0, 2, 4, 6, 8, 10, 12, 14 are Completed
    }

    //
    // Retry Job Tests
    //

    [Test]
    public async Task RetryJob_FailedJob_RetriesSuccessfully()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Failed)
            .WithError("Previous error")
            .WithRetryCount(1)
            .WithMaxRetries(3)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new RetryJobCommandHandler(Context, jobDispatcher);
        var command = new RetryJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var retriedJob = await Context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        await Assert.That(retriedJob!.Status).IsEqualTo(JobStatus.Pending);
        await Assert.That(retriedJob.Error).IsNull();
        await Assert.That(retriedJob.RetryCount).IsEqualTo(2);

        await jobDispatcher.Received(1).DispatchAsync(Arg.Any<Domain.Entities.BackgroundJob>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RetryJob_NotFailed_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithStatus(JobStatus.Pending)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new RetryJobCommandHandler(Context, jobDispatcher);
        var command = new RetryJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeRetried);

        await jobDispatcher.DidNotReceive().DispatchAsync(Arg.Any<Domain.Entities.BackgroundJob>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RetryJob_MaxRetriesExceeded_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithStatus(JobStatus.Failed)
            .WithRetryCount(3)
            .WithMaxRetries(3)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new RetryJobCommandHandler(Context, jobDispatcher);
        var command = new RetryJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.MaxRetriesExceeded);

        await jobDispatcher.DidNotReceive().DispatchAsync(Arg.Any<Domain.Entities.BackgroundJob>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RetryJob_JobNotFound_ReturnsFailure()
    {
        // Arrange
        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new RetryJobCommandHandler(Context, jobDispatcher);
        var command = new RetryJobCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobNotFound);

        await jobDispatcher.DidNotReceive().DispatchAsync(Arg.Any<Domain.Entities.BackgroundJob>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RetryJob_NotFailed_Pending_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Pending)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new RetryJobCommandHandler(Context, jobDispatcher);
        var command = new RetryJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeRetried);

        await jobDispatcher.DidNotReceive().DispatchAsync(Arg.Any<Domain.Entities.BackgroundJob>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RetryJob_NotFailed_Processing_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Processing)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new RetryJobCommandHandler(Context, jobDispatcher);
        var command = new RetryJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeRetried);

        await jobDispatcher.DidNotReceive().DispatchAsync(Arg.Any<Domain.Entities.BackgroundJob>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RetryJob_NotFailed_Completed_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Completed)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new RetryJobCommandHandler(Context, jobDispatcher);
        var command = new RetryJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeRetried);

        await jobDispatcher.DidNotReceive().DispatchAsync(Arg.Any<Domain.Entities.BackgroundJob>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RetryJob_NotFailed_Cancelled_RetriesSuccessfully()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Cancelled)
            .WithRetryCount(0)
            .WithMaxRetries(3)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new RetryJobCommandHandler(Context, jobDispatcher);
        var command = new RetryJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert - Cancelled jobs CAN be retried (per domain logic in BackgroundJob.Retry())
        await Assert.That(result.IsSuccess).IsTrue();

        var retriedJob = await Context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        await Assert.That(retriedJob).IsNotNull();
        await Assert.That(retriedJob!.Status).IsEqualTo(JobStatus.Pending);

        await jobDispatcher.Received(1).DispatchAsync(Arg.Any<Domain.Entities.BackgroundJob>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RetryJob_IncrementsRetryCount()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Failed)
            .WithRetryCount(0)
            .WithMaxRetries(3)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var jobDispatcher = Substitute.For<IJobDispatcher>();
        var handler = new RetryJobCommandHandler(Context, jobDispatcher);
        var command = new RetryJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var retriedJob = await Context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        await Assert.That(retriedJob!.RetryCount).IsEqualTo(1);
    }

    //
    // Cancel Job Tests
    //

    [Test]
    public async Task CancelJob_PendingJob_CancelsSuccessfully()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Pending)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var handler = new CancelJobCommandHandler(Context);
        var command = new CancelJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var cancelledJob = await Context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        await Assert.That(cancelledJob!.Status).IsEqualTo(JobStatus.Cancelled);
        await Assert.That(cancelledJob.CompletedAt).IsNotNull();
    }

    [Test]
    public async Task CancelJob_CompletedJob_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithStatus(JobStatus.Completed)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var handler = new CancelJobCommandHandler(Context);
        var command = new CancelJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeCancelled);

        var unchangedJob = await Context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        await Assert.That(unchangedJob!.Status).IsEqualTo(JobStatus.Completed);
    }

    [Test]
    public async Task CancelJob_JobNotFound_ReturnsFailure()
    {
        // Arrange
        var handler = new CancelJobCommandHandler(Context);
        var command = new CancelJobCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobNotFound);
    }

    [Test]
    public async Task CancelJob_ProcessingJob_CancelsSuccessfully()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Processing)
            .WithStartedAt(DateTime.UtcNow)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var handler = new CancelJobCommandHandler(Context);
        var command = new CancelJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();

        var cancelledJob = await Context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        await Assert.That(cancelledJob).IsNotNull();
        await Assert.That(cancelledJob!.Status).IsEqualTo(JobStatus.Cancelled);
        await Assert.That(cancelledJob.CompletedAt).IsNotNull();
    }

    [Test]
    public async Task CancelJob_FailedJob_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Failed)
            .WithError("Some error")
            .WithCompletedAt(DateTime.UtcNow)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var handler = new CancelJobCommandHandler(Context);
        var command = new CancelJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeCancelled);

        var unchangedJob = await Context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        await Assert.That(unchangedJob!.Status).IsEqualTo(JobStatus.Failed);
    }

    [Test]
    public async Task CancelJob_AlreadyCancelled_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Cancelled)
            .WithCompletedAt(DateTime.UtcNow)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var handler = new CancelJobCommandHandler(Context);
        var command = new CancelJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeCancelled);

        var unchangedJob = await Context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        await Assert.That(unchangedJob!.Status).IsEqualTo(JobStatus.Cancelled);
    }

    //
    // Get Jobs Tests - Additional Filter Tests
    //

    [Test]
    public async Task GetJobs_ByStatus_ReturnsFilteredResults()
    {
        // Arrange
        var pendingJob = new BackgroundJobBuilder()
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Pending)
            .Build();

        var processingJob = new BackgroundJobBuilder()
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Processing)
            .Build();

        var completedJob = new BackgroundJobBuilder()
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Completed)
            .Build();

        Context.BackgroundJobs.AddRange(pendingJob, processingJob, completedJob);
        await Context.SaveChangesAsync();

        var handler = new GetJobsQueryHandler(Context);
        var query = new GetJobsQuery(Status: JobStatus.Pending);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Items.Count).IsEqualTo(1);
        await Assert.That(result.Value!.TotalCount).IsEqualTo(1);
        await Assert.That(result.Value!.Items[0].Status).IsEqualTo(JobStatus.Pending);
    }

    [Test]
    public async Task GetJobs_ByJobType_ReturnsFilteredResults()
    {
        // Arrange
        var syncJob1 = new BackgroundJobBuilder()
            .WithJobType("SyncCompetitions")
            .WithStatus(JobStatus.Completed)
            .Build();

        var syncJob2 = new BackgroundJobBuilder()
            .WithJobType("SyncCompetitions")
            .WithStatus(JobStatus.Pending)
            .Build();

        var calcJob = new BackgroundJobBuilder()
            .WithJobType("CalculateResults")
            .WithStatus(JobStatus.Completed)
            .Build();

        Context.BackgroundJobs.AddRange(syncJob1, syncJob2, calcJob);
        await Context.SaveChangesAsync();

        var handler = new GetJobsQueryHandler(Context);
        var query = new GetJobsQuery(JobType: "SyncCompetitions");

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Items.Count).IsEqualTo(2);
        await Assert.That(result.Value!.TotalCount).IsEqualTo(2);
        await Assert.That(result.Value!.Items[0].JobType).IsEqualTo("SyncCompetitions");
        await Assert.That(result.Value!.Items[1].JobType).IsEqualTo("SyncCompetitions");
    }
}
