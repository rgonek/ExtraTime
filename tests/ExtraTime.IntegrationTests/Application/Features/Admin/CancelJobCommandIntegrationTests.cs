using ExtraTime.Application.Features.Admin;
using ExtraTime.Application.Features.Admin.Commands.CancelJob;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.IntegrationTests.Application.Features.Admin;

public sealed class CancelJobCommandIntegrationTests : IntegrationTestBase
{
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

        await Assert.That(cancelledJob).IsNotNull();
        await Assert.That(cancelledJob!.Status).IsEqualTo(JobStatus.Cancelled);
        await Assert.That(cancelledJob.CompletedAt).IsNotNull();
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
    public async Task CancelJob_CompletedJob_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Completed)
            .WithCompletedAt(DateTime.UtcNow)
            .Build();

        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var handler = new CancelJobCommandHandler(Context);
        var command = new CancelJobCommand(jobId);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeCancelled);

        var unchangedJob = await Context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        await Assert.That(unchangedJob!.Status).IsEqualTo(JobStatus.Completed);
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
        await Assert.That(result.IsSuccess).IsFalse();
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
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeCancelled);

        var unchangedJob = await Context.BackgroundJobs
            .FirstOrDefaultAsync(j => j.Id == jobId);

        await Assert.That(unchangedJob!.Status).IsEqualTo(JobStatus.Cancelled);
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
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobNotFound);
    }
}
