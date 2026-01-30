using ExtraTime.Application.Features.Admin;
using ExtraTime.Application.Features.Admin.DTOs;
using ExtraTime.Application.Features.Admin.Queries.GetJobById;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;

namespace ExtraTime.IntegrationTests.Application.Features.Admin;

public sealed class GetJobByIdQueryIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetJobById_ExistingJob_ReturnsJobDetails()
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
            .WithError(null)
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
        await Assert.That(result.Value).IsNotNull();

        var jobDto = result.Value!;
        await Assert.That(jobDto.Id).IsEqualTo(jobId);
        await Assert.That(jobDto.JobType).IsEqualTo("TestJob");
        await Assert.That(jobDto.Status).IsEqualTo(JobStatus.Completed);
        await Assert.That(jobDto.Payload).IsEqualTo("{\"key\":\"value\"}");
        await Assert.That(jobDto.Result).IsEqualTo("Success");
        await Assert.That(jobDto.Error).IsNull();
        await Assert.That(jobDto.RetryCount).IsEqualTo(0);
        await Assert.That(jobDto.MaxRetries).IsEqualTo(3);
        await Assert.That(jobDto.CreatedByUserId).IsEqualTo(userId);
        await Assert.That(jobDto.CorrelationId).IsEqualTo("corr-123");
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
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Error).IsEqualTo(AdminErrors.JobNotFound);
    }
}
