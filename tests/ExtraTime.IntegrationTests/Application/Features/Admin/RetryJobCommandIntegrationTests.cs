using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Admin;
using ExtraTime.Application.Features.Admin.Commands.RetryJob;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Application.Features.Admin;

public sealed class RetryJobCommandIntegrationTests : IntegrationTestBase
{
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
            .WithStartedAt(DateTime.UtcNow)
            .WithCompletedAt(DateTime.UtcNow)
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

        await Assert.That(retriedJob).IsNotNull();
        await Assert.That(retriedJob!.Status).IsEqualTo(JobStatus.Pending);
        await Assert.That(retriedJob.Error).IsNull();
        await Assert.That(retriedJob.RetryCount).IsEqualTo(2);
        // Note: StartedAt is not cleared by Retry() method - it tracks when the job first started
        await Assert.That(retriedJob.CompletedAt).IsNull();

        await jobDispatcher.Received(1).DispatchAsync(Arg.Any<Domain.Entities.BackgroundJob>(), Arg.Any<CancellationToken>());
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
    public async Task RetryJob_MaxRetriesExceeded_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
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
        await Assert.That(result.IsSuccess).IsFalse();
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
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobNotFound);

        await jobDispatcher.DidNotReceive().DispatchAsync(Arg.Any<Domain.Entities.BackgroundJob>(), Arg.Any<CancellationToken>());
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
}
