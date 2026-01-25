using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Admin.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Admin.Queries.GetJobs;

public sealed class GetJobsQueryHandler(
    IApplicationDbContext context) : IRequestHandler<GetJobsQuery, Result<JobsPagedResponse>>
{
    public async ValueTask<Result<JobsPagedResponse>> Handle(
        GetJobsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.BackgroundJobs.AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(j => j.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.JobType))
        {
            query = query.Where(j => j.JobType == request.JobType);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(j => new JobSummaryDto(
                j.Id,
                j.JobType,
                j.Status,
                j.RetryCount,
                j.CreatedAt,
                j.CompletedAt))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return Result<JobsPagedResponse>.Success(new JobsPagedResponse(
            items,
            totalCount,
            request.Page,
            request.PageSize,
            totalPages));
    }
}
