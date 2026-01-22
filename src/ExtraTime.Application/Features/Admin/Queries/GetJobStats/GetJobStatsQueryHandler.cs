using ExtraTime.Application.Common;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Admin.DTOs;
using ExtraTime.Domain.Enums;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Application.Features.Admin.Queries.GetJobStats;

public sealed class GetJobStatsQueryHandler(
    IApplicationDbContext context) : IRequestHandler<GetJobStatsQuery, Result<JobStatsDto>>
{
    public async ValueTask<Result<JobStatsDto>> Handle(
        GetJobStatsQuery request,
        CancellationToken cancellationToken)
    {
        var stats = await context.BackgroundJobs
            .GroupBy(_ => 1)
            .Select(g => new JobStatsDto(
                g.Count(),
                g.Count(j => j.Status == JobStatus.Pending),
                g.Count(j => j.Status == JobStatus.Processing),
                g.Count(j => j.Status == JobStatus.Completed),
                g.Count(j => j.Status == JobStatus.Failed),
                g.Count(j => j.Status == JobStatus.Cancelled)))
            .FirstOrDefaultAsync(cancellationToken);

        return Result<JobStatsDto>.Success(stats ?? new JobStatsDto(0, 0, 0, 0, 0, 0));
    }
}
