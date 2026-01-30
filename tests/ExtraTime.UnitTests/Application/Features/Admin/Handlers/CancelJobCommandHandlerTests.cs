using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Admin;
using ExtraTime.Application.Features.Admin.Commands.CancelJob;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Admin.Handlers;

public sealed class CancelJobCommandHandlerTests : HandlerTestBase
{
    private readonly CancelJobCommandHandler _handler;
    private readonly DateTime _now = new(2026, 1, 30, 12, 0, 0, DateTimeKind.Utc);

    public CancelJobCommandHandlerTests()
    {
        _handler = new CancelJobCommandHandler(Context);
    }

    [Before(Test)]
    public void Setup()
    {
        Clock.Current = new FakeClock(_now);
    }

    [After(Test)]
    public void Cleanup()
    {
        Clock.Current = null!;
    }

    [Test]
    public async Task Handle_PendingJob_CancelsJob()
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

        var command = new CancelJobCommand(jobId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(job.Status).IsEqualTo(JobStatus.Cancelled);
        await Assert.That(job.CompletedAt).IsNotNull();
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task Handle_ProcessingJob_CancelsJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Processing)
            .WithStartedAt(_now.AddMinutes(-5))
            .Build();

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var command = new CancelJobCommand(jobId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(job.Status).IsEqualTo(JobStatus.Cancelled);
        await Assert.That(job.CompletedAt).IsNotNull();
    }

    [Test]
    public async Task Handle_JobNotFound_ReturnsFailure()
    {
        // Arrange
        var mockJobs = CreateMockDbSet(new List<BackgroundJob>().AsQueryable());
        Context.BackgroundJobs.Returns(mockJobs);

        var command = new CancelJobCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobNotFound);
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
            .WithCompletedAt(_now.AddDays(-1))
            .Build();

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var command = new CancelJobCommand(jobId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeCancelled);
    }

    [Test]
    public async Task Handle_FailedJob_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Failed)
            .WithError("Something went wrong")
            .Build();

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var command = new CancelJobCommand(jobId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeCancelled);
    }

    [Test]
    public async Task Handle_CancelledJob_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .WithStatus(JobStatus.Cancelled)
            .WithCompletedAt(_now.AddDays(-1))
            .Build();

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var command = new CancelJobCommand(jobId);

        // Act
        var result = await _handler.Handle(command, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobCannotBeCancelled);
    }
}
