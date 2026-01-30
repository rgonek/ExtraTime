using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Admin;
using ExtraTime.Application.Features.Admin.Commands.RetryJob;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Admin.Handlers;

public sealed class RetryJobCommandHandlerTests : HandlerTestBase
{
    private readonly IJobDispatcher _jobDispatcher;
    private readonly RetryJobCommandHandler _handler;

    public RetryJobCommandHandlerTests()
    {
        _jobDispatcher = Substitute.For<IJobDispatcher>();
        _handler = new RetryJobCommandHandler(Context, _jobDispatcher);
    }

    [Test]
    public async Task Handle_FailedJob_RetriesJob()
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

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var command = new RetryJobCommand(jobId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(job.Status).IsEqualTo(JobStatus.Pending);
        await Assert.That(job.Error).IsNull();
        await Assert.That(job.RetryCount).IsEqualTo(2);
        await Assert.That(job.StartedAt).IsNull();
        await Assert.That(job.CompletedAt).IsNull();

        await Context.Received(1).SaveChangesAsync(CancellationToken);
        await _jobDispatcher.Received(1).DispatchAsync(job, CancellationToken);
    }

    [Test]
    public async Task Handle_JobNotFound_ReturnsFailure()
    {
        // Arrange
        var mockJobs = CreateMockDbSet(new List<BackgroundJob>().AsQueryable());
        Context.BackgroundJobs.Returns(mockJobs);

        var command = new RetryJobCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobNotFound);
        await _jobDispatcher.DidNotReceive().DispatchAsync(Arg.Any<BackgroundJob>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_PendingJob_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Pending)
            .Build();

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var command = new RetryJobCommand(jobId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeRetried);
        await _jobDispatcher.DidNotReceive().DispatchAsync(Arg.Any<BackgroundJob>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ProcessingJob_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Processing)
            .Build();

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var command = new RetryJobCommand(jobId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeRetried);
    }

    [Test]
    public async Task Handle_CompletedJob_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Completed)
            .Build();

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var command = new RetryJobCommand(jobId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeRetried);
    }

    [Test]
    public async Task Handle_MaxRetriesExceeded_ReturnsFailure()
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

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var command = new RetryJobCommand(jobId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.MaxRetriesExceeded);
        await _jobDispatcher.DidNotReceive().DispatchAsync(Arg.Any<BackgroundJob>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ExactlyAtMaxRetries_ReturnsFailure()
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

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var command = new RetryJobCommand(jobId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.MaxRetriesExceeded);
    }

    [Test]
    public async Task Handle_OneRetryRemaining_Succeeds()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Failed)
            .WithRetryCount(2)
            .WithMaxRetries(3)
            .Build();

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var command = new RetryJobCommand(jobId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(job.RetryCount).IsEqualTo(3);
        await _jobDispatcher.Received(1).DispatchAsync(job, CancellationToken);
    }
}
