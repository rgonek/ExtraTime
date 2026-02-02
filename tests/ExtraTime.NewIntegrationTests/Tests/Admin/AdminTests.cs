using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Admin;
using ExtraTime.Application.Features.Admin.Commands.CancelJob;
using ExtraTime.Application.Features.Admin.Commands.RetryJob;
using ExtraTime.Application.Features.Admin.Queries.GetJobById;
using ExtraTime.Application.Features.Admin.Queries.GetJobs;
using ExtraTime.Application.Features.Admin.Queries.GetJobStats;
using ExtraTime.Domain.Enums;
using ExtraTime.NewIntegrationTests.Base;
using ExtraTime.UnitTests.TestData;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExtraTime.NewIntegrationTests.Tests.Admin;

public sealed class AdminTests : NewIntegrationTestBase
{
    // ... (existing commands)

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
        await Assert.That(result.Value.TotalJobs).IsEqualTo(5);
        await Assert.That(result.Value.PendingJobs).IsEqualTo(1);
    }

    [Test]
    public async Task GetJobById_ExistingJob_ReturnsJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new BackgroundJobBuilder()
            .WithId(jobId)
            .WithJobType("TestJob")
            .Build();
        Context.BackgroundJobs.Add(job);
        await Context.SaveChangesAsync();

        var handler = new GetJobByIdQueryHandler(Context);
        var query = new GetJobByIdQuery(jobId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value.Id).IsEqualTo(jobId);
        await Assert.That(result.Value.JobType).IsEqualTo("TestJob");
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
        await Assert.That(result.Value.Items.Count).IsEqualTo(5);
        await Assert.That(result.Value.TotalCount).IsEqualTo(8); // 0, 2, 4, 6, 8, 10, 12, 14 are Completed
    }
}
