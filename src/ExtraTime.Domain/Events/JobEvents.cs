using ExtraTime.Domain.Common;
using ExtraTime.Domain.Enums;

namespace ExtraTime.Domain.Events;

public sealed record JobCreated(Guid JobId, string JobType) : IDomainEvent;

public sealed record JobStatusChanged(Guid JobId, JobStatus OldStatus, JobStatus NewStatus) : IDomainEvent;

public sealed record JobFailed(Guid JobId, string Error, int RetryCount) : IDomainEvent;

public sealed record JobRetrying(Guid JobId, int CurrentRetry, int MaxRetries) : IDomainEvent;
