using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Admin;
using ExtraTime.Application.Features.Admin.DTOs;
using ExtraTime.Application.Features.Admin.Queries.GetJobById;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.UnitTests.Application.Features.Admin.Handlers;

public sealed class GetJobByIdQueryHandlerTests : HandlerTestBase
{
    private readonly GetJobByIdQueryHandler _handler;

    public GetJobByIdQueryHandlerTests()
    {
        _handler = new GetJobByIdQueryHandler(Context);
    }

    [Test]
    public async Task Handle_ExistingJob_ReturnsJobDto()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddHours(-2);
        var startedAt = DateTime.UtcNow.AddHours(-1);
        var completedAt = DateTime.UtcNow.AddMinutes(-30);
        var createdByUserId = Guid.NewGuid();

        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("CalculateBetResults")
            .WithStatus(JobStatus.Completed)
            .WithPayload("{\"leagueId\": \"123\"}")
            .WithResult("Processed 50 bets")
            .WithRetryCount(1)
            .WithMaxRetries(3)
            .WithCreatedAt(createdAt)
            .WithStartedAt(startedAt)
            .WithCompletedAt(completedAt)
            .WithCreatedByUserId(createdByUserId)
            .WithCorrelationId("corr-123")
            .Build();

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var query = new GetJobByIdQuery(jobId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Id).IsEqualTo(jobId);
        await Assert.That(result.Value.JobType).IsEqualTo("CalculateBetResults");
        await Assert.That(result.Value.Status).IsEqualTo(JobStatus.Completed);
        await Assert.That(result.Value.Payload).IsEqualTo("{\"leagueId\": \"123\"}");
        await Assert.That(result.Value.Result).IsEqualTo("Processed 50 bets");
        await Assert.That(result.Value.RetryCount).IsEqualTo(1);
        await Assert.That(result.Value.MaxRetries).IsEqualTo(3);
        await Assert.That(result.Value.CreatedAt).IsEqualTo(createdAt);
        await Assert.That(result.Value.StartedAt).IsEqualTo(startedAt);
        await Assert.That(result.Value.CompletedAt).IsEqualTo(completedAt);
        await Assert.That(result.Value.CreatedByUserId).IsEqualTo(createdByUserId);
        await Assert.That(result.Value.CorrelationId).IsEqualTo("corr-123");
    }

    [Test]
    public async Task Handle_JobNotFound_ReturnsFailure()
    {
        // Arrange
        var mockJobs = CreateMockDbSet(new List<BackgroundJob>().AsQueryable());
        Context.BackgroundJobs.Returns(mockJobs);

        var query = new GetJobByIdQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobNotFound);
    }

    [Test]
    public async Task Handle_FailedJob_ReturnsJobWithError()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("FailingJob")
            .WithStatus(JobStatus.Failed)
            .WithError("Connection timeout after 30 seconds")
            .WithRetryCount(3)
            .Build();

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var query = new GetJobByIdQuery(jobId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Status).IsEqualTo(JobStatus.Failed);
        await Assert.That(result.Value.Error).IsEqualTo("Connection timeout after 30 seconds");
    }

    [Test]
    public async Task Handle_PendingJob_ReturnsJobWithoutStartedAt()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("PendingJob")
            .WithStatus(JobStatus.Pending)
            .WithStartedAt(null)
            .WithCompletedAt(null)
            .Build();

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var query = new GetJobByIdQuery(jobId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Status).IsEqualTo(JobStatus.Pending);
        await Assert.That(result.Value.StartedAt).IsNull();
        await Assert.That(result.Value.CompletedAt).IsNull();
    }

    [Test]
    public async Task Handle_ProcessingJob_ReturnsJobWithStartedAt()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow.AddMinutes(-10);
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("ProcessingJob")
            .WithStatus(JobStatus.Processing)
            .WithStartedAt(startedAt)
            .WithCompletedAt(null)
            .Build();

        var jobs = new List<BackgroundJob> { job }.AsQueryable();
        var mockJobs = CreateMockDbSet(jobs);
        Context.BackgroundJobs.Returns(mockJobs);

        var query = new GetJobByIdQuery(jobId);

        // Act
        var result = await _handler.Handle(query, CancellationToken);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value!.Status).IsEqualTo(JobStatus.Processing);
        await Assert.That(result.Value.StartedAt).IsEqualTo(startedAt);
        await Assert.That(result.Value.CompletedAt).IsNull();
    }
}
