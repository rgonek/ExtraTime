using System.Text.Json;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services;

public sealed class InMemoryJobDispatcher(
    IApplicationDbContext context,
    ILogger<InMemoryJobDispatcher> logger) : IJobDispatcher
{
    public async Task<Guid> EnqueueAsync<T>(string jobType, T payload, CancellationToken cancellationToken = default)
    {
        var job = new BackgroundJob
        {
            JobType = jobType,
            Payload = JsonSerializer.Serialize(payload),
            Status = JobStatus.Pending,
            CreatedAt = Clock.UtcNow
        };

        context.BackgroundJobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Job {JobId} of type {JobType} enqueued", job.Id, jobType);

        return job.Id;
    }

    public Task DispatchAsync(BackgroundJob job, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Job {JobId} dispatched for processing", job.Id);
        return Task.CompletedTask;
    }
}
