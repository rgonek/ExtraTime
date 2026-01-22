using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.Entities;

public sealed class BackgroundJob : BaseEntity
{
    public required string JobType { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string? Payload { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CorrelationId { get; set; }
}
