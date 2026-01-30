using ExtraTime.Application.Features.Admin;
using ExtraTime.Application.Features.Admin.DTOs;
using ExtraTime.Application.Features.Admin.Queries.GetJobs;
using ExtraTime.Domain.Enums;
using ExtraTime.IntegrationTests.Common;
using ExtraTime.UnitTests.TestData;

namespace ExtraTime.IntegrationTests.Application.Features.Admin;

public sealed class GetJobsQueryIntegrationTests : IntegrationTestBase
{
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
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Items.Count).IsEqualTo(0);
        await Assert.That(result.Value.TotalCount).IsEqualTo(0);
        await Assert.That(result.Value.TotalPages).IsEqualTo(0);
    }

    [Test]
    public async Task GetJobs_WithJobs_ReturnsPagedResults()
    {
        // Arrange
        var job1 = new BackgroundJobBuilder()
            .WithJobType("SyncCompetitions")
            .WithStatus(JobStatus.Completed)
            .Build();

        var job2 = new BackgroundJobBuilder()
            .WithJobType("SyncMatches")
            .WithStatus(JobStatus.Pending)
            .Build();

        var job3 = new BackgroundJobBuilder()
            .WithJobType("CalculateResults")
            .WithStatus(JobStatus.Failed)
            .Build();

        Context.BackgroundJobs.AddRange(job1, job2, job3);
        await Context.SaveChangesAsync();

        var handler = new GetJobsQueryHandler(Context);
        var query = new GetJobsQuery(Page: 1, PageSize: 2);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsNotNull();
        await Assert.That(result.Value!.Items.Count).IsEqualTo(2);
        await Assert.That(result.Value.TotalCount).IsEqualTo(3);
        await Assert.That(result.Value.TotalPages).IsEqualTo(2);
        await Assert.That(result.Value.Page).IsEqualTo(1);
        await Assert.That(result.Value.PageSize).IsEqualTo(2);
    }

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
        await Assert.That(result.Value.TotalCount).IsEqualTo(1);
        await Assert.That(result.Value.Items[0].Status).IsEqualTo(JobStatus.Pending);
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
        await Assert.That(result.Value.TotalCount).IsEqualTo(2);
        await Assert.That(result.Value.Items[0].JobType).IsEqualTo("SyncCompetitions");
        await Assert.That(result.Value.Items[1].JobType).IsEqualTo("SyncCompetitions");
    }
}
