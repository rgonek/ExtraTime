using ExtraTime.Application.Common;
using ExtraTime.Application.Features.Admin.DTOs;
using ExtraTime.Domain.Enums;
using Mediator;

namespace ExtraTime.Application.Features.Admin.Queries.GetJobs;

public sealed record GetJobsQuery(
    int Page = 1,
    int PageSize = 20,
    JobStatus? Status = null,
    string? JobType = null) : IRequest<Result<JobsPagedResponse>>;
