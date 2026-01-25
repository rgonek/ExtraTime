using ExtraTime.Domain.Enums;

namespace ExtraTime.Application.Features.Admin.DTOs;

public sealed record JobDto(
    Guid Id,
    string JobType,
    JobStatus Status,
    string? Payload,
    string? Result,
    string? Error,
    int RetryCount,
    int MaxRetries,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? ScheduledAt,
    Guid? CreatedByUserId,
    string? CorrelationId);

public sealed record JobSummaryDto(
    Guid Id,
    string JobType,
    JobStatus Status,
    int RetryCount,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public sealed record JobsPagedResponse(
    IReadOnlyList<JobSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record JobStatsDto(
    int TotalJobs,
    int PendingJobs,
    int ProcessingJobs,
    int CompletedJobs,
    int FailedJobs,
    int CancelledJobs);
