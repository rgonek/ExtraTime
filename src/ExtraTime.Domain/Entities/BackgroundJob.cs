using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.Events;

namespace ExtraTime.Domain.Entities;

public sealed class BackgroundJob : BaseEntity
{
    public string JobType { get; private set; } = null!;
    public JobStatus Status { get; private set; } = JobStatus.Pending;
    public string? Payload { get; private set; }
    public string? Result { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = 3;
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public string? CorrelationId { get; private set; }

    private BackgroundJob() { } // Required for EF Core

    public static BackgroundJob Create(
        string jobType,
        string? payload = null,
        DateTime? scheduledAt = null,
        Guid? createdByUserId = null,
        string? correlationId = null,
        int maxRetries = 3)
    {
        if (string.IsNullOrWhiteSpace(jobType))
            throw new ArgumentException("Job type is required", nameof(jobType));

        var job = new BackgroundJob
        {
            JobType = jobType,
            Payload = payload,
            ScheduledAt = scheduledAt,
            CreatedByUserId = createdByUserId,
            CorrelationId = correlationId,
            MaxRetries = maxRetries,
            CreatedAt = Clock.UtcNow,
            Status = JobStatus.Pending
        };

        job.AddDomainEvent(new JobCreated(job.Id, jobType));
        return job;
    }

    public void MarkAsProcessing()
    {
        if (Status != JobStatus.Pending && Status != JobStatus.Retrying)
            throw new InvalidOperationException($"Cannot mark job as processing from status {Status}");

        var oldStatus = Status;
        Status = JobStatus.Processing;
        StartedAt = Clock.UtcNow;

        AddDomainEvent(new JobStatusChanged(Id, oldStatus, Status));
    }

    public void MarkAsCompleted(string? result = null)
    {
        if (Status != JobStatus.Processing)
            throw new InvalidOperationException($"Cannot complete job from status {Status}");

        var oldStatus = Status;
        Status = JobStatus.Completed;
        Result = result;
        CompletedAt = Clock.UtcNow;

        AddDomainEvent(new JobStatusChanged(Id, oldStatus, Status));
    }

    public void MarkAsFailed(string error)
    {
        if (Status != JobStatus.Processing)
            throw new InvalidOperationException($"Cannot fail job from status {Status}");

        var oldStatus = Status;
        Error = error;
        RetryCount++;

        if (RetryCount >= MaxRetries)
        {
            Status = JobStatus.Failed;
            CompletedAt = Clock.UtcNow;
            AddDomainEvent(new JobFailed(Id, error, RetryCount));
        }
        else
        {
            Status = JobStatus.Retrying;
            AddDomainEvent(new JobRetrying(Id, RetryCount, MaxRetries));
        }

        AddDomainEvent(new JobStatusChanged(Id, oldStatus, Status));
    }

    public void Cancel()
    {
        if (Status != JobStatus.Pending && Status != JobStatus.Processing && Status != JobStatus.Retrying)
            throw new InvalidOperationException($"Cannot cancel job with status {Status}");

        var oldStatus = Status;
        Status = JobStatus.Cancelled;
        CompletedAt = Clock.UtcNow;

        AddDomainEvent(new JobStatusChanged(Id, oldStatus, Status));
    }

    public void Retry()
    {
        if (Status != JobStatus.Failed && Status != JobStatus.Cancelled)
            throw new InvalidOperationException($"Cannot retry job with status {Status}");

        var oldStatus = Status;
        Status = JobStatus.Pending;
        Error = null;
        CompletedAt = null;
        RetryCount++;

        AddDomainEvent(new JobStatusChanged(Id, oldStatus, Status));
    }

    public bool CanBeRetried => Status == JobStatus.Failed || Status == JobStatus.Cancelled;
    public bool IsTerminalStatus => Status == JobStatus.Completed || Status == JobStatus.Failed || Status == JobStatus.Cancelled;
}
